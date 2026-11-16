using BymlLibrary;
using Revrs;
using Revrs.Extensions;
using SarcLibrary;

namespace OwlScripts.BgymlKeyManager.Models;

/// <summary>
/// 
/// </summary>
/// <param name="CanonicalFilePath"></param>
/// <param name="FilePath">The absolute path to the bgyml or containing sarc file</param>
public sealed record BgymlFile(string CanonicalFilePath, string FilePath)
{
    public string GetYaml()
    {
        using var fs = File.OpenRead(FilePath);
        using var decompressed = App.Romfs.GetDecompressed(fs);
        
        if (decompressed.Span.Read<uint>() == Sarc.MAGIC) {
            RevrsReader sarcReader = new(decompressed.Span);
            ImmutableSarc sarc = new(ref sarcReader);
            return GetYaml(sarc[CanonicalFilePath].Data);
        }
        
        return GetYaml(decompressed.Span);
    }

    private static string GetYaml(Span<byte> data)
    {
        RevrsReader reader = new(data);
        ImmutableByml byml = new(ref reader);
        return byml.ToYaml();
    }
}