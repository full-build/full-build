namespace FullBuildInterface.NatLangParser
{
    public class MatchBuilder
    {
        private MatchBuilder()
        {
        }

        public static FluentMatchBuilder Match<T>(string name)
        {
            var builder = new FluentMatchBuilder();
            return builder.Match<T>(name);
        }

        public static FluentMatchBuilder Text(string text)
        {
            var builder = new FluentMatchBuilder();
            return builder.Text(text);
        }
    }
}