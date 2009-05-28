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
using System.IO;
using System.Text;
using System.Web;
using LinkbackNet;
using LinkbackNet.Web;
using Moq;
using Xunit;

namespace Tests
{
    public class PingbackReceive
    {
        [Fact]
        public void Receive()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);
            string request_content = String.Format("<?xml version=\"1.0\"?><methodCall><methodName>pingback.ping</methodName><params><param><value><string>{0}</string></value></param><param><value><string>{1}</string></value></param></params></methodCall>",
                "http://source/1", "http://target/1");
            request.SetupGet(x => x.InputStream).Returns(new MemoryStream(new UTF8Encoding().GetBytes(request_content)));

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1", UriKind.Absolute))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = @"
...
<head>
    <title>Source Title</title>
</head>
<div>
<p>
This is a text <a href=""http://tarGet/1"">post1</a> with link
</p>
</div>
...
";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var pingback = new Pingback(webRequest.Object);

            var result = pingback.Receive(request.Object, null);

            // Verify

            request.VerifyAll();
            webRequest.VerifyAll();
            webResponse.VerifyAll();

            Assert.True(result.Valid);
            Assert.Equal("http://source/1", result.SourceUri.ToString().ToLowerInvariant());
            Assert.Equal("http://target/1", result.TargetUri.ToString().ToLowerInvariant());
            Assert.Equal("Source Title", result.Title);
            Assert.Equal("Source Title", result.BlogName);
            Assert.Equal("This is a text post1 with link", result.Excerpt);
        }

        [Fact]
        public void Request_Params_Do_Not_Contain_SourceURL()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);
            string request_content = String.Format("<?xml version=\"1.0\"?><methodCall><methodName>pingback.ping</methodName><params><param><value><string>{0}</string></value></param></params></methodCall>",
                "http://target/1");
            request.SetupGet(x => x.InputStream).Returns(new MemoryStream(new UTF8Encoding().GetBytes(request_content)));

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            // Test

            var pingback = new Pingback(webRequest.Object);

            var result = pingback.Receive(request.Object, null);

            // Verify

            request.VerifyAll();
            webRequest.VerifyAll();

            Assert.False(result.Valid);
            Assert.Null(result.SourceUri);
            Assert.Null(result.TargetUri);
        }

        [Fact]
        public void Request_Params_Do_Not_Contain_TargetURL()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);
            string request_content = String.Format("<?xml version=\"1.0\"?><methodCall><methodName>pingback.ping</methodName><params><param><value><string>{0}</string></value></param></params></methodCall>",
                "http://source/1");
            request.SetupGet(x => x.InputStream).Returns(new MemoryStream(new UTF8Encoding().GetBytes(request_content)));

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            // Test

            var pingback = new Pingback(webRequest.Object);

            var result = pingback.Receive(request.Object, null);

            // Verify

            request.VerifyAll();
            webRequest.VerifyAll();

            Assert.False(result.Valid);
            Assert.Null(result.SourceUri);
            Assert.Null(result.TargetUri);
        }

        [Fact]
        public void Request_Params_Invalid_SourceURI()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);
            string request_content = String.Format("<?xml version=\"1.0\"?><methodCall><methodName>pingback.ping</methodName><params><param><value><string>{0}</string></value></param><param><value><string>{1}</string></value></param></params></methodCall>",
                "~this~is~not~an~url*xxx://", "http://target/1");
            request.SetupGet(x => x.InputStream).Returns(new MemoryStream(new UTF8Encoding().GetBytes(request_content)));

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            // Test

            var pingback = new Pingback(webRequest.Object);

            var result = pingback.Receive(request.Object, null);

            // Verify

            request.VerifyAll();
            webRequest.VerifyAll();

            Assert.False(result.Valid);
            Assert.Null(result.SourceUri);
            Assert.Null(result.TargetUri);
        }

        [Fact]
        public void Request_Params_Invalid_TargetURI()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);
            string request_content = String.Format("<?xml version=\"1.0\"?><methodCall><methodName>pingback.ping</methodName><params><param><value><string>{0}</string></value></param><param><value><string>{1}</string></value></param></params></methodCall>",
                "http://source/1", "~this~is~not~an~url*xxx://");
            request.SetupGet(x => x.InputStream).Returns(new MemoryStream(new UTF8Encoding().GetBytes(request_content)));

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            // Test

            var pingback = new Pingback(webRequest.Object);

            var result = pingback.Receive(request.Object, null);

            // Verify

            request.VerifyAll();
            webRequest.VerifyAll();

            Assert.False(result.Valid);
            Assert.Equal("http://source/1", result.SourceUri.ToString().ToLowerInvariant());
            Assert.Null(result.TargetUri);
        }

        [Fact]
        public void Request_Params_Not_Http_SourceURI()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);
            string request_content = String.Format("<?xml version=\"1.0\"?><methodCall><methodName>pingback.ping</methodName><params><param><value><string>{0}</string></value></param><param><value><string>{1}</string></value></param></params></methodCall>",
                "ftp://source/1", "http://target/1");
            request.SetupGet(x => x.InputStream).Returns(new MemoryStream(new UTF8Encoding().GetBytes(request_content)));

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            // Test

            var pingback = new Pingback(webRequest.Object);

            var result = pingback.Receive(request.Object, null);

            // Verify

            request.VerifyAll();
            webRequest.VerifyAll();

            Assert.False(result.Valid);
            Assert.Null(result.SourceUri);
            Assert.Null(result.TargetUri);
        }

        [Fact]
        public void Request_Params_Not_Http_TargetURI()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);
            string request_content = String.Format("<?xml version=\"1.0\"?><methodCall><methodName>pingback.ping</methodName><params><param><value><string>{0}</string></value></param><param><value><string>{1}</string></value></param></params></methodCall>",
                "http://source/1", "ftp://target/1");
            request.SetupGet(x => x.InputStream).Returns(new MemoryStream(new UTF8Encoding().GetBytes(request_content)));

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            // Test

            var pingback = new Pingback(webRequest.Object);

            var result = pingback.Receive(request.Object, null);

            // Verify

            request.VerifyAll();
            webRequest.VerifyAll();

            Assert.False(result.Valid);
            Assert.Equal("http://source/1", result.SourceUri.ToString().ToLowerInvariant());
            Assert.Null(result.TargetUri);
        }

        [Fact]
        public void Dicovery_Source_Empty()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);
            string request_content = String.Format("<?xml version=\"1.0\"?><methodCall><methodName>pingback.ping</methodName><params><param><value><string>{0}</string></value></param><param><value><string>{1}</string></value></param></params></methodCall>",
                "http://source/1", "http://target/1");
            request.SetupGet(x => x.InputStream).Returns(new MemoryStream(new UTF8Encoding().GetBytes(request_content)));

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1", UriKind.Absolute))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("")));

            // Test

            var pingback = new Pingback(webRequest.Object);

            var result = pingback.Receive(request.Object, null);

            // Verify

            request.VerifyAll();
            webRequest.VerifyAll();
            webResponse.VerifyAll();

            Assert.False(result.Valid);
            Assert.Equal("http://source/1", result.SourceUri.ToString().ToLowerInvariant());
            Assert.Equal("http://target/1", result.TargetUri.ToString().ToLowerInvariant());
        }

        [Fact]
        public void Discovery_Source_Has_No_Link()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);
            string request_content = String.Format("<?xml version=\"1.0\"?><methodCall><methodName>pingback.ping</methodName><params><param><value><string>{0}</string></value></param><param><value><string>{1}</string></value></param></params></methodCall>",
                "http://source/1", "http://target/1");
            request.SetupGet(x => x.InputStream).Returns(new MemoryStream(new UTF8Encoding().GetBytes(request_content)));

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1", UriKind.Absolute))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            string source = "... http://ta-get/1 ...";
            webResponse.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(source)));

            // Test

            var pingback = new Pingback(webRequest.Object);

            var result = pingback.Receive(request.Object, null);

            // Verify

            request.VerifyAll();
            webRequest.VerifyAll();
            webResponse.VerifyAll();

            Assert.False(result.Valid);
            Assert.Equal("http://source/1", result.SourceUri.ToString().ToLowerInvariant());
            Assert.Equal("http://target/1", result.TargetUri.ToString().ToLowerInvariant());
        }

        [Fact]
        public void WebException_Saved()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);
            string request_content = String.Format("<?xml version=\"1.0\"?><methodCall><methodName>pingback.ping</methodName><params><param><value><string>{0}</string></value></param><param><value><string>{1}</string></value></param></params></methodCall>",
                "http://source/1", "http://target/1");
            request.SetupGet(x => x.InputStream).Returns(new MemoryStream(new UTF8Encoding().GetBytes(request_content)));

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1", UriKind.Absolute))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Throws(new System.Net.WebException("WebException"));

            // Test

            var pingback = new Pingback(webRequest.Object);

            var result = pingback.Receive(request.Object, null);

            // Verify

            request.VerifyAll();
            webRequest.VerifyAll();

            Assert.False(result.Valid);
            Assert.IsType<System.Net.WebException>(result.ReceiveException);
            Assert.Equal("WebException", result.ReceiveException.Message);
        }

        [Fact]
        public void Source_Discovery_Http_Error_LinkbackReceiveException_Saved()
        {
            // Setup

            var request = new Mock<HttpRequestBase>(MockBehavior.Strict);
            string request_content = String.Format("<?xml version=\"1.0\"?><methodCall><methodName>pingback.ping</methodName><params><param><value><string>{0}</string></value></param><param><value><string>{1}</string></value></param></params></methodCall>", "http://source/1", "http://target/1");
            request.SetupGet(x => x.InputStream).Returns(new MemoryStream(new UTF8Encoding().GetBytes(request_content)));

            var webRequest = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest.Setup(x => x.Create(new Uri("http://source/1", UriKind.Absolute))).Returns(webRequest.Object);
            webRequest.Setup(x => x.GetResponse()).Returns(webResponse.Object);

            webResponse.Setup(x => x.Close());
            webResponse.SetupGet(x => x.IsHttpStatusCode2XX).Returns(false);

            // Test

            var pingback = new Pingback(webRequest.Object);

            var result = pingback.Receive(request.Object, null);

            // Verify

            request.VerifyAll();
            webRequest.VerifyAll();
            webResponse.VerifyAll();

            Assert.False(result.Valid);
            Assert.IsType<LinkbackReceiveException>(result.ReceiveException);
            Assert.Equal("Http error while discovering Pingback source at http://source/1", result.ReceiveException.Message);
        }
    }
}