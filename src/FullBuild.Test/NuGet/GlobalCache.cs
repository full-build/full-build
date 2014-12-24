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
using System.IO;
using FullBuild.Helpers;
using FullBuild.Model;
using FullBuild.NuGet;
using NFluent;
using NUnit.Framework;

namespace FullBuild.Test.NuGet
{
    [TestFixture]
    public class GlobalCacheTests
    {
        [Test]
        public void Ensure_failure_if_package_is_corrupt()
        {
            using (var cacheDir = new TemporaryDirectory())
            {
                using (var pkgDir = new TemporaryDirectory())
                {
                    var package = new Package("Castle.Core", "3.3.3");

                    Check.That(Directory.EnumerateFiles(cacheDir.Directory.FullName, "*.*", SearchOption.AllDirectories)).IsEmpty();
                    Check.That(Directory.EnumerateFiles(pkgDir.Directory.FullName, "*.*", SearchOption.AllDirectories)).IsEmpty();

                    var castlePkg = cacheDir.Directory.GetFile("Castle.Core.3.3.3.nupkg");
                    using (File.Open(castlePkg.FullName, FileMode.Create, FileAccess.Write))
                    {
                    }

                    Check.That(new FileInfo(castlePkg.FullName).Length).IsEqualTo(0);
                    Check.That(GlobalCache.IsPackageInCache(package, cacheDir.Directory)).IsTrue();
                    Check.ThatCode(() => GlobalCache.InstallPackageFromCache(package, cacheDir.Directory, pkgDir.Directory)).Throws<Exception>();
                    Check.That(GlobalCache.IsPackageInCache(package, cacheDir.Directory)).IsFalse();
                }
            }
        }
    }
}
