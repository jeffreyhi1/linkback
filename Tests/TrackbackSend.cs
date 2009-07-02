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
    public class TrackbackSend
    {
        [Fact]
        public void RDF_Html()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            string rdf = String.Format(@"
...
<p>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:dc=""http://purl.org/dc/elements/1.1/""
         xmlns:trackback=""http://madskills.com/public/xml/rss/module/trackback/"">
    <rdf:Description rdf:about=""{0}""
                     dc:identifier=""{0}""
                     dc:title=""{1}""
                     trackback:ping=""{2}"" />
</rdf:RDF>
</p>
...", "http://target/post/1", "1", "http://target/post/1/trackback");
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(rdf)));
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 82);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>0</error></response>")));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.Equal("url=http%3A%2F%2Fsource%2F1&title=Source%20Title&excerpt=ABC&blog_name=Test%20Blog", new UTF8Encoding().GetString(requestStream.ToArray()));

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void RDF_Xhtml()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            string rdf = String.Format(@"
...
<p>
<!--<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:dc=""http://purl.org/dc/elements/1.1/""
         xmlns:trackback=""http://madskills.com/public/xml/rss/module/trackback/"">
    <rdf:Description rdf:about=""{0}""
                     dc:identifier=""{0}""
                     dc:title=""{1}""
                     trackback:ping=""{2}"" />
</rdf:RDF>-->
</p>
...", "http://target/post/1", "1", "http://target/post/1/trackback");
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(rdf)));
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 82);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>0</error></response>")));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.Equal("url=http%3A%2F%2Fsource%2F1&title=Source%20Title&excerpt=ABC&blog_name=Test%20Blog", new UTF8Encoding().GetString(requestStream.ToArray()));

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Multiple_RDF()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            string rdf = String.Format(@"
...
<p>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:dc=""http://purl.org/dc/elements/1.1/""
         xmlns:trackback=""http://madskills.com/public/xml/rss/module/trackback/"">
    <rdf:Description rdf:about=""{0}""
                     dc:identifier=""{0}""
                     dc:title=""{1}""
                     trackback:ping=""{2}"" />
</rdf:RDF>
</p>
...", "http://target/post/2", "2", "http://target/post/2/trackback") + String.Format(@"

...
<p>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:dc=""http://purl.org/dc/elements/1.1/""
         xmlns:trackback=""http://madskills.com/public/xml/rss/module/trackback/"">
    <rdf:Description rdf:about=""{0}""
                     dc:identifier=""{0}""
                     dc:title=""{1}""
                     trackback:ping=""{2}"" />
</rdf:RDF>
</p>
...", "http://target/post/1", "1", "http://target/post/1/trackback");
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(rdf)));
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 82);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>0</error></response>")));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.Equal("url=http%3A%2F%2Fsource%2F1&title=Source%20Title&excerpt=ABC&blog_name=Test%20Blog", new UTF8Encoding().GetString(requestStream.ToArray()));

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Empty_Response()
        {
            // Subtext 2.1.1.1 returns empty string ???

            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            string rdf = String.Format("... <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:trackback=\"http://madskills.com/public/xml/rss/module/trackback/\"><rdf:Description rdf:about=\"{0}\" dc:identifier=\"{0}\" dc:title=\"{1}\" trackback:ping=\"{2}\" /></rdf:RDF> ...", "http://target/post/1", "1", "http://target/post/1/trackback");
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(rdf)));
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 82);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.Setup(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream());

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

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

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            string rdf = String.Format("... <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:trackback=\"http://madskills.com/public/xml/rss/module/trackback/\"><rdf:Description rdf:about=\"{0}\" dc:identifier=\"{0}\" dc:title=\"{1}\" trackback:ping=\"{2}\" /></rdf:RDF> ...", "http://target/post/1", "1", "http://target/post/1/trackback");
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(rdf)));
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 82);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>1</error><message>Error</message></response>")));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.True(result.Success);
            Assert.Equal(1, result.Code);
            Assert.Equal("Error", result.Message);
        }

        [Fact]
        public void Discovery_Response_Is_GZipped()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns("gzip");
            string rdf = String.Format(@"
...
<p>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:dc=""http://purl.org/dc/elements/1.1/""
         xmlns:trackback=""http://madskills.com/public/xml/rss/module/trackback/"">
    <rdf:Description rdf:about=""{0}""
                     dc:identifier=""{0}""
                     dc:title=""{1}""
                     trackback:ping=""{2}"" />
</rdf:RDF>
</p>
...", "http://target/post/1", "1", "http://target/post/1/trackback");
            var rdf_bytes = new UTF8Encoding().GetBytes(rdf);
            var memoryStream = new MemoryStream();
            var gzippedStream = new GZipStream(memoryStream, CompressionMode.Compress, true);
            gzippedStream.Write(rdf_bytes, 0, rdf_bytes.Length);
            gzippedStream.Close();
            memoryStream.Position = 0;

            webResponse1.Setup(x => x.GetResponseStream()).Returns(memoryStream);
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 82);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>0</error></response>")));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.Equal("url=http%3A%2F%2Fsource%2F1&title=Source%20Title&excerpt=ABC&blog_name=Test%20Blog", new UTF8Encoding().GetString(requestStream.ToArray()));

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Response_Is_GZipped()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns("gzip");
            string rdf = String.Format(@"
...
<p>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:dc=""http://purl.org/dc/elements/1.1/""
         xmlns:trackback=""http://madskills.com/public/xml/rss/module/trackback/"">
    <rdf:Description rdf:about=""{0}""
                     dc:identifier=""{0}""
                     dc:title=""{1}""
                     trackback:ping=""{2}"" />
</rdf:RDF>
</p>
...", "http://target/post/1", "1", "http://target/post/1/trackback");
            var rdf_bytes = new UTF8Encoding().GetBytes(rdf);
            var memoryStream = new MemoryStream();
            var gzippedStream = new GZipStream(memoryStream, CompressionMode.Compress, true);
            gzippedStream.Write(rdf_bytes, 0, rdf_bytes.Length);
            gzippedStream.Close();
            memoryStream.Position = 0;

            webResponse1.Setup(x => x.GetResponseStream()).Returns(memoryStream);
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 82);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns("gzip");

            string responseContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>0</error></response>";
            var responseContentBytes = new UTF8Encoding().GetBytes(responseContent);
            var responseMemoryStream = new MemoryStream();
            var gzippedResponseStream = new GZipStream(responseMemoryStream, CompressionMode.Compress, true);
            gzippedResponseStream.Write(responseContentBytes, 0, responseContentBytes.Length);
            gzippedResponseStream.Close();
            responseMemoryStream.Position = 0;

            webResponse2.Setup(x => x.GetResponseStream()).Returns(responseMemoryStream);

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.Equal("url=http%3A%2F%2Fsource%2F1&title=Source%20Title&excerpt=ABC&blog_name=Test%20Blog", new UTF8Encoding().GetString(requestStream.ToArray()));

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Skip_Autodiscovery()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest1.Object);
            webRequest1.SetupSet(x => x.Method = "POST");
            webRequest1.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest1.SetupSet(x => x.ContentLength = 82);
            var requestStream = new MemoryStream();
            webRequest1.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>0</error></response>")));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1/trackback");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog",
                AutoDiscovery = false
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();

            Assert.Equal("url=http%3A%2F%2Fsource%2F1&title=Source%20Title&excerpt=ABC&blog_name=Test%20Blog", new UTF8Encoding().GetString(requestStream.ToArray()));

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Autodiscovery_Fail_Then_Try_Direct()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            //webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest1.Object);
            bool firstRequest = true;
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(false);
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(
                () => {
                    if (firstRequest) {
                        firstRequest = false;
                        return webRequest1.Object;
                    }
                    else {
                        return webRequest2.Object;
                    }
                });
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 82);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>0</error></response>")));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1/trackback");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.Equal("url=http%3A%2F%2Fsource%2F1&title=Source%20Title&excerpt=ABC&blog_name=Test%20Blog", new UTF8Encoding().GetString(requestStream.ToArray()));

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Title_Not_Specified()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            string rdf = String.Format(@"
...
<p>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:dc=""http://purl.org/dc/elements/1.1/""
         xmlns:trackback=""http://madskills.com/public/xml/rss/module/trackback/"">
    <rdf:Description rdf:about=""{0}""
                     dc:identifier=""{0}""
                     dc:title=""{1}""
                     trackback:ping=""{2}"" />
</rdf:RDF>
</p>
...", "http://target/post/1", "1", "http://target/post/1/trackback");
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(rdf)));
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 61);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>0</error></response>")));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.Equal("url=http%3A%2F%2Fsource%2F1&excerpt=ABC&blog_name=Test%20Blog", new UTF8Encoding().GetString(requestStream.ToArray()));

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Excerpt_Not_Specified()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            string rdf = String.Format(@"
...
<p>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:dc=""http://purl.org/dc/elements/1.1/""
         xmlns:trackback=""http://madskills.com/public/xml/rss/module/trackback/"">
    <rdf:Description rdf:about=""{0}""
                     dc:identifier=""{0}""
                     dc:title=""{1}""
                     trackback:ping=""{2}"" />
</rdf:RDF>
</p>
...", "http://target/post/1", "1", "http://target/post/1/trackback");
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(rdf)));
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 70);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>0</error></response>")));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.Equal("url=http%3A%2F%2Fsource%2F1&title=Source%20Title&blog_name=Test%20Blog", new UTF8Encoding().GetString(requestStream.ToArray()));

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void BlogName_Not_Specified()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            string rdf = String.Format(@"
...
<p>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:dc=""http://purl.org/dc/elements/1.1/""
         xmlns:trackback=""http://madskills.com/public/xml/rss/module/trackback/"">
    <rdf:Description rdf:about=""{0}""
                     dc:identifier=""{0}""
                     dc:title=""{1}""
                     trackback:ping=""{2}"" />
</rdf:RDF>
</p>
...", "http://target/post/1", "1", "http://target/post/1/trackback");
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(rdf)));
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 60);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>0</error></response>")));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.Equal("url=http%3A%2F%2Fsource%2F1&title=Source%20Title&excerpt=ABC", new UTF8Encoding().GetString(requestStream.ToArray()));

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void WebException_Saved()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Throws(new System.Net.WebException("WebException"));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

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

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Throws(new System.Net.ProtocolViolationException("ProtocolViolationException"));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();

            Assert.False(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
            Assert.IsType<System.Net.ProtocolViolationException>(result.SendException);
        }

        [Fact]
        public void Autodiscovery_Http_Error_But_Autodiscovery_Is_True_Then_LinkbackSendException_Saved()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(false);
            webResponse1.Setup(x => x.Close());

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog",
                AutoDiscovery = true
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();

            Assert.False(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
            Assert.IsType<LinkbackSendException>(result.SendException);
            Assert.Equal("Http error while discovering Trackback url for http://target/post/1", result.SendException.Message);
        }

        [Fact]
        public void Autodiscovery_RDF_Not_Found_But_Autodiscovery_Is_True_Then_LinkbackSendException_Saved()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            string rdf = "... there are no any RDFs ... ";
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(rdf)));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog",
                AutoDiscovery = true
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();

            Assert.False(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
            Assert.IsType<LinkbackSendException>(result.SendException);
            Assert.Equal("RDF not found while discovering Trackback url for http://target/post/1", result.SendException.Message);
        }

        [Fact]
        public void Autodiscovery_RDF_Not_Found_But_Autodiscovery_Not_Specified_Try_Direct()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            bool firstRequest = true;
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);
            webResponse1.Setup(x => x.Close());
            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            string rdf = "... there are no any RDFs ... ";
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(rdf)));

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(
                () => {
                    if (firstRequest) {
                        firstRequest = false;
                        return webRequest1.Object;
                    }
                    else {
                        return webRequest2.Object;
                    }
                });
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 82);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><response><error>0</error></response>")));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1/trackback");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.Equal("url=http%3A%2F%2Fsource%2F1&title=Source%20Title&excerpt=ABC&blog_name=Test%20Blog", new UTF8Encoding().GetString(requestStream.ToArray()));

            Assert.True(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
        }

        [Fact]
        public void Http_Error_LinkbackSendExceptions_Saved()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            string rdf = String.Format("... <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:trackback=\"http://madskills.com/public/xml/rss/module/trackback/\"><rdf:Description rdf:about=\"{0}\" dc:identifier=\"{0}\" dc:title=\"{1}\" trackback:ping=\"{2}\" /></rdf:RDF> ...", "http://target/post/1", "1", "http://target/post/1/trackback");
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(rdf)));
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 82);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(false);

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.False(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
            Assert.IsType<LinkbackSendException>(result.SendException);
            Assert.Equal("Http error while sending Trackback for http://target/post/1/trackback", result.SendException.Message);
        }

        [Fact]
        public void Invalid_Response_LinkbackSendException_Saved()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Returns(webResponse1.Object);

            webResponse1.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse1.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            string rdf = String.Format("... <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:trackback=\"http://madskills.com/public/xml/rss/module/trackback/\"><rdf:Description rdf:about=\"{0}\" dc:identifier=\"{0}\" dc:title=\"{1}\" trackback:ping=\"{2}\" /></rdf:RDF> ...", "http://target/post/1", "1", "http://target/post/1/trackback");
            webResponse1.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes(rdf)));
            webResponse1.Setup(x => x.Close());

            var webRequest2 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse2 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1/trackback"))).Returns(webRequest2.Object);
            webRequest2.SetupSet(x => x.Method = "POST");
            webRequest2.SetupSet(x => x.ContentType = "application/x-www-form-urlencoded");
            webRequest2.SetupSet(x => x.ContentLength = 82);
            var requestStream = new MemoryStream();
            webRequest2.Setup(x => x.GetRequestStream()).Returns(requestStream);
            webRequest2.Setup(x => x.GetResponse()).Returns(webResponse2.Object);

            webResponse2.Setup(x => x.Close());
            webResponse2.SetupGet(x => x.IsHttpStatusCode2XX).Returns(true);
            webResponse2.SetupGet(x => x.ContentEncoding).Returns(String.Empty);
            webResponse2.Setup(x => x.GetResponseStream()).Returns(new MemoryStream(new UTF8Encoding().GetBytes("... invalid response ...")));

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            var result = trackback.Send(url, parameters);

            // Verify

            webRequest1.VerifyAll();
            webResponse1.VerifyAll();
            webRequest2.VerifyAll();
            webResponse2.VerifyAll();

            Assert.False(result.Success);
            Assert.Equal(0, result.Code);
            Assert.True(String.IsNullOrEmpty(result.Message));
            Assert.IsType<LinkbackSendException>(result.SendException);
            Assert.Equal("Invalid response received from http://target/post/1/trackback", result.SendException.Message);
        }

        [Fact]
        public void Unknown_Exceptions_Are_Not_Handled()
        {
            // Setup

            var webRequest1 = new Mock<IHttpWebRequestImplementation>(MockBehavior.Strict);
            var webResponse1 = new Mock<IHttpWebResponseImplementation>(MockBehavior.Strict);

            webRequest1.Setup(x => x.Create(new Uri("http://target/post/1"))).Returns(webRequest1.Object);
            webRequest1.Setup(x => x.GetResponse()).Throws(new InvalidOperationException());

            // Test

            var trackback = new Trackback(webRequest1.Object);

            var url = new Uri("http://target/post/1");

            var parameters = new LinkbackSendParameters
            {
                Title = "Source Title",
                Excerpt = "ABC",
                Url = new Uri("http://source/1"),
                BlogName = "Test Blog"
            };

            InvalidOperationException exception = null;

            try
            {
                trackback.Send(url, parameters);
            }
            catch (InvalidOperationException ex)
            {
                exception = ex;
            }

            // Verify

            Assert.NotNull(exception);
        }

        [Fact]
        public void LinkbackSendParameters_SetupRequestForTrackback_Url_Null_Throws_InvalidOperationException()
        {
            var parameters = new LinkbackSendParameters();

            var requestImplementation = new Mock<IHttpWebRequestImplementation>();
            var request = new HttpWebRequestAbstraction(requestImplementation.Object);

            Assert.Throws<InvalidOperationException>(() =>
            {
                parameters.SetupRequestForTrackback(request);
            });
        }
    }
}