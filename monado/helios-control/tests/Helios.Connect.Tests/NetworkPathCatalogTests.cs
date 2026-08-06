using System.Text.Json;
using Helios.Connect.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class NetworkPathCatalogTests
{
    private static readonly object EnvironmentLock = new();

    [Fact]
    public void Identity_profile_does_not_authorize_model_provider_hosts()
    {
        var catalog = new NetworkPathCatalog(new TestWebHostEnvironment());
        var result = JsonSerializer.SerializeToElement(catalog.GetEffectivePaths());
        var destinations = result.GetProperty("policy")
            .GetProperty("egress")
            .GetProperty("approvedDestinations");

        var identityHosts = destinations.GetProperty("microsoftIdentityAzure")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var modelHosts = destinations.GetProperty("modelProviders")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.DoesNotContain("*.azure.com", identityHosts);
        Assert.DoesNotContain(identityHosts, host => host?.Contains("openai", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(modelHosts, identityHosts.Contains);
    }

    [Fact]
    public void Enabled_profiles_are_trimmed_deduplicated_and_sorted()
    {
        lock (EnvironmentLock)
        {
            var previous = Environment.GetEnvironmentVariable("HELIOS_ENABLED_EGRESS_PROFILES");
            try
            {
                Environment.SetEnvironmentVariable("HELIOS_ENABLED_EGRESS_PROFILES", "modelProviders, github,MODELPROVIDERS");
                var catalog = new NetworkPathCatalog(new TestWebHostEnvironment());
                var result = JsonSerializer.SerializeToElement(catalog.GetEffectivePaths());

                Assert.Equal(
                    ["github", "modelProviders"],
                    result.GetProperty("enabledProfiles").EnumerateArray().Select(item => item.GetString()));
            }
            finally
            {
                Environment.SetEnvironmentVariable("HELIOS_ENABLED_EGRESS_PROFILES", previous);
            }
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Helios.Connect.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(AppContext.BaseDirectory);
    }
}
