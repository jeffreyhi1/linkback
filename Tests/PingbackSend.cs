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
using LinkbackNet;
using LinkbackNet.Web;
using Moq;
using Xunit;
using System.IO.Compression;

namespace Tests
{
    public class PingbackSend
    {
        [Fact]
        public void Discovery_Header()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.Headers).Returns(
                new System.Net.WebHeaderCollection {
                    {"X-Pingback", "http://target/pingback"}
                });

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/pingback", UriKind.Absolute))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "text/xml");
            webRequest2.SetupSet(x => x.ContentLength = 225);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\"?><methodResponse><params><param><value><string>ok</string></value></param></params></methodResponse>")));

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Discovery_Link_In_HTML()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse1.SetupGet(x => x.Headers).Returns(new System.Net.WebHeaderCollection { });
            string response_content = @"
...
<p>
<link rel=""pingback"" href=""http://target/pingback"">
</p>
...";
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(response_content)));

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/pingback", UriKind.Absolute))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "text/xml");
            webRequest2.SetupSet(x => x.ContentLength = 225);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\"?><methodResponse><params><param><value><string>ok</string></value></param></params></methodResponse>")));

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Discovery_Link_In_XHTML()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse1.SetupGet(x => x.Headers).Returns(new System.Net.WebHeaderCollection { });
            string response_content = @"
...
<p>
<link rel=""pingback"" href=""http://target/pingback"" />
</p>
...";
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(response_content)));

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/pingback", UriKind.Absolute))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "text/xml");
            webRequest2.SetupSet(x => x.ContentLength = 225);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\"?><methodResponse><params><param><value><string>ok</string></value></param></params></methodResponse>")));

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Response_Error()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.Headers).Returns(
                new System.Net.WebHeaderCollection {
                    {"X-Pingback", "http://target/pingback"}
                });

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/pingback", UriKind.Absolute))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "text/xml");
            webRequest2.SetupSet(x => x.ContentLength = 225);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\"?><methodResponse><fault><value><struct><member><name>faultCode</name><value><int>0</int></value></member><member><name>faultString</name><value><string>Error.</string></value></member></struct></value></fault></methodResponse>")));

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };
            
            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.Equal("Error.", result.Message);
        }

        [Fact]
        public void Send_Discovery_Response_Is_GZipped()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.Headers).Returns(new System.Net.WebHeaderCollection { });
            webResponse1.SetupGet(x => x.ContentEncoding).Returns("gzip");
            string response_content = @"
...
<p>
<link rel=""pingback"" href=""http://target/pingback"">
</p>
...";
            var response_content_bytes = new UTF8Encoding().GetBytes(response_content);
            var memoryStream = new MemoryStream();
            var gzippedStream = new GZipStream(memoryStream, CompressionMode.Compress, true);
            gzippedStream.Write(response_content_bytes, 0, response_content_bytes.Length);
            gzippedStream.Close();
            memoryStream.Position = 0;

            webResponse1.Setup(x => x.GetResponseStream()).Returns(memoryStream);

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/pingback", UriKind.Absolute))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "text/xml");
            webRequest2.SetupSet(x => x.ContentLength = 225);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\"?><methodResponse><params><param><value><string>ok</string></value></param></params></methodResponse>")));

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Send_Response_Is_GZipped()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.Headers).Returns(new System.Net.WebHeaderCollection { });
            webResponse1.SetupGet(x => x.ContentEncoding).Returns("gzip");
            string response_content = @"
