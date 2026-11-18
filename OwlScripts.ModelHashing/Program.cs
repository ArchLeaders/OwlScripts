using System.Diagnostics;
using System.IO.Hashing;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BfresLibrary;
using CommunityToolkit.HighPerformance;
using McSharp;
using OwlScripts.Common;
using OwlScripts.Common.Models;
using OwlScripts.ModelHashing.Models;

var version = typeof(Program).Assembly
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
    .InformationalVersion.Split('+')[0];
Console.WriteLine($"""
    Owl Scripts [{version ?? "v0.0.0-dev"}]
    (c) Arch Leaders. MIT.

    """);

if (args is not [var outputJson, var outputBinary]) {
    Console.WriteLine("""
        USAGE:

        OwlScripts.BgymlKeyFinder <output-json> <output-binary>
        """);
    return;
}

if (Config.Shared.GameDumpFolderPath is null) {
Retry:
    Console.WriteLine("Please enter the path to your game dump: ");
    if (Console.ReadLine() is not { } dump || !Directory.Exists(dump)) {
        Console.WriteLine("Folder not found");
        goto Retry;
    }

    Config.Shared.GameDumpFolderPath = dump;
    Config.Save();
}

var romfs = new Romfs(Config.Shared.GameDumpFolderPath);
var context = new ScriptContext(FilesHandled: [], Hashes: []);

var startingTimestamp = Stopwatch.GetTimestamp();

romfs.EnumerateLite(CollectHashes, static (_, _) => true, context,
#if TOTK
    "*.bfres.mc"
#else
    "*.bfres"
#endif
);

var endingTimestamp = Stopwatch.GetTimestamp();
var elapsedTime = Stopwatch.GetElapsedTime(startingTimestamp, endingTimestamp);

Console.WriteLine($"Evaluated {context.FilesHandled.Count:N0} files in {elapsedTime.Seconds} seconds");

var options = new JsonSerializerOptions {
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var json = JsonSerializer.Serialize(
    context.Hashes
        .OrderBy(x => x.Key, StringComparer.Ordinal)
        .ToDictionary(),
    options);

File.WriteAllText(outputJson, json);

using var fs = File.Create(outputBinary);
using var bw = new BinaryWriter(fs);

var hashesSorted = context.Hashes
    .Select(kvp => (NameHash: XxHash3.HashToUInt64(kvp.Key.AsSpan().Cast<char, byte>()), kvp.Value))
    .OrderBy(x => x.NameHash)
    .ToArray();

foreach (var (nameHash, _) in hashesSorted) {
    bw.Write(nameHash);
}

foreach (var (_, hash) in hashesSorted) {
    bw.Write((long)hash);
}

return;

static void CollectHashes(RomfsFile file, ScriptContext context)
{
    if (file.FilePath is null) {
        return;
    }

    Console.WriteLine($"Collecting hashes for {file.CanonicalFilePath}");
    context.FilesHandled.Add(file.CanonicalFilePath);

    if (file.FileAttributes.HasFlag(RomfsFileAttributes.HasMcExtension) && MeshCodec.DecompressMc(file.Data) is { } decompressed) {
        using var ms = new MemoryStream(decompressed);
        CollectHashesFrom(ref file, new ResFile(ms, leaveOpen: true), context);
        return;
    }

    unsafe {
        fixed (byte* ptr = file.Data) {
            using var stream = new UnmanagedMemoryStream(ptr, file.Data.Length);
            CollectHashesFrom(ref file, new ResFile(stream, leaveOpen: true), context);
        }
    }
}

static void CollectHashesFrom(ref RomfsFile file, ResFile bfres, ScriptContext context)
{
    foreach (var (name, model) in bfres.Models) {
        context.Hashes[$"{file.CanonicalFilePath}/{name}"] = model.GetHashCode();

        foreach (var (materialName, material) in model.Materials) {
            context.Hashes[$"{file.CanonicalFilePath}/{name}/{materialName}"] = material.GetHashCode();
        }
    }
}