namespace OwlScripts.BgymlKeyFinder.Models;

public sealed record ScriptContext(
    HashSet<string> ProcessedFiles,
    Dictionary<string, BgymlEvaluationResult> EvaluationResults
);