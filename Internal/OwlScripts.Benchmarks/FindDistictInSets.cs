using BenchmarkDotNet.Attributes;

namespace OwlScripts.Benchmarks;

/// <summary>
/// Benchmark different stategies for picking scalars present in every set of a collection of sets
/// </summary>
[MemoryDiagnoser]
public class FindDistictInSets
{
    private readonly string[] _set1 = ["A", "B", "1", "D", "E"];
    private readonly string[] _set2 = ["A", "2", "C", "D", "3"];
    private readonly string[] _set3 = ["A", "4", "C", "D", "E"];
    private readonly string[][] _collection;

    public FindDistictInSets()
    {
        _collection = [_set1, _set2, _set3];
    }

    [GlobalSetup]
    public void Setup()
    {
        Random.Shared.Shuffle(_set1);
        Random.Shared.Shuffle(_set2);
        Random.Shared.Shuffle(_set3);
        Random.Shared.Shuffle(_collection);
    }

    [Benchmark]
    public HashSet<string> Simple()
    {
        HashSet<string> final = [];
        HashSet<string>? inline = null;

        for (int i = 0; i < _collection.Length; i++) {
            var set = _collection[i];
            
            if (i == 1) {
                inline ??= new HashSet<string>(final.Count);
            }

            foreach (string element in set) {
                if (i == 0) {
                    final.Add(element);
                }
                else if (final.Contains(element)) {
                    inline!.Add(element);
                }
            }

            if (inline is not null) {
                final = SimpleRemoveUnique(final, inline);
                inline.Clear();
            }
        }
        
        return final;
    }

    [Benchmark]
    public List<string> MinAlloc()
    {
        if (_collection is not [var first, ..]) {
            return [];
        }

        var result = new List<string>(first);

        if (_collection.Length < 2) {
            return result;
        }

        for (int setIndex = 1; setIndex < _collection.Length; setIndex++) {
            var set = _collection[setIndex];
            for (int i = 0; i < result.Count;) {
                var element = result[i];
                if (set.Contains(element)) {
                    i++;
                    continue;
                }
                
                result.RemoveAt(i);
            }
        }

        return result;
    }
    
    private static HashSet<string> SimpleRemoveUnique(HashSet<string> left, HashSet<string> right)
    {
        var mix = new HashSet<string>(Math.Min(left.Count, right.Count));
        foreach (string key in left.Where(right.Contains)) {
            mix.Add(key);
        }

        return mix;
    }
}