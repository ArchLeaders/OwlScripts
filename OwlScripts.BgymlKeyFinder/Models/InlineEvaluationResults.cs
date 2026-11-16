namespace OwlScripts.BgymlKeyFinder.Models;

public record struct InlineEvaluationResults(bool FoundArrays)
{
    public static readonly InlineEvaluationResults Default = default;
}