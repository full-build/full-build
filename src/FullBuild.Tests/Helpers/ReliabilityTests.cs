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
using FullBuild.Helpers;
using NFluent;
using NUnit.Framework;

namespace FullBuild.Test.Helpers
{
    [TestFixture]
    public class ReliabilityTests
    {
        [Test]
        public void Check_default_retry_count_max()
        {
            var callCount = 0;
            Action action = () =>
                            {
                                ++callCount;
                                throw new ApplicationException("Error !");
                            };

            Check.ThatCode(() => Reliability.Do(action)).Throws<ApplicationException>();
            Check.That(callCount).IsEqualTo(3);
        }

        [Test]
        public void Check_failure_if_lower_than_zero_try_count()
        {
            var callCount = 0;
            Action action = () =>
                            {
                                ++callCount;
                                throw new ApplicationException("Error !");
                            };

            Check.ThatCode(() => Reliability.Do(action, 0)).Throws<ArgumentException>();
            Check.That(callCount).IsEqualTo(0);
        }

        [Test]
        public void Check_specified_retry_count_max()
        {
            var callCount = 0;
            Action action = () =>
                            {
                                ++callCount;
                                throw new ApplicationException("Error !");
                            };

            Check.ThatCode(() => Reliability.Do(action, 2)).Throws<ApplicationException>();
            Check.That(callCount).IsEqualTo(2);
        }
    }
}
