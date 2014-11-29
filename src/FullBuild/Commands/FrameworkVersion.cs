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

using System.Collections.Generic;

namespace FullBuild.Commands
{
    internal static class FrameworkVersion
    {
        public static string[] CompatibilityOrder =
        {
            "v1.0",
            "v1.1",
            "v2.0",
            "v3.5",
            "v4.0",
            "v4.5",
            "v4.5.1",
            "v4.5.2",
            "v4.5.3",
            "v4.5.4",
        };

        public static Dictionary<string, string[]> FxVersion2Folder = new Dictionary<string, string[]>
                                                                      {
                                                                          {"v1.0", new[] {"10"}},
                                                                          {"v1.1", new[] {"11", "net11"}},
                                                                          {"v2.0", new[] {"20", "net20", "net20-full", "net"}},
                                                                          {"v3.5", new[] {"35", "net35", "net35-full"}},
                                                                          {"v4.0", new[] {"40", "net4", "net40", "net40-full", "net40-client"}},
                                                                          {"v4.5", new[] {"45", "net45", "net45-full"}},
                                                                          {"v4.5.1", new[] {"net451"}},
                                                                          {"v4.5.2", new[] {"net452"}},
                                                                          {"v4.5.3", new[] {"net453"}},
                                                                          {"v4.5.4", new[] {"net454"}},
                                                                      };
    }
}
