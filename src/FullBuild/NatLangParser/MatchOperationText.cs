namespace FullBuild.NatLangParser
{
    public class MatchOperationText : IMatchOperation
    {
        private readonly string _text;

        public MatchOperationText(string text)
        {
            _text = text;
        }

        public bool TryParse(string input)
        {
            var res = _text.InvariantEquals(input);
            return res;
        }

        public bool HasValue
        {
            get { return false; }
        }

        public object Value
        {
            get { return _text; }
        }

        public string Describe
        {
            get { return _text; }
        }

        public bool IsAccumulator
        {
            get { return false; }
        }
    }
}