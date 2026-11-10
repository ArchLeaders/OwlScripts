namespace OwlScripts.BgymlKeyFinder.Models;

public record BgymlEvaluationResult(
    string Type,
    string CanonicalFileName,
    Dictionary<string, List<string>> Arrays
);