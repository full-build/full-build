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

using FullBuild.Commands;
using NFluent;
using NUnit.Framework;

namespace FullBuild.Test.Commands
{
    [TestFixture]
    public class HintPathParserTests
    {
        [Test]
        public void Parse_succeed_with_expected_hintpath()
        {
            const string hintPath = "../packages/Connectivity.Services.Model.1.1.24/lib/net45/Connectivity.Services.Model.dll";
            var package = HintPathParser.ExtractPackageNameFromPackagePlusVersion(hintPath);
            Check.That(package.Name).IsEqualTo("Connectivity.Services.Model");
            Check.That(package.Version).IsEqualTo("1.1.24");
        }

        [Test]
        public void Parse_succeed_with_missing_version()
        {
            const string hintPath = "../packages/Microsoft.Data.Services.Client/lib/portable-net45+wp8+win8+wpa/zh-Hant/Microsoft.Data.Services.Client.resources.dll";
            var package = HintPathParser.ExtractPackageNameFromPackagePlusVersion(hintPath);
            Check.That(package.Name).IsEqualTo("Microsoft.Data.Services.Client");
            Check.That(package.Version).IsNull();
        }

        [Test]
        public void Parse_succeed_with_expected_hintpath_with_beta()
        {
            const string hintPath = "../packages/Connectivity.Services.Model.1.1.24-beta2/lib/net45/Connectivity.Services.Model.dll";
            var package = HintPathParser.ExtractPackageNameFromPackagePlusVersion(hintPath);
            Check.That(package.Name).IsEqualTo("Connectivity.Services.Model");
            Check.That(package.Version).IsEqualTo("1.1.24-beta2");
        }

        [Test]
        public void Parse_fail_when_invalid_version()
        {
            const string hintPath = "../packages/Connectivity.Services.Model.42bb/lib/net45/Connectivity.Services.Model.dll";
            var package = HintPathParser.ExtractPackageNameFromPackagePlusVersion(hintPath);
            Check.That(package).IsNull();
        }

        [Test]
        public void Parse_fail_when_non_nuget_path()
        {
            const string hintPath = "../../References/Adomd.net/100/Microsoft.AnalysisServices.AdomdClient.dll";
            var package = HintPathParser.ExtractPackageNameFromPackagePlusVersion(hintPath);
            Check.That(package).IsNull();
        }
    }
}
