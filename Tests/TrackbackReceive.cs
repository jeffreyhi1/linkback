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
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using LinkbackNet;
using LinkbackNet.Web;
using Moq;
using Xunit;

namespace Tests
{
    public class TrackbackReceive
    {
        [Fact]
        public void Receive()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "http://source/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... <a href=\"http://taRget/post/1\">post1</a> ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.True(result.Valid);
            Assert.Equal("Source Title", result.Title);
            Assert.Equal("ABC", result.Excerpt);
            Assert.Equal("http://source/1", result.Url.ToString().ToLowerInvariant());
            Assert.Equal("Test Blog", result.BlogName);
        }

        [Fact]
        public void Discovery_Source_Empty()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection
                {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "http://source/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream());

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.False(result.Valid);
            Assert.Equal("Source Title", result.Title);
            Assert.Equal("ABC", result.Excerpt);
            Assert.Equal("http://source/1", result.Url.ToString().ToLowerInvariant());
            Assert.Equal("Test Blog", result.BlogName);
        }

        [Fact]
        public void Discovery_Source_Has_No_Link()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection
                {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "http://source/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... http://target/any ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.False(result.Valid);
            Assert.Equal("Source Title", result.Title);
            Assert.Equal("ABC", result.Excerpt);
            Assert.Equal("http://source/1", result.Url.ToString().ToLowerInvariant());
            Assert.Equal("Test Blog", result.BlogName);
        }

        [Fact]
        public void WebException_Saved()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "http://source/post/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/post/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Throws(new System.Net.WebException("WebException"));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            request.VerifyAll();

            Assert.False(result.Valid);
            Assert.IsType<System.Net.WebException>(result.ReceiveException);
            Assert.Equal("WebException", result.ReceiveException.Message);
        }

        [Fact]
        public void Source_Discovery_Http_Error_LinkbackReceiveException_Saved()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "http://source/post/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/post/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(false);
            webResponse.Setup(x => x.Close());

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.False(result.Valid);
            Assert.IsType<LinkbackReceiveException>(result.ReceiveException);
            Assert.Equal("Http error while discovering Trackback source at http://source/post/1", result.ReceiveException.Message);
        }

        [Fact]
        public void Title_Not_Specified()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"excerpt", "ABC"},
                    {"url", "http://source/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... <a href=\"http://taRget/post/1\">post1</a> ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.True(result.Valid);
            Assert.True(String.IsNullOrEmpty(result.Title));
            Assert.Equal("ABC", result.Excerpt);
            Assert.Equal("http://source/1", result.Url.ToString().ToLowerInvariant());
            Assert.Equal("Test Blog", result.BlogName);
        }

        [Fact]
        public void Excerpt_Not_Specified()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"url", "http://source/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... <a href=\"http://taRget/post/1\">post1</a> ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.True(result.Valid);
            Assert.Equal("Source Title", result.Title);
            Assert.True(String.IsNullOrEmpty(result.Excerpt));
            Assert.Equal("http://source/1", result.Url.ToString().ToLowerInvariant());
            Assert.Equal("Test Blog", result.BlogName);
        }

        [Fact]
        public void Url_Not_Specified_LinkbackReceiveException_Saved()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            request.VerifyAll();

            Assert.False(result.Valid);
            Assert.IsType<LinkbackReceiveException>(result.ReceiveException);
            Assert.Equal("Url parameter for Trackback not specified", result.ReceiveException.Message);
        }

        [Fact]
        public void BlogName_Not_Specified()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "http://source/1"},
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... <a href=\"http://taRget/post/1\">post1</a> ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.True(result.Valid);
            Assert.Equal("Source Title", result.Title);
            Assert.Equal("ABC", result.Excerpt);
            Assert.Equal("http://source/1", result.Url.ToString().ToLowerInvariant());
            Assert.True(String.IsNullOrEmpty(result.BlogName));
        }

        [Fact]
        public void Uri_Not_Http_Or_Https_UriFormatException_Saved()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "ftp://source/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            request.VerifyAll();

            Assert.False(result.Valid);
            Assert.IsType<UriFormatException>(result.ReceiveException);
            Assert.Equal("Url scheme must be http or https", result.ReceiveException.Message);
        }

        [Fact]
        public void Uri_Invalid_UriFormatException_Saved()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "~this~is~not~an~url*xxx://"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            request.VerifyAll();

            Assert.False(result.Valid);
            Assert.IsType<UriFormatException>(result.ReceiveException);
        }

        [Fact]
        public void Uri_Scheme_Http()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "http://source/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... <a href=\"http://taRget/post/1\">post1</a> ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.True(result.Valid);
        }

        [Fact]
        public void Uri_Scheme_Https()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "https://source/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("https://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... <a href=\"http://taRget/post/1\">post1</a> ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.True(result.Valid);
        }

        [Fact]
        public void Title_Encoded()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source <script /> Title"},
                    {"excerpt", "ABC"},
                    {"url", "http://source/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... <a href=\"http://taRget/post/1\">post1</a> ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.True(result.Valid);
            Assert.Equal("Source &lt;script /&gt; Title", result.Title);
        }

        [Fact]
        public void Excerpt_Encoded()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC <script />"},
                    {"url", "http://source/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... <a href=\"http://taRget/post/1\">post1</a> ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.True(result.Valid);
            Assert.Equal("ABC &lt;script /&gt;", result.Excerpt);
        }

        [Fact]
        public void BlogName_Encoded()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "http://source/1"},
                    {"blog_name", "Test <script /> Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... <a href=\"http://taRget/post/1\">post1</a> ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.True(result.Valid);
            Assert.Equal("Test &lt;script /&gt; Blog", result.BlogName);
        }

        [Fact]
        public void TargetUrl_Null_ArgumentNullException()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            // Test

            var trackback = new Trackback(webRequest.Object);

            ArgumentNullException ex = null;

            try
            {
                trackback.Receive(request.Object, null);
            }
            catch (ArgumentNullException _ex)
            {
                ex = _ex;
            }

            // Verify

            webRequest.VerifyAll();
            request.VerifyAll();

            Assert.NotNull(ex);
            Assert.Equal("targetUrl", ex.ParamName);
        }

        [Fact]
        public void TargetUrl_Not_Http_Or_Https_ArgumentOutOfRangeException()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            // Test

            var trackback = new Trackback(webRequest.Object);

            ArgumentOutOfRangeException ex = null;

            try
            {
                trackback.Receive(request.Object, new Uri("ftp://target/post/1"));
            }
            catch (ArgumentOutOfRangeException _ex)
            {
                ex = _ex;
            }

            // Verify

            webRequest.VerifyAll();
            request.VerifyAll();

            Assert.NotNull(ex);
            Assert.Equal("targetUrl", ex.ParamName);
        }

        [Fact]
        public void TargetUrl_Http()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "http://source/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... <a href=\"http://taRget/post/1\">post1</a> ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("http://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.True(result.Valid);
        }

        [Fact]
        public void TargetUrl_Https()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);

            request.SetupGet(x => x.Form).Returns(
                new NameValueCollection {
                    {"title", "Source Title"},
                    {"excerpt", "ABC"},
                    {"url", "http://source/1"},
                    {"blog_name", "Test Blog"}
                });

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1"))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... <a href=\"https://taRget/post/1\">post1</a> ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var trackback = new Trackback(webRequest.Object);

            var result = trackback.Receive(request.Object, new Uri("https://target/post/1"));

            // Verify

            webRequest.VerifyAll();
            webResponse.VerifyAll();
            request.VerifyAll();

            Assert.True(result.Valid);
        }
    }
}