namespace OwlScripts.BgymlKeyFinder.Models;

public record BgymlEvaluationResult(
    HashSet<string> Files,
    Dictionary<string, List<string>> Arrays
);