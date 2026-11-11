using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using BymlLibrary;
using BymlLibrary.Extensions;
using BymlLibrary.Nodes.Immutable.Containers;
using OwlScripts.BgymlKeyFinder.Models;
using OwlScripts.Common;
using OwlScripts.Common.Extensions;
using OwlScripts.Common.Models;
using Revrs;

var version = typeof(Program).Assembly
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
    .InformationalVersion.Split('+')[0];
Console.WriteLine($"""
    Owl Scripts [{version ?? "v0.0.0-dev"}]
    (c) Arch Leaders. MIT.

    """);

if (args is not [var gameDumpFolderPath, var output]) {
    Console.WriteLine("""
        USAGE:

        OwlScripts.BgymlKeyFinder <game-path> <output>
        """);
    return;
}

var romfs = new Romfs(gameDumpFolderPath);
var context = new ScriptContext(ProcessedFiles: [], EvaluationResults: []);

var startingTimestamp = Stopwatch.GetTimestamp();

// GOAL: Find a property that is present in every BYML Map Array (with a given path and BGYML type)
romfs.EnumerateLite(EvaluateFile,
    (file, ctx) => Path.GetExtension(file.CanonicalFilePath) is ".pack" or ".bgyml" or ".byml" &&
                   ctx.ProcessedFiles.Add(file.CanonicalFilePath),
    context, "*.pack.zs", "*.bgyml", "*.bcett.byml.zs"
);

var endingTimestamp = Stopwatch.GetTimestamp();
var elapsedTime = Stopwatch.GetElapsedTime(startingTimestamp, endingTimestamp);

Console.WriteLine($"Evaluated {context.ProcessedFiles.Count:N0} files in {elapsedTime.Seconds} seconds");

var options = new JsonSerializerOptions {
    WriteIndented = true
};

var json = JsonSerializer.Serialize(context.EvaluationResults, options);
File.WriteAllText(output, json);

return;

static void EvaluateFile(RomfsFile file, ScriptContext context)
{
    var bgymlTypeName = file.CanonicalFilePath.GetBgymlTypeName().ToString();
    Console.WriteLine($"Read {file.CanonicalFilePath} [{bgymlTypeName}, {file.Data.Length}]");

    var reader = new RevrsReader(file.Data);
    var byml = new ImmutableByml(ref reader);

    if (byml.Type is not BymlNodeType.Map) {
        return;
    }

    ref var evaluation = ref CollectionsMarshal.GetValueRefOrAddDefault(context.EvaluationResults, bgymlTypeName, out _);
    evaluation ??= new BgymlEvaluationResult(bgymlTypeName, file.CanonicalFilePath, Arrays: []);
    
    EvaluateBymlMap(ref byml, byml.GetMap(), context, evaluation);
}

static void EvaluateBymlMap(ref ImmutableByml root, ImmutableBymlMap map, ScriptContext context, BgymlEvaluationResult evaluation)
{
    foreach (var (key, child) in map) {
        if (child.Type is BymlNodeType.Map) {
            EvaluateBymlMap(ref root, child.GetMap(), context, evaluation);
        }

        if (child.Type is BymlNodeType.Array) {
            ref var keys = ref CollectionsMarshal.GetValueRefOrAddDefault(
                evaluation.Arrays, root.KeyTable[key].ToManaged(), out bool exists);
            EvaluateBymlArray(ref root, child.GetArray(), context, evaluation, ref keys);
        }
    }
}

static void EvaluateBymlArray(ref ImmutableByml root, ImmutableBymlArray array, ScriptContext context, BgymlEvaluationResult evaluation, ref List<string>? keys)
{
    if (array.Count == 0 || array[0].Type is not BymlNodeType.Map) {
        return;
    }

    int startIndex = 0;
    
    if (keys is null) {
        var first = array[0].GetMap();
        keys = new List<string>(first.Count);

        foreach (var (keyIndex, value) in first) {
            if (EvaluateMapElement(ref root, keyIndex, value, context, evaluation) is { } key) {
                keys.Add(key);
            }
        }
        
        startIndex = 1;
    }
    
    for (int arrayIndex = startIndex; arrayIndex < array.Count; arrayIndex++) {
        var node = array[arrayIndex];

        if (node.Type is not BymlNodeType.Map) {
            // TODO: Flag that this keyed-array is not only maps
            return;
        }

        var map = node.GetMap();

        for (int i = 0; i < keys.Count;) {
            bool exists = false;
            foreach (var (keyIndex, value) in map) {
                if (EvaluateMapElement(ref root, keyIndex, value, context, evaluation) is not { } key) {
                    continue;
                }
                
                exists = key == keys[i];
            }

            if (exists) {
                i++;
                continue;
            }
            
            keys.RemoveAt(i);
        }
    }
}

static string? EvaluateMapElement(ref ImmutableByml root, int key, ImmutableByml value, ScriptContext context, BgymlEvaluationResult evaluation)
{
    // Recursively search maps
    if (value.Type is BymlNodeType.Map) {
        EvaluateBymlMap(ref root, value.GetMap(), context, evaluation);
        return null;
    }
            
    // Skip node types that can never make a useful ID
    if (!value.Type.IsValueType() ||
        value.Type is BymlNodeType.Double or BymlNodeType.Float or BymlNodeType.Bool or BymlNodeType.Null) {
        return null;
    }
            
    return root.KeyTable[key].ToManaged();
}