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
using System.Xml;
using HtmlAgilityPack;
using LinkbackNet.Helpers;
using LinkbackNet.Web;

namespace LinkbackNet
{
    /// <summary>
    /// Version 1.0
    /// http://www.hixie.ch/specs/pingback/pingback-1.0
    /// </summary>
    public class Pingback : Linkback
    {
        public override string Name
        {
            get
            {
                return "Pingback";
            }
        }

        public Pingback()
            : base()
        {
        }

        public Pingback(IHttpWebRequestImplementation implementation)
            : base(implementation)
        {
        }

        public static void DeclareServiceInHttpHeader(HttpResponseBase response, Uri serviceUrl)
        {
            response.AddHeader("X-Pingback", serviceUrl.AbsoluteUri);
        }

        public static string DeclareInLink(Uri serviceUrl)
        {
            return String.Format(CultureInfo.InvariantCulture, "<link rel=\"pingback\" href=\"{0}\" />", serviceUrl.AbsoluteUri);
        }

        #region Send

        protected override Uri DiscoveryTargetUrl(Uri sendUrl, LinkbackSendParameters parameters)
        {
            HttpWebRequestAbstraction request = HttpWebRequestAbstraction.Create(sendUrl);
            using (HttpWebResponseAbstraction response = request.GetResponse())
            {
                if (response.IsHttpStatusCode2XX)
                {
                    return FindTargetUrl(sendUrl, response);
                }
            }

            throw new LinkbackSendException(String.Format(CultureInfo.InvariantCulture, "Http error while discovering {0} url for {1}", Name, sendUrl));
        }

        Uri FindTargetUrl(Uri sendUrl, HttpWebResponseAbstraction response)
        {
            string ping_url = response.Headers["x-pingback"];

            if (!String.IsNullOrEmpty(ping_url))
                return new Uri(ping_url);

            Stream receiveStream = response.GetResponseStream();

            if (response.ContentEncoding.ToUpperInvariant().Contains("GZIP"))
                receiveStream = new GZipStream(receiveStream, CompressionMode.Decompress);
            else if (response.ContentEncoding.ToUpperInvariant().Contains("DEFLATE"))
                receiveStream = new DeflateStream(receiveStream, CompressionMode.Decompress);

            HtmlDocument html = new HtmlDocument();
            html.Load(receiveStream);

            var node = html.DocumentNode.SelectSingleNode("//link[@rel='pingback' and @href]");

            if (node != null)
            {
                return new Uri(node.Attributes["href"].Value.ToLowerInvariant());
            }

            throw new LinkbackSendException(String.Format(CultureInfo.InvariantCulture, "{0} url discovering failed for {1}", Name, sendUrl));
        }

        protected override void SetupRequest(HttpWebRequestAbstraction request, LinkbackSendParameters parameters)
        {
            parameters.SetupRequestForPingback(request);
        }

        protected override void ParseResponse(Uri targetUrl, string responseContent)
        {
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(responseContent);

                XmlNode n1 = xml.SelectSingleNode("methodResponse/fault");
                Success = n1 == null;

                if(!Success)
                {
                    ParseError(xml);
                }

                return;
            }
            catch (XmlException)
            {
                if (String.IsNullOrEmpty(responseContent))
                {
                    throw new LinkbackSendException(String.Format(CultureInfo.InvariantCulture, "Empty response received from {0}", targetUrl));
                }

                throw new LinkbackSendException(String.Format(CultureInfo.InvariantCulture, "Invalid response received from {0}", targetUrl));
            }
        }

        void ParseError(XmlDocument xml)
        {
            XmlNode faultCode = xml.SelectSingleNode("methodResponse/fault/value/struct/member[name='faultCode']/value");

            if (faultCode != null)
            {
                int code;
                if (int.TryParse(faultCode.InnerText, out code))
                {
                    Code = code;
                }
            }

            XmlNode faultString = xml.SelectSingleNode("methodResponse/fault/value/struct/member[name='faultString']/value");

            if (faultString != null)
            {
                Message = faultString.InnerText;
            }
        }

        #endregion

        #region Receive

        protected override bool CollectReceiveParametersFromRequest(HttpRequestBase request, Uri targetUrl)
        {
            byte[] buffer = new byte[request.InputStream.Length];
            request.InputStream.Read(buffer, 0, buffer.Length);
            string request_content = Encoding.UTF8.GetString(buffer);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(request_content);

            XmlNode n1 = doc.SelectSingleNode("methodCall/methodName");
            if (n1 == null || n1.InnerText != "pingback.ping") return false;

            XmlNodeList list = doc.SelectNodes("methodCall/params/param/value/string");
            if (list.Count != 2)
                return false;

            try
            {
                LinkbackSourceUrl = UriHelpers.CreateHttpUrl(list[0].InnerText.Trim());

                LinkbackTargetUrl = UriHelpers.CreateHttpUrl(list[1].InnerText.Trim());
            }
            catch (UriFormatException)
            {
                return false;
            }

            return true;
        }

        protected override void CollectReceiveParametersFromSource(string content)
        {
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(content);

            var title = html.DocumentNode.SelectSingleNode("//head/title");

            if (title != null)
            {
                Title = title.InnerText;
                BlogName = Title;
            }

            var link = HtmlHelpers.GetLinkNode(html, LinkbackTargetUrl);

            if (link != null)
            {
                Excerpt = link.ParentNode.InnerText.Trim();
            }
        }
        
        protected override bool CheckRequest(string content)
        {
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(content);

            return HtmlHelpers.HtmlContainsLink(html, LinkbackTargetUrl);
        }

        public override void SendResponse(HttpResponseBase response)
        {
            string response_content = Valid
                ? "<?xml version=\"1.0\"?><methodResponse><params><param><value><string>ok</string></value></param></params></methodResponse>"
                : "<?xml version=\"1.0\"?><methodResponse><fault><value><struct><member><name>faultCode</name><value><int>0</int></value></member><member><name>faultString</name><value><string>Error.</string></value></member></struct></value></fault></methodResponse>";

            SendResponseContent(response, response_content);
        }

        #endregion
    }
}