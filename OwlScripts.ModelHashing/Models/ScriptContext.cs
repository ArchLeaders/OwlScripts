namespace OwlScripts.ModelHashing.Models;

public record ScriptContext(
    List<string> FilesHandled,
    Dictionary<string, int> Hashes
);