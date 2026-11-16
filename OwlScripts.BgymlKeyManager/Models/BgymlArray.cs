using System.Collections.Immutable;

namespace OwlScripts.BgymlKeyManager.Models;

public sealed class BgymlArray : BgymlEntity
{
    public ImmutableArray<BgymlArrayKey> PossibleKeys { get; set; } = [];
}