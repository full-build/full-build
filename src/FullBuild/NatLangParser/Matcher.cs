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
using System.Text;

namespace FullBuild.NatLangParser
{
    public class Matcher
    {
        private readonly Action<Context> _action;

        private readonly object[] _args;

        private readonly string _description;

        private readonly KeyValuePair<string, IMatchOperation>[] _operations;

        public Matcher(string description, object[] args, IEnumerable<KeyValuePair<string, IMatchOperation>> operations, Action<Context> action)
        {
            _operations = operations.ToArray();
            _description = description;
            _args = args;
            _action = action;
        }

        public bool ParseAndInvoke(string[] args, Context context)
        {
            var isMatch = true;
            var argIndex = 0;
            var opIndex = 0;
            IMatchOperation op = null;
            string key = null;
            while (isMatch && argIndex < args.Length && opIndex < _operations.Length)
            {
                var arg = args[argIndex];
                key = _operations[opIndex].Key;
                op = _operations[opIndex].Value;
                isMatch = op.TryParse(arg);

                if (! op.IsAccumulator)
                {
                    ++opIndex;
                    if (isMatch && null != key)
                    {
                        context = context.Add(key, op.Value);
                    }
                }

                ++argIndex;
            }

            if (null != op && op.IsAccumulator)
            {
                context = context.Add(key, op.Value);
                ++opIndex;
            }

            if (! isMatch || argIndex != args.Length || opIndex != _operations.Length)
            {
                return false;
            }

            _action(context);
            return true;
        }

        public string Usage()
        {
            var res = _operations.Aggregate(new StringBuilder(), (sb, op) =>
                                                                 {
                                                                     if (null != op.Key)
                                                                     {
                                                                         sb.AppendFormat("<{0}:{1}>", op.Key, op.Value.Describe);
                                                                     }
                                                                     else
                                                                     {
                                                                         sb.Append(op.Value.Describe);
                                                                     }

                                                                     return sb.Append(" ");
                                                                 });

            res.AppendFormat(": {0}", string.Format(_description, _args) + ".");

            return res.ToString();
        }
    }
}
