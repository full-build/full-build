using System;

namespace FullBuild.NatLangParser
{
    public class MatchOperation<T> : IMatchOperation
    {
        private T _value;

        public bool TryParse(string input)
        {
            try
            {
                _value = (T)Convert.ChangeType(input, typeof(T));
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
            get { return _value; }
        }

        public string Describe
        {
            get { return typeof(T).Name; }
        }

        public bool IsAccumulator
        {
            get { return false; }
        }
    }
}