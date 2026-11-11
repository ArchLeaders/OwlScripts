namespace OwlScripts.BgymlKeyFinder.Models;

public record BgymlEvaluationResult(
    string Type,
    HashSet<string> Files,
    Dictionary<string, List<string>> Arrays
);