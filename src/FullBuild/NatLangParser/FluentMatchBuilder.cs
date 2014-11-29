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

namespace FullBuild.NatLangParser
{
    public class FluentMatchBuilder
    {
        private readonly object[] _args;

        private readonly string _description;

        private readonly List<KeyValuePair<string, IMatchOperation>> _operations = new List<KeyValuePair<string, IMatchOperation>>();

        public FluentMatchBuilder(string description, params object[] args)
        {
            _description = description;
            _args = args;
        }

        private void AddOperation(string name, IMatchOperation operation)
        {
            var kvp = new KeyValuePair<string, IMatchOperation>(name, operation);
            _operations.Add(kvp);
        }

        public FluentMatchBuilder Param<T>(Parameter<T> parameter)
        {
            IMatchOperation operation;
            if (typeof(T).IsArray)
            {
                var matchOpAggType = typeof(MatchOperationAggregate<>);
                var matchOpAggOfTtype = matchOpAggType.MakeGenericType(typeof(T).GetElementType());
                operation = (IMatchOperation)Activator.CreateInstance(matchOpAggOfTtype);
            }
            else
            {
                operation = new MatchOperation<T>();
            }

            AddOperation(parameter.Name, operation);

            return this;
        }

        public FluentMatchBuilder Command(string text)
        {
            var operation = new MatchOperationText(text);
            AddOperation(null, operation);

            return this;
        }

        public Matcher Do(Action<Context> action)
        {
            return new Matcher(_description, _args, _operations.AsEnumerable(), action);
        }
    }
}
