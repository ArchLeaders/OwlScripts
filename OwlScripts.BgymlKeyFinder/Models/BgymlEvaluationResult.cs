namespace OwlScripts.BgymlKeyFinder.Models;

public record BgymlEvaluationResult(
    Dictionary<string, string?> Files,
    Dictionary<string, List<string>> Arrays
);