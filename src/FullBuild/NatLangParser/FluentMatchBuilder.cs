using System;
using System.Collections.Generic;
using System.Linq;

namespace FullBuild.NatLangParser
{
    public class FluentMatchBuilder
    {
        private readonly string _description;

        private readonly List<KeyValuePair<string, IMatchOperation>> _operations = new List<KeyValuePair<string, IMatchOperation>>();

        public FluentMatchBuilder(string description)
        {
            _description = description;
        }

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
            return new Matcher(_description, _operations.AsEnumerable(), prms => action.DynamicInvoke(prms));
        }

        public Matcher Do<T0>(Action<T0> action)
        {

            return new Matcher(_description, _operations.AsEnumerable(), prms => action.DynamicInvoke(prms));
        }

        public Matcher Do<T0, T1>(Action<T0, T1> action)
        {
            return new Matcher(_description, _operations.AsEnumerable(), prms => action.DynamicInvoke(prms));
        }
    }
}