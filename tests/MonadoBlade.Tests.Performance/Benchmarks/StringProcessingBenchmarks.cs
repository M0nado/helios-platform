namespace MonadoBlade.Tests.Performance.Benchmarks;

using BenchmarkDotNet.Attributes;
using System.Text;

/// <summary>
/// Benchmarks for string processing optimization.
/// Measures string manipulation, splitting, parsing, and formatting performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, targetCount: 5)]
public class StringProcessingBenchmarks
{
    private string _longString = null!;
    private string[] _stringArray = null!;

    [GlobalSetup]
    public void Setup()
    {
        _longString = string.Concat(Enumerable.Range(0, 1000).Select(i => $"Item{i};"));
        _stringArray = new[]
        {
            "The quick brown fox",
            "jumps over the lazy",
            "dog in the forest",
            "near the river bank",
            "with great speed"
        };
    }

    [Benchmark(Description = "String.Split with allocation")]
    public string[] SplitWithAllocation()
    {
        return _longString.Split(';');
    }

    [Benchmark(Description = "String.Split using array of chars")]
    public string[] SplitWithCharArray()
    {
        return _longString.Split(new[] { ';' });
    }

    [Benchmark(Description = "String.Split with StringSplitOptions")]
    public string[] SplitWithOptions()
    {
        return _longString.Split(';', StringSplitOptions.RemoveEmptyEntries);
    }

    [Benchmark(Description = "String concatenation with +")]
    public string ConcatenationWithPlus()
    {
        string result = "";
        for (int i = 0; i < 100; i++)
        {
            result += $"Item{i}|";
        }
        return result;
    }

    [Benchmark(Description = "String concatenation with StringBuilder")]
    public string ConcatenationWithStringBuilder()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            sb.Append($"Item{i}|");
        }
        return sb.ToString();
    }

    [Benchmark(Description = "String concatenation with string.Concat")]
    public string ConcatenationWithConcat()
    {
        return string.Concat(Enumerable.Range(0, 100).Select(i => $"Item{i}|"));
    }

    [Benchmark(Description = "String concatenation with string.Join")]
    public string ConcatenationWithJoin()
    {
        return string.Join("|", Enumerable.Range(0, 100).Select(i => $"Item{i}"));
    }

    [Benchmark(Description = "String comparison case-sensitive")]
    public int StringComparisonCaseSensitive()
    {
        int matches = 0;
        foreach (var str in _stringArray)
        {
            if (str.Equals("the quick brown fox", StringComparison.Ordinal))
                matches++;
        }
        return matches;
    }

    [Benchmark(Description = "String comparison case-insensitive")]
    public int StringComparisonCaseInsensitive()
    {
        int matches = 0;
        foreach (var str in _stringArray)
        {
            if (str.Equals("the quick brown fox", StringComparison.OrdinalIgnoreCase))
                matches++;
        }
        return matches;
    }

    [Benchmark(Description = "String.Contains")]
    public int StringContains()
    {
        int matches = 0;
        foreach (var str in _stringArray)
        {
            if (str.Contains("the"))
                matches++;
        }
        return matches;
    }

    [Benchmark(Description = "String.StartsWith")]
    public int StringStartsWith()
    {
        int matches = 0;
        foreach (var str in _stringArray)
        {
            if (str.StartsWith("the"))
                matches++;
        }
        return matches;
    }

    [Benchmark(Description = "String.Substring")]
    public string StringSubstring()
    {
        return _longString.Substring(10, 100);
    }

    [Benchmark(Description = "String span slice")]
    public ReadOnlySpan<char> StringSpanSlice()
    {
        ReadOnlySpan<char> span = _longString.AsSpan();
        return span.Slice(10, 100);
    }

    [Benchmark(Description = "String.Trim")]
    public string StringTrim()
    {
        return "  test string  ".Trim();
    }

    [Benchmark(Description = "String.ToLower")]
    public string StringToLower()
    {
        return _longString.ToLower();
    }

    [Benchmark(Description = "String.Replace")]
    public string StringReplace()
    {
        return _longString.Replace("Item", "Element");
    }

    [Benchmark(Description = "String interpolation")]
    public string StringInterpolation()
    {
        int count = 0;
        return $"Count: {count}, Time: {DateTime.UtcNow}, Value: {Math.PI}";
    }

    [Benchmark(Description = "String.Format")]
    public string StringFormat()
    {
        int count = 0;
        return string.Format("Count: {0}, Time: {1}, Value: {2}", count, DateTime.UtcNow, Math.PI);
    }
}
