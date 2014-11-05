namespace FullBuildInterface.NatLangParser
{
    public interface IMatchOperation
    {
        bool HasValue { get; }

        object Value { get; }

        string Describe { get; }

        bool IsAccumulator { get; }

        bool TryParse(string input);
    }
}