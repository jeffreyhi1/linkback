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
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using HtmlAgilityPack;
using LinkbackNet.Helpers;
using LinkbackNet.Web;

namespace LinkbackNet
{
    /// <summary>
    /// Version 1.2
    /// http://www.sixapart.com/pronet/docs/trackback_spec
    /// </summary>
    public class Trackback : Linkback
    {
        public override string Name
        {
            get
            {
                return "Trackback";
            }
        }

        public Trackback()
            : base()
        {
        }

        public Trackback(IHttpWebRequestImplementation implementation)
            : base(implementation)
        {
        }

        public static string DeclareServiceInHtml(Uri entryUrl, string entryTitle, Uri serviceUrl)
        {
            return String.Format(CultureInfo.InvariantCulture, "<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:trackback=\"http://madskills.com/public/xml/rss/module/trackback/\"><rdf:Description rdf:about=\"{0}\" dc:identifier=\"{0}\" dc:title=\"{1}\" trackback:ping=\"{2}\" /></rdf:RDF>", entryUrl, entryTitle, serviceUrl);
        }

        #region Send

        protected override Uri DiscoveryTargetUrl(Uri sendUrl, LinkbackSendParameters parameters)
        {
            if (parameters.AutoDiscovery == false)
            {
                return sendUrl;
            }

            HttpWebRequestAbstraction request = HttpWebRequestAbstraction.Create(sendUrl);
            using (HttpWebResponseAbstraction response = request.GetResponse())
            {
                if (response.IsHttpStatusCode2XX)
                {
                    return FindTargetUrl(sendUrl, parameters, response);
                }
            }

            if(parameters.AutoDiscovery == null)
            {
                return sendUrl;
            }

            throw new LinkbackSendException(String.Format(CultureInfo.InvariantCulture, "Http error while discovering {0} url for {1}", Name, sendUrl));
        }

        Regex _regex_rdf = new Regex(".*?<rdf:RDF.+?>.*?<rdf:Description.+?dc:identifier=\"([^\"]+)\".+?trackback:ping=\"([^\"]+)\".*?/>.*?</rdf:RDF>.*?", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        Uri FindTargetUrl(Uri sendUrl, LinkbackSendParameters parameters, HttpWebResponseAbstraction response)
        {
            Stream receiveStream = response.GetResponseStream();

            if (response.ContentEncoding.ToUpperInvariant().Contains("GZIP"))
                receiveStream = new GZipStream(receiveStream, CompressionMode.Decompress);
            else if (response.ContentEncoding.ToUpperInvariant().Contains("DEFLATE"))
                receiveStream = new DeflateStream(receiveStream, CompressionMode.Decompress);

            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
            string content = readStream.ReadToEnd();

            var matches = _regex_rdf.Matches(content);

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (String.Compare(match.Groups[1].ToString().Trim(), sendUrl.ToString(), StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        string trackback_ping_url = match.Groups[2].ToString().Trim();

                        return new Uri(trackback_ping_url, UriKind.Absolute);
                    }
                }
            }

            if (parameters.AutoDiscovery == null)
            {
                return sendUrl;
            }

            throw new LinkbackSendException(String.Format(CultureInfo.InvariantCulture, "RDF not found while discovering {0} url for {1}", Name, sendUrl));
        }

        protected override void SetupRequest(HttpWebRequestAbstraction request, LinkbackSendParameters parameters)
        {
            parameters.SetupRequestForTrackback(request);
        }

        protected override void ParseResponse(Uri targetUrl, string responseContent)
        {
            if (String.IsNullOrEmpty(responseContent))
            {
                // http://code.google.com/p/linkback/issues/detail?id=1
                Success = true;
                return;
            }

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(responseContent);

                XmlNode n1 = xml.SelectSingleNode("response/error");

                if (n1 != null && n1.InnerText != "0")
                {
                    Success = false;

                    int code;
                    if (int.TryParse(n1.InnerText, out code))
                    {
                        Code = code;
                    }
                    else
                    {
                        Message = n1.InnerText;
                    }

                    XmlNode n2 = xml.SelectSingleNode("response/message");
                    if (n2 != null)
                    {
                        Message = n2.InnerText;
                    }
                }
                else
                {
                    Success = true;
                }
            }
            catch (XmlException)
            {
                throw new LinkbackSendException(String.Format(CultureInfo.InvariantCulture, "Invalid response received from {0}", targetUrl));
            }
        }

        #endregion

        #region Receive

        protected override bool CollectReceiveParametersFromRequest(HttpRequestBase request, Uri targetUrl)
        {
            if (targetUrl == null)
            {
                throw new ArgumentNullException("targetUrl");
            }

            try
            {
                LinkbackTargetUrl = UriHelpers.CreateHttpUrl(targetUrl.ToString());
            }
            catch (UriFormatException)
            {
                throw new ArgumentOutOfRangeException("targetUrl");
            }

            if (String.IsNullOrEmpty(request.Form["url"]))
            {
                throw new LinkbackReceiveException(String.Format(CultureInfo.InvariantCulture, "Url parameter for {0} not specified", Name));
            }

            LinkbackSourceUrl = UriHelpers.CreateHttpUrl(request.Form["url"]);
            
            Title = HttpUtility.HtmlEncode(request.Form["title"]);
            Excerpt = HttpUtility.HtmlEncode(request.Form["excerpt"]);
            BlogName = HttpUtility.HtmlEncode(request.Form["blog_name"]);

            return true;
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
                ? "<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>0</error></response>"
                : "<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>1</error><message>Error</message></response>";

            SendResponseContent(response, response_content);
        }

        #endregion
    }
}