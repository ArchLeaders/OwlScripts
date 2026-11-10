using AwesomeAssertions;
using Xunit;

namespace OwlScripts.Benchmarks.Tests;

public class FindDistictInSetsTests
{
    private readonly string[] _control = ["A", "D"];
    
    [Fact]
    public void FindDistictInSetsSimple_ShouldEqual_Control()
    {
        var cls = new FindDistictInSets();
        cls.Setup();
        
        var distictMany = cls.Simple();
            
        distictMany.ToArray().Sort();
        distictMany.Should().BeEquivalentTo(_control);
    }
    
    [Fact]
    public void FindDistictInSetsMinAlloc_ShouldEqual_Control()
    {
        var cls = new FindDistictInSets();
        cls.Setup();
        
        var distictMany = cls.MinAlloc();
            
        distictMany.ToArray().Sort();
        distictMany.Should().BeEquivalentTo(_control);
    }
}