/*
http://www.opensource.org/licenses/bsd-license.php

Copyright (c) 2009, Linkback.NET Team (http://code.google.com/p/linkback/) All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted
provided that the following conditions are met:

- Redistributions of source code must retain the above copyright notice, this list of conditions
  and the following disclaimer.

- Redistributions in binary form must reproduce the above copyright notice, this list of conditions
  and the following disclaimer in the documentation and/or other materials provided with the
  distribution.

- Neither the name of the Linkback.NET Team nor the names of its contributors may be used to
  endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS
BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Web;
using LinkbackNet.Web;

namespace LinkbackNet
{
    /// <summary>
    /// http://en.wikipedia.org/wiki/Linkback
    /// </summary>
    public abstract class Linkback : ILinkback, ISendResult, IReceiveResult
    {
        public abstract string Name
        {
            get;
        }

        internal HttpWebRequestAbstraction HttpWebRequestAbstraction;

        protected Linkback()
            : this(new HttpWebRequestImplementation())
        {
        }

        protected Linkback(IHttpWebRequestImplementation implementation)
        {
            this.HttpWebRequestAbstraction = new HttpWebRequestAbstraction(implementation);
        }

        #region Send

        public ISendResult Send(Uri sendUrl, LinkbackSendParameters parameters)
        {
            HandleExceptionsOnSend(
                () => {
                    Uri targetUrl = DiscoveryTargetUrl(sendUrl, parameters);

                    SendToTarget(targetUrl, parameters);
                });

            return this as ISendResult;
        }

        void HandleExceptionsOnSend(Action action)
        {
            Success = false;

            try
            {
                action();

                Success = true;
            }
            catch (LinkbackSendException ex)
            {
                SendException = ex;
            }
            catch (System.Net.WebException ex)
            {
                SendException = ex;
            }
            catch (System.Net.ProtocolViolationException ex)
            {
                SendException = ex;
            }
        }

        protected abstract Uri DiscoveryTargetUrl(Uri sendUrl, LinkbackSendParameters parameters);

        void SendToTarget(Uri targetUrl, LinkbackSendParameters parameters)
        {
            HttpWebRequestAbstraction request = HttpWebRequestAbstraction.Create(targetUrl);

            SetupRequest(request, parameters);

            using (HttpWebResponseAbstraction response = request.GetResponse())
            {
                if (!response.IsHttpStatusCode2XX)
                {
                    throw new LinkbackSendException(String.Format(CultureInfo.InvariantCulture, "Http error while sending {0} for {1}", Name, targetUrl));
                }

                Stream receiveStream = response.GetResponseStream();

                if (response.ContentEncoding.ToUpperInvariant().Contains("GZIP"))
                    receiveStream = new GZipStream(receiveStream, CompressionMode.Decompress);
                else if (response.ContentEncoding.ToUpperInvariant().Contains("DEFLATE"))
                    receiveStream = new DeflateStream(receiveStream, CompressionMode.Decompress);

                StreamReader streamReader = new StreamReader(receiveStream, Encoding.UTF8);

                ParseResponse(targetUrl, streamReader.ReadToEnd());
            }
        }

        protected abstract void SetupRequest(HttpWebRequestAbstraction request, LinkbackSendParameters parameters);

        protected abstract void ParseResponse(Uri targetUrl, string responseContent);

        #endregion

        #region ILinkbackSendResult

        public bool Success
        {
            get;
            protected set;
        }

        public int Code
        {
            get;
            protected set;
        }

        public string Message
        {
            get;
            protected set;
        }

        public Exception SendException
        {
            get;
            protected set;
        }

        #endregion

        #region Receive

        public IReceiveResult Receive(HttpRequestBase request, Uri targetUrl)
        {
            HandleExceptionsOnReceive(
                () => {
                    if (CollectReceiveParametersFromRequest(request, targetUrl))
                    {
                        string content = DownloadSourceContent(LinkbackSourceUrl);

                        CollectReceiveParametersFromSource(content);

                        Valid = CheckRequest(content);
                    }
                });

            return this as IReceiveResult;
        }

        void HandleExceptionsOnReceive(Action action)
        {
            Valid = false;

            try
            {
                action();
            }
            catch (LinkbackReceiveException ex)
            {
                ReceiveException = ex;
            }
            catch (System.Net.WebException ex)
            {
                ReceiveException = ex;
            }
            catch (System.UriFormatException ex)
            {
                ReceiveException = ex;
            }
        }

        protected abstract bool CollectReceiveParametersFromRequest(HttpRequestBase request, Uri targetUrl);

        protected string DownloadSourceContent(Uri sourceUrl)
        {
            HttpWebRequestAbstraction request = HttpWebRequestAbstraction.Create(sourceUrl);
            using (HttpWebResponseAbstraction response = request.GetResponse())
            {
                if (response.IsHttpStatusCode2XX)
                {
                    Stream receiveStream = response.GetResponseStream();
                    StreamReader streamReader = new StreamReader(receiveStream, Encoding.UTF8);
                    return streamReader.ReadToEnd();
                }
            }

            throw new LinkbackReceiveException(String.Format(CultureInfo.InvariantCulture, "Http error while discovering {0} source at {1}", Name, sourceUrl));
        }

        protected virtual void CollectReceiveParametersFromSource(string content)
        {
        }

        protected abstract bool CheckRequest(string content);

        public abstract void SendResponse(HttpResponseBase response);

        static protected void SendResponseContent(HttpResponseBase response, string responseContent)
        {
            response.ContentType = "text/xml";

            using (Stream writeStream = response.OutputStream)
            {
                byte[] response_bytes = new UTF8Encoding().GetBytes(responseContent);
                writeStream.Write(response_bytes, 0, response_bytes.Length);
            }
        }

        #endregion

        #region ILinkbackReceiveResult

        protected Uri LinkbackSourceUrl
        {
            get;
            set;
        }

        protected Uri LinkbackTargetUrl
        {
            get;
            set;
        }

        public bool Valid
        {
            get;
            protected set;
        }

        public string Title
        {
            get;
            protected set;
        }

        public string Excerpt
        {
            get;
            protected set;
        }

        public Uri Url
        {
            get
            {
                return LinkbackSourceUrl;
            }
        }

        public string BlogName
        {
            get;
            protected set;
        }

        public Uri SourceUri
        {
            get
            {
                return LinkbackSourceUrl;
            }
        }

        public Uri TargetUri
        {
            get
            {
                return LinkbackTargetUrl;
            }
        }

        public Exception ReceiveException
        {
            get;
            protected set;
        }

        #endregion
    }
}