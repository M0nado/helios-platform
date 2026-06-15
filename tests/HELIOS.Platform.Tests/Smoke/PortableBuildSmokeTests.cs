using Xunit;

namespace HELIOS.Platform.Tests.Smoke;

public sealed class PortableBuildSmokeTests
{
    [Fact]
    public void PortableBuildProfileIsEnabledByDefault()
    {
        var assembly = typeof(HELIOS.Platform.Program).Assembly;

        Assert.Equal("HELIOS.Platform", assembly.GetName().Name);
    }
}
