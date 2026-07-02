using BenchmarkDotNet.Running;

namespace MonadoBlade.Tests.Performance;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
