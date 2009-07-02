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
using LinkbackNet.Web;
using System.Text;
using System.IO;
using System.Web;
using System.Globalization;

namespace LinkbackNet
{
    public class LinkbackSendParameters : ITrackbackSendParameters, IPingbackSendParameters
    {
        public Uri Url
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string Excerpt
        {
            get;
            set;
        }

        public string BlogName
        {
            get;
            set;
        }

        public bool? AutoDiscovery
        {
            get;
            set;
        }

        public void SetupRequestForTrackback(HttpWebRequestAbstraction request)
        {
            string post_content = BuildPostContent();
            byte[] post_bytes = new UTF8Encoding().GetBytes(post_content);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = post_bytes.Length;

            using (Stream writeStream = request.GetRequestStream())
            {
                writeStream.Write(post_bytes, 0, post_bytes.Length);
            }
        }

        string BuildPostContent()
        {
            StringBuilder post_content = new StringBuilder();

            if (Url == null)
            {
                throw new ArgumentNullException("Url");
            }

            string url_url_encoded = Uri.EscapeDataString(Url.AbsoluteUri);
            post_content.AppendFormat("url={0}", url_url_encoded);

            if (!String.IsNullOrEmpty(Title))
            {
                string title_url_encoded = Uri.EscapeDataString(Title);
                post_content.AppendFormat("&title={0}", title_url_encoded);
            }

            if (!String.IsNullOrEmpty(Excerpt))
            {
                string excerpt_html_encoded = HttpUtility.HtmlEncode(Excerpt);
                string excerpt_url_html_encoded = Uri.EscapeDataString(excerpt_html_encoded);
                post_content.AppendFormat("&excerpt={0}", excerpt_url_html_encoded);
            }

            if (!String.IsNullOrEmpty(BlogName))
            {
                string blog_name_url_encoded = Uri.EscapeDataString(BlogName);
                post_content.AppendFormat("&blog_name={0}", blog_name_url_encoded);
            }

            return post_content.ToString();
        }

        public Uri SourceUrl
        {
            get;
            set;
        }

        public Uri TargetUrl
        {
            get;
            set;
        }

        public void SetupRequestForPingback(HttpWebRequestAbstraction request)
        {
            if (SourceUrl == null)
            {
                //throw new ArgumentNullException("SourceUrl");
                throw new InvalidOperationException("SourceUrl is null");
            }

            if (TargetUrl == null)
            {
                throw new ArgumentNullException("TargetUrl");
            }

            string post_content = String.Format(CultureInfo.InvariantCulture, "<?xml version=\"1.0\"?><methodCall><methodName>pingback.ping</methodName><params><param><value><string>{0}</string></value></param><param><value><string>{1}</string></value></param></params></methodCall>", SourceUrl.AbsoluteUri, TargetUrl.AbsoluteUri);
            byte[] post_bytes = new UTF8Encoding().GetBytes(post_content);

            request.Method = "POST";
            request.ContentType = "text/xml";
            request.ContentLength = post_bytes.Length;

            using (Stream writeStream = request.GetRequestStream())
            {
                writeStream.Write(post_bytes, 0, post_bytes.Length);
            }
        }
    }
}