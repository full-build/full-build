// Copyright (c) 2014, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using FullBuild.Helpers;
using FullBuild.Model;
using NLog;

namespace FullBuild.NuGet
{
    internal class NuGet1 : INuGet
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly Uri _url;

        internal NuGet1(string url)
        {
            _url = new Uri(url);
        }

        public NuSpec GetLatestVersion(string name)
        {
            using (var client = new WebClient())
            {
                var query = string.Format("Packages(Id='{0}',IsLatestVersion=true)", name);
                var uri = new Uri(_url, query);

                _logger.Debug("Querying latest version {0}", uri);

                var resp = client.DownloadString(uri);
                var xresp = XDocument.Parse(resp);

                var entries = from xentry in xresp.Descendants(XmlHelpers.Atom + "entry")
                              select NuSpec.CreateFromEntry(xentry);

                NuSpec entry = null;
                var max = DateTime.MinValue;
                foreach (var candidate in entries)
                {
                    if (max < candidate.Published && candidate.IsLatestVersion)
                    {
                        entry = candidate;
                    }
                }

                return entry;
            }
        }

        public NuSpec GetVersion(Package package)
        {
            using (var client = new WebClient())
            {
                var query = string.Format("Packages(Id='{0}',Version='{1}')", package.Name, package.Version);
                var uri = new Uri(_url, query);

                _logger.Debug("Querying version {0}", uri);

                var resp = client.DownloadString(uri);
                var xresp = XDocument.Parse(resp);

                var xentry = xresp.Descendants(XmlHelpers.Atom + "entry").SingleOrDefault();
                return null != xentry
                    ? NuSpec.CreateFromEntry(xentry)
                    : null;
            }
        }
    }
}
