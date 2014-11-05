namespace FullBuildInterface.NatLangParser
{
    public static class MatchBuilder
    {
        public static FluentMatchBuilder Describe(string description)
        {
            return new FluentMatchBuilder(description);
        }
    }
}