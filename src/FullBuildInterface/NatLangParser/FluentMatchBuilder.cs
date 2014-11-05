﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace FullBuildInterface.NatLangParser
{
    public class FluentMatchBuilder
    {
        private readonly List<KeyValuePair<string, IMatchOperation>> _operations = new List<KeyValuePair<string, IMatchOperation>>();

        private void AddOperation(string name, IMatchOperation operation)
        {
            var kvp = new KeyValuePair<string, IMatchOperation>(name, operation);
            _operations.Add(kvp);
        }

        public FluentMatchBuilder Match<T>(string name)
        {
            var operation = new MatchOperation<T>();
            AddOperation(name, operation);

            return this;
        }

        public FluentMatchBuilder MatchAggregate<T>(string name)
        {
            var operation = new MatchOperationAggregate<T>();
            AddOperation(name, operation);

            return this;
        }

        public FluentMatchBuilder Text(string text)
        {
            var operation = new MatchOperationText(text);
            AddOperation(null, operation);

            return this;
        }

        public Matcher Do(Action action)
        {
            return new Matcher(_operations.AsEnumerable(), prms => action.DynamicInvoke(prms));
        }

        public Matcher Do<T0>(Action<T0> action)
        {

            return new Matcher(_operations.AsEnumerable(), prms => action.DynamicInvoke(prms));
        }

        public Matcher Do<T0, T1>(Action<T0, T1> action)
        {
            return new Matcher(_operations.AsEnumerable(), prms => action.DynamicInvoke(prms));
        }
    }
}