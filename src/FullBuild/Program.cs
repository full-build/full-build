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
using FullBuild.Commands;
using FullBuild.NatLangParser;
using NLog;

namespace FullBuild
{
    internal class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static int Main(string[] args)
        {
            try
            {
                TryMain(args);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.Debug("Failed with error", ex);

                Console.Error.WriteLine("ERROR:");
                Console.Error.WriteLine(ex.Message);
            }

            return 5;
        }

        private static void TryMain(string[] args)
        {
            var parser = new ParserBuilder().With(Usage.Commands())
                                            .With(Workspace.Commands())
                                            .With(Packages.Commands())
                                            .With(Views.Commands())
                                            .With(Configuration.Commands())
                                            .With(Exec.Commands()).Build();

            if (! parser.ParseAndInvoke(args))
            {
                throw new ArgumentException("Invalid arguments. Use /? for usage.");
            }
        }
    }
}
