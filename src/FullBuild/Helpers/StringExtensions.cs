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
using System.Collections.Generic;
using System.Linq;

namespace FullBuild.Helpers
{
    public static class StringExtensions
    {
        public static bool InvariantEquals(this string @this, string other)
        {
            return 0 == string.Compare(@this, other, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool InvariantStartsWith(this string @this, string what)
        {
            return @this.StartsWith(what, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool InvariantContains(this string @this, string what)
        {
            return -1 != @this.InvariantIndexOf(what);
        }

        public static int InvariantIndexOf(this string @this, string what)
        {
            return @this.IndexOf(what, StringComparison.InvariantCultureIgnoreCase);
        }

        public static int InvariantFirstIndexOf(this string @this, string[] whats, int startIndex)
        {
            if (null != whats)
            {
                var idxs = new List<int>();
                foreach (var what in whats)
                {
                    var idx = @this.IndexOf(what, startIndex, StringComparison.InvariantCultureIgnoreCase);
                    if (-1 != idx)
                    {
                        idxs.Add(idx);
                    }
                }

                if (idxs.Any())
                {
                    return idxs.Min();
                }
            }

            return -1;
        }

        public static string ToUnixSeparator(this string @this)
        {
            return @this.Replace('\\', '/');
        }

        public static string ToMsBuild(this string @this)
        {
            return @this.Replace("-", "_").Replace(".", "_");
        }

        public static string ToCamelCase(this string @this)
        {
            return @this.Substring(0, 1).ToLowerInvariant() + @this.Substring(1);
        }
    }
}
