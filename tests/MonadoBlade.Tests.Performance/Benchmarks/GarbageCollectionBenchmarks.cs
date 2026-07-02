namespace MonadoBlade.Tests.Performance.Benchmarks;

using BenchmarkDotNet.Attributes;
using System.Collections.Generic;

/// <summary>
/// Benchmarks for GC pressure and allocation patterns.
/// Measures the effectiveness of object pooling and allocation reduction strategies.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, targetCount: 5)]
public class GarbageCollectionBenchmarks
{
    private const int Iterations = 1000;

    [Benchmark(Description = "High allocation pattern")]
    public long HighAllocationPattern()
    {
        long total = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var list = new List<int>();
            for (int j = 0; j < 100; j++)
            {
                list.Add(j);
            }
            total += list.Count;
        }
        return total;
    }

    [Benchmark(Description = "Pre-allocated pattern")]
    public long PreAllocatedPattern()
    {
        long total = 0;
        var list = new List<int>(100);
        for (int i = 0; i < Iterations; i++)
        {
            list.Clear();
            for (int j = 0; j < 100; j++)
            {
                list.Add(j);
            }
            total += list.Count;
        }
        return total;
    }

    [Benchmark(Description = "String concatenation with allocation")]
    public string StringConcatenationWithAllocation()
    {
        string result = "";
        for (int i = 0; i < 100; i++)
        {
            result += $"Item{i};";
        }
        return result;
    }

    [Benchmark(Description = "String concatenation with StringBuilder")]
    public string StringConcatenationWithStringBuilder()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            sb.Append($"Item{i};");
        }
        return sb.ToString();
    }

    [Benchmark(Description = "LINQ with allocation")]
    public List<int> LinqWithAllocation()
    {
        return Enumerable.Range(0, 1000)
            .Where(x => x % 2 == 0)
            .Select(x => x * 2)
            .ToList();
    }

    [Benchmark(Description = "Manual loop without allocation")]
    public List<int> ManualLoopWithoutAllocation()
    {
        var result = new List<int>();
        for (int i = 0; i < 1000; i++)
        {
            if (i % 2 == 0)
            {
                result.Add(i * 2);
            }
        }
        return result;
    }

    [Benchmark(Description = "Dictionary allocation per iteration")]
    public int DictionaryAllocationPerIteration()
    {
        int count = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var dict = new Dictionary<string, int>();
            for (int j = 0; j < 50; j++)
            {
                dict[$"Key{j}"] = j;
            }
            count += dict.Count;
        }
        return count;
    }

    [Benchmark(Description = "Dictionary reuse with clear")]
    public int DictionaryReuseWithClear()
    {
        int count = 0;
        var dict = new Dictionary<string, int>();
        for (int i = 0; i < Iterations; i++)
        {
            dict.Clear();
            for (int j = 0; j < 50; j++)
            {
                dict[$"Key{j}"] = j;
            }
            count += dict.Count;
        }
        return count;
    }

    [Benchmark(Description = "Array boxing")]
    public long ArrayBoxing()
    {
        long total = 0;
        var array = new object[1000];
        for (int i = 0; i < 1000; i++)
        {
            array[i] = i;
            if (array[i] is int value)
                total += value;
        }
        return total;
    }

    [Benchmark(Description = "Generic array without boxing")]
    public long GenericArrayWithoutBoxing()
    {
        long total = 0;
        var array = new int[1000];
        for (int i = 0; i < 1000; i++)
        {
            array[i] = i;
            total += array[i];
        }
        return total;
    }

    [Benchmark(Description = "Closure allocation")]
    public int ClosureAllocation()
    {
        var results = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            int captured = i;
            var func = new Func<int>(() => captured);
            results.Add(func());
        }
        return results.Count;
    }

    [Benchmark(Description = "Delegate allocation")]
    public int DelegateAllocation()
    {
        int count = 0;
        for (int i = 0; i < 1000; i++)
        {
            Action action = () => count++;
            action?.Invoke();
        }
        return count;
    }
}
