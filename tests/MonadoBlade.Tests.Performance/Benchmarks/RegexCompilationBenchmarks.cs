namespace MonadoBlade.Tests.Performance.Benchmarks;

using BenchmarkDotNet.Attributes;
using System.Text.RegularExpressions;

/// <summary>
/// Benchmarks for regex compilation optimization.
/// Measures the performance impact of compiled vs non-compiled regex patterns.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, targetCount: 5)]
public class RegexCompilationBenchmarks
{
    private const string EmailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    private const string UrlPattern = @"https?://[^\s/$.?#].[^\s]*";
    private const string DatePattern = @"^\d{4}-\d{2}-\d{2}$";

    private Regex _emailRegex = null!;
    private Regex _emailCompiledRegex = null!;
    private Regex _urlRegex = null!;
    private Regex _urlCompiledRegex = null!;

    private string[] _testEmails = null!;
    private string[] _testUrls = null!;

    [GlobalSetup]
    public void Setup()
    {
        _emailRegex = new Regex(EmailPattern);
        _emailCompiledRegex = new Regex(EmailPattern, RegexOptions.Compiled);
        _urlRegex = new Regex(UrlPattern);
        _urlCompiledRegex = new Regex(UrlPattern, RegexOptions.Compiled);

        _testEmails = new[]
        {
            "user@example.com",
            "invalid.email@",
            "test.user+tag@domain.co.uk",
            "another@test.org",
            "notanemail"
        };

        _testUrls = new[]
        {
            "https://github.com/user/repo",
            "http://example.com/path?query=value",
            "https://test.org",
            "ftp://files.example.com",
            "not a url"
        };
    }

    [Benchmark(Description = "Non-compiled regex email validation")]
    public int NonCompiledEmailValidation()
    {
        int matches = 0;
        foreach (var email in _testEmails)
        {
            if (_emailRegex.IsMatch(email))
                matches++;
        }
        return matches;
    }

    [Benchmark(Description = "Compiled regex email validation")]
    public int CompiledEmailValidation()
    {
        int matches = 0;
        foreach (var email in _testEmails)
        {
            if (_emailCompiledRegex.IsMatch(email))
                matches++;
        }
        return matches;
    }

    [Benchmark(Description = "Non-compiled regex URL validation")]
    public int NonCompiledUrlValidation()
    {
        int matches = 0;
        foreach (var url in _testUrls)
        {
            if (_urlRegex.IsMatch(url))
                matches++;
        }
        return matches;
    }

    [Benchmark(Description = "Compiled regex URL validation")]
    public int CompiledUrlValidation()
    {
        int matches = 0;
        foreach (var url in _testUrls)
        {
            if (_urlCompiledRegex.IsMatch(url))
                matches++;
        }
        return matches;
    }

    [Benchmark(Description = "Regex.IsMatch static call")]
    public int StaticRegexIsMatch()
    {
        int matches = 0;
        foreach (var email in _testEmails)
        {
            if (Regex.IsMatch(email, EmailPattern))
                matches++;
        }
        return matches;
    }

    [Benchmark(Description = "Regex.IsMatch with compiled static")]
    public int StaticRegexIsMatchCompiled()
    {
        int matches = 0;
        foreach (var email in _testEmails)
        {
            if (Regex.IsMatch(email, EmailPattern, RegexOptions.Compiled))
                matches++;
        }
        return matches;
    }

    [Benchmark(Description = "Regex extraction non-compiled")]
    public int RegexExtractionNonCompiled()
    {
        int extracted = 0;
        foreach (var url in _testUrls)
        {
            var match = _urlRegex.Match(url);
            if (match.Success)
                extracted++;
        }
        return extracted;
    }

    [Benchmark(Description = "Regex extraction compiled")]
    public int RegexExtractionCompiled()
    {
        int extracted = 0;
        foreach (var url in _testUrls)
        {
            var match = _urlCompiledRegex.Match(url);
            if (match.Success)
                extracted++;
        }
        return extracted;
    }

    [Benchmark(Description = "Regex Split non-compiled")]
    public string[] RegexSplitNonCompiled()
    {
        return _urlRegex.Split("url1|url2|url3|url4|url5");
    }

    [Benchmark(Description = "Regex Split compiled")]
    public string[] RegexSplitCompiled()
    {
        return _urlCompiledRegex.Split("url1|url2|url3|url4|url5");
    }
}
