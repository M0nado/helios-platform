namespace MonadoBlade.Tests.Performance.Benchmarks;

using BenchmarkDotNet.Attributes;

/// <summary>
/// Benchmarks comparing LINQ vs traditional loops for collection operations.
/// Validates optimization choices in collection processing.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, targetCount: 5)]
public class LINQVsLoopBenchmarks
{
    private int[] _data = null!;
    private List<int> _dataList = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = Enumerable.Range(1, 10000).ToArray();
        _dataList = _data.ToList();
    }

    [Benchmark(Description = "Array filtering with LINQ")]
    public int FilterWithLinq()
    {
        return _data.Where(x => x % 2 == 0).Count();
    }

    [Benchmark(Description = "Array filtering with loop")]
    public int FilterWithLoop()
    {
        int count = 0;
        for (int i = 0; i < _data.Length; i++)
        {
            if (_data[i] % 2 == 0)
                count++;
        }
        return count;
    }

    [Benchmark(Description = "List filtering with LINQ")]
    public int ListFilterWithLinq()
    {
        return _dataList.Where(x => x % 2 == 0).Count();
    }

    [Benchmark(Description = "List filtering with loop")]
    public int ListFilterWithLoop()
    {
        int count = 0;
        for (int i = 0; i < _dataList.Count; i++)
        {
            if (_dataList[i] % 2 == 0)
                count++;
        }
        return count;
    }

    [Benchmark(Description = "Array mapping with LINQ")]
    public int[] MapWithLinq()
    {
        return _data.Select(x => x * 2).ToArray();
    }

    [Benchmark(Description = "Array mapping with loop")]
    public int[] MapWithLoop()
    {
        var result = new int[_data.Length];
        for (int i = 0; i < _data.Length; i++)
        {
            result[i] = _data[i] * 2;
        }
        return result;
    }

    [Benchmark(Description = "Complex LINQ chain")]
    public int ComplexLinqChain()
    {
        return _data
            .Where(x => x > 1000)
            .Select(x => x * 2)
            .Where(x => x % 3 == 0)
            .Sum();
    }

    [Benchmark(Description = "Complex loop equivalent")]
    public int ComplexLoopEquivalent()
    {
        int sum = 0;
        for (int i = 0; i < _data.Length; i++)
        {
            if (_data[i] > 1000)
            {
                int mapped = _data[i] * 2;
                if (mapped % 3 == 0)
                {
                    sum += mapped;
                }
            }
        }
        return sum;
    }

    [Benchmark(Description = "LINQ with deferred execution")]
    public int LinqDeferredExecution()
    {
        IEnumerable<int> query = _data
            .Where(x => x % 2 == 0)
            .Select(x => x * 2);
        return query.Count();
    }

    [Benchmark(Description = "LINQ with eager evaluation")]
    public int LinqEagerEvaluation()
    {
        var query = _data
            .Where(x => x % 2 == 0)
            .Select(x => x * 2)
            .ToList();
        return query.Count;
    }

    [Benchmark(Description = "Array.FindAll vs LINQ")]
    public int[] ArrayFindAllVsLinq()
    {
        return _data.Where(x => x > 5000).ToArray();
    }

    [Benchmark(Description = "Array.FindAll")]
    public int[] ArrayFindAll()
    {
        return System.Array.FindAll(_data, x => x > 5000);
    }
}
