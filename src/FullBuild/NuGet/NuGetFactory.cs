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
using FullBuild.Commands;
using FullBuild.Config;
using FullBuild.Helpers;

namespace FullBuild.NuGet
{
    internal class NuGetFactory
    {
        public static INuGet CreateAll(params NuGetConfig[] nugets)
        {
            return new NuGetAll(nugets);
        }

        public static INuGet Create(NuGetConfig nuget)
        {
            switch (nuget.Version)
            {
                case 1:
                    return new NuGet1(nuget.Url);

                case 2:
                    return new NuGet2(nuget.Url);

                default:
                    throw new ArgumentException("Unsupported NuGet version");
            }
        }

        public static int GetNuGetVersion(string url)
        {
            using (var webClient = new WebClient())
            {
                var uri = new Uri(new Uri(url), "$metadata");
                var resp = webClient.DownloadString(uri);
                var xresp = XDocument.Parse(resp);
                var entityType = xresp.Descendants(XmlHelpers.Edm + "EntityType").FirstOrDefault();
                if (null == entityType)
                {
                    throw new ApplicationException("Can't determine NuGet feed version");
                }

                var entityName = entityType.Attribute("Name").Value;
                switch (entityName)
                {
                    case "Package":
                        return 1;

                    case "V2FeedPackage":
                        return 2;

                    default:
                        throw new ApplicationException("Can't determine NuGet version");
                }
            }
        }
    }
}