...
<p>
<link rel=""pingback"" href=""http://target/pingback"">
</p>
...";
            var response_content_bytes = new UTF8Encoding().GetBytes(response_content);
            var memoryStream = new MemoryStream();
            var gzippedStream = new GZipStream(memoryStream, CompressionMode.Compress, true);
            gzippedStream.Write(response_content_bytes, 0, response_content_bytes.Length);
            gzippedStream.Close();
            memoryStream.Position = 0;

            webResponse1.Setup(x => x.GetResponseStream()).Returns(memoryStream);

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/pingback", UriKind.Absolute))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "text/xml");
            webRequest2.SetupSet(x => x.ContentLength = 225);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns("gzip");

            string responseContent = "<?xml version=\"1.0\"?><methodResponse><params><param><value><string>ok</string></value></param></params></methodResponse>";
            var responseContentBytes = new UTF8Encoding().GetBytes(responseContent);
            var responseMemoryStream = new MemoryStream();
            var gzippedResponseStream = new GZipStream(responseMemoryStream, CompressionMode.Compress, true);
            gzippedResponseStream.Write(responseContentBytes, 0, responseContentBytes.Length);
            gzippedResponseStream.Close();
            responseMemoryStream.Position = 0;

            webResponse2.Setup(x => x.GetResponseStream()).Returns(responseMemoryStream);

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void TargetUrl_Not_Specified()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.Headers).Returns(
                new System.Net.WebHeaderCollection {
                    {"X-Pingback", "http://target/pingback"}
                });

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/pingback", UriKind.Absolute))).Returns(webRequest2.Object);

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
            };

            ArgumentNullException ex = null;
            try
            {
                pingback.Send(url, parameters);
            }
            catch (ArgumentNullException _ex)
            {
                ex = _ex;
            }

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();

            Assert.NotNull(ex);
            Assert.Equal("TargetUrl", ex.ParamName);
        }

        [Fact]
        public void WebException_Saved()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Throws(new System.Net.WebException("WebException"));

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();

            Assert.False(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
            Assert.IsType<System.Net.WebException>(result.SendException);
        }

        [Fact]
        public void ProtocolViolationException_Saved()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Throws(new System.Net.ProtocolViolationException("ProtocolViolationException"));

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();

            Assert.False(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
            Assert.IsType<System.Net.ProtocolViolationException>(result.SendException);
        }

        [Fact]
        public void Discovery_Http_Error_Then_LinkbackSendException_Saved()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(false);

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();

            Assert.False(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
            Assert.IsType<LinkbackSendException>(result.SendException);
            Assert.Equal("Http error while discovering Pingback url for http://target/1", result.SendException.Message);
        }

        [Fact]
        public void Discovery_Both_Header_And_Link_Not_Found_Then_LinkbackSendException_Saved()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse1.SetupGet(x => x.Headers).Returns(new System.Net.WebHeaderCollection { });
            string response_content = "...";
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(response_content)));

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();

            Assert.False(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
            Assert.IsType<LinkbackSendException>(result.SendException);
            Assert.Equal("Pingback url discovering failed for http://target/1", result.SendException.Message);
        }

        [Fact]
        public void Http_Error_LinkbackSendExceptions_Saved()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.Headers).Returns(
                new System.Net.WebHeaderCollection {
                    {"X-Pingback", "http://target/pingback"}
                });

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/pingback", UriKind.Absolute))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "text/xml");
            webRequest2.SetupSet(x => x.ContentLength = 225);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(false);

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.False(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
            Assert.IsType<LinkbackSendException>(result.SendException);
            Assert.Equal("Http error while sending Pingback for http://target/pingback", result.SendException.Message);
        }

        [Fact]
        public void Empty_Response_LinkbackSendException_Saved()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.Headers).Returns(
                new System.Net.WebHeaderCollection {
                    {"X-Pingback", "http://target/pingback"}
                });

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/pingback", UriKind.Absolute))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "text/xml");
            webRequest2.SetupSet(x => x.ContentLength = 225);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream());

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.False(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
            Assert.IsType<LinkbackSendException>(result.SendException);
            Assert.Equal("Empty response received from http://target/pingback", result.SendException.Message);
        }

        [Fact]
        public void Invalid_Response_LinkbackSendException_Saved()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.Headers).Returns(
                new System.Net.WebHeaderCollection {
                    {"X-Pingback", "http://target/pingback"}
                });

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/pingback", UriKind.Absolute))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "text/xml");
            webRequest2.SetupSet(x => x.ContentLength = 225);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("... invalid response ...")));

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            var result = pingback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.False(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
            Assert.IsType<LinkbackSendException>(result.SendException);
            Assert.Equal("Invalid response received from http://target/pingback", result.SendException.Message);
        }

        [Fact]
        public void Unknown_Exceptions_Are_Not_Handled()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/1", UriKind.Absolute))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Throws(new InvalidOperationException());

            // Test

            var pingback = new Pingback(webRequest1.Object);

            var url = new Uri("http://target/1");

            var parameters = new LinkbackSendParameters
            {
                SourceUrl = new Uri("http://source/1"),
                TargetUrl = new Uri("http://target/1")
            };

            InvalidOperationException exception = null;

            try
            {
                pingback.Send(url, parameters);
            }
            catch (InvalidOperationException ex)
            {
                exception = ex;
            }

            // Verify

            Assert.NotNull(exception);
        }

        [Fact]
        public void LinkbackSendParameters_SetupRequestForPingback_SourceUrl_Null_Throws_InvalidOperationException()
        {
            var parameters = new LinkbackSendParameters();

            parameters.SourceUrl = null;
            parameters.TargetUrl = new Uri("http://localhost");

            var requestImplementation = new Mock<IHttpWebRequestImplementation>();
            var request = new HttpWebRequestAbstraction(requestImplementation.Object);

            Assert.Throws<InvalidOperationException>(() => {
                parameters.SetupRequestForPingback(request);
            });
        }
    }
}