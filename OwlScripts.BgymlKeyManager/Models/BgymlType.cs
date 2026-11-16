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
    
    public BgymlFile? GetFileAfter(BgymlFile? currentBgymlFile, out int resultIndex)
    {
        resultIndex = Files.Count > 0 ? 0 : -1;
        
        if (currentBgymlFile is null) {
            return Files.FirstOrDefault();
        }

        if (Files.IndexOf(currentBgymlFile) is var index and > -1) {
            if (++index >= Files.Count) {
                resultIndex = 0;
                return Files.FirstOrDefault();
            }

            resultIndex = index;
            return Files[index];
        }

        return Files.FirstOrDefault();
    }
    
    public BgymlFile? GetFileBefore(BgymlFile? currentBgymlFile, out int resultIndex)
    {
        resultIndex = Files.Count > 0 ? 0 : -1;
        
        if (currentBgymlFile is null) {
            return Files.LastOrDefault();
        }

        if (Files.IndexOf(currentBgymlFile) is var index and > -1) {
            if (--index < 0) {
                resultIndex = Files.Count - 1;
                return Files.LastOrDefault();
            }
            
            resultIndex = index;
            return Files[index];
        }

        return null;
    }
}