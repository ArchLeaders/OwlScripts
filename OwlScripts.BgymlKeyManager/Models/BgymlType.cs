using System.Collections.Immutable;
using BymlLibrary;
using Revrs;
using Revrs.Extensions;
using SarcLibrary;

namespace OwlScripts.BgymlKeyManager.Models;

public sealed class BgymlType : BgymlEntity
{
    public ImmutableArray<BgymlArray> Arrays { get; set; }

    public required List<BgymlFile> Files { get; set; }
    
    public BgymlFile? GetFileAfter(BgymlFile? currentBgymlFile)
    {
        if (currentBgymlFile is null) {
            return Files.FirstOrDefault();
        }

        if (Files.IndexOf(currentBgymlFile) is var index and > -1) {
            return ++index >= Files.Count ? Files.FirstOrDefault() : Files[index];
        }

        return null;
    }
    
    public BgymlFile? GetFileBefore(BgymlFile? currentBgymlFile)
    {
        if (currentBgymlFile is null) {
            return Files.LastOrDefault();
        }

        if (Files.IndexOf(currentBgymlFile) is var index and > -1) {
            return --index < 0 ? Files.LastOrDefault() : Files[index];
        }

        return null;
    }
}