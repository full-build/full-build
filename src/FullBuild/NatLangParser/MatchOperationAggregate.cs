using System;
using System.Collections.Generic;

namespace FullBuildInterface.NatLangParser
{
    public class MatchOperationAggregate<T> : IMatchOperation
    {
        private readonly List<T> _value = new List<T>();

        public bool TryParse(string input)
        {
            try
            {
                var value = (T)Convert.ChangeType(input, typeof(T));
                _value.Add(value);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool HasValue
        {
            get { return true; }
        }

        public object Value
        {
            get { return _value.ToArray(); }
        }

        public string Describe
        {
            get { return string.Format("{0} ...", typeof(T).Name); }
        }

        public bool IsAccumulator
        {
            get { return true; }
        }
    }
}