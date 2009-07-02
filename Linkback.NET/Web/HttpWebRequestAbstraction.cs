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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace LinkbackNet.Web
{
    public class HttpWebRequestAbstraction
    {
        IHttpWebRequestImplementation Implementation;

        public HttpWebRequestAbstraction(IHttpWebRequestImplementation implementation)
        {
            this.Implementation = implementation;
        }

        public HttpWebRequestAbstraction Create(Uri url)
        {
            var requestImplementation = Implementation.Create(url);

            return new HttpWebRequestAbstraction(requestImplementation);
        }

        public string Method
        {
            get
            {
                return Implementation.Method;
            }
            set
            {
                Implementation.Method = value;
            }
        }

        public string ContentType
        {
            get
            {
                return Implementation.ContentType;
            }
            set
            {
                Implementation.ContentType = value;
            }
        }

        public long ContentLength
        {
            get
            {
                return Implementation.ContentLength;
            }
            set
            {
                Implementation.ContentLength = value;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public Stream GetRequestStream()
        {
            return Implementation.GetRequestStream();
        }

        public HttpWebResponseAbstraction GetResponse()
        {
            var responseImplementation = Implementation.GetResponse();

            return new HttpWebResponseAbstraction(responseImplementation);
        }
    }
}