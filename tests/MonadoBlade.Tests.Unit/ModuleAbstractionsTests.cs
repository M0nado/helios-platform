namespace MonadoBlade.Tests.Unit;

using Xunit;
using MonadoBlade.Security.Abstractions;
using MonadoBlade.Graphics.Abstractions;
using MonadoBlade.Boot.Abstractions;
using MonadoBlade.Dashboard.Abstractions;
using MonadoBlade.Audio.Abstractions;
using MonadoBlade.Developer.Abstractions;
using MonadoBlade.Tools.Abstractions;

public class ModuleAbstractionsTests
{
    [Fact]
    public void SecurityService_InterfaceExists()
    {
        // Verify interface is defined
        var type = typeof(ISecurityService);
        Assert.NotNull(type);
        Assert.True(type.IsInterface);
    }

    [Fact]
    public void GraphicsService_InterfaceExists()
    {
        var type = typeof(IGraphicsService);
        Assert.NotNull(type);
        Assert.True(type.IsInterface);
    }

    [Fact]
    public void BootService_InterfaceExists()
    {
        var type = typeof(IBootService);
        Assert.NotNull(type);
        Assert.True(type.IsInterface);
    }

    [Fact]
    public void DashboardService_InterfaceExists()
    {
        var type = typeof(IDashboardService);
        Assert.NotNull(type);
        Assert.True(type.IsInterface);
    }

    [Fact]
    public void AudioService_InterfaceExists()
    {
        var type = typeof(IAudioService);
        Assert.NotNull(type);
        Assert.True(type.IsInterface);
    }

    [Fact]
    public void DeveloperService_InterfaceExists()
    {
        var type = typeof(IDeveloperService);
        Assert.NotNull(type);
        Assert.True(type.IsInterface);
    }

    [Fact]
    public void ToolsService_InterfaceExists()
    {
        var type = typeof(IToolsService);
        Assert.NotNull(type);
        Assert.True(type.IsInterface);
    }
}
