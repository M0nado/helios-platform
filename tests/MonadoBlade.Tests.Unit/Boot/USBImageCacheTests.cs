namespace MonadoBlade.Tests.Unit.Boot;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MonadoBlade.Boot.Services;

public class USBImageCacheTests
{
    private readonly Mock<ILogger<USBImageCache>> _mockLogger;
    private readonly USBImageCache _cache;

    public USBImageCacheTests()
    {
        _mockLogger = new Mock<ILogger<USBImageCache>>();
        _cache = new USBImageCache(_mockLogger.Object);
    }

    [Fact]
    public void TryGetImage_WithMissingKey_ReturnsFalse()
    {
        // Act
        var result = _cache.TryGetImage("nonexistent", out var image);

        // Assert
        result.Should().BeFalse();
        image.Should().BeEmpty();
    }

    [Fact]
    public void StoreImage_WithValidData_SuccessfullyStores()
    {
        // Arrange
        var key = "test-image";
        var imageData = new byte[1024]; // 1KB

        // Act
        _cache.StoreImage(key, imageData);
        var result = _cache.TryGetImage(key, out var retrievedImage);

        // Assert
        result.Should().BeTrue();
        retrievedImage.Should().Equal(imageData);
    }

    [Fact]
    public void TryGetImage_AfterStore_UpdatesAccessMetadata()
    {
        // Arrange
        var key = "test-image";
        var imageData = new byte[1024];
        _cache.StoreImage(key, imageData);

        // Act
        _cache.TryGetImage(key, out _);
        _cache.TryGetImage(key, out _);
        var stats = _cache.GetStatistics();

        // Assert
        stats.ItemCount.Should().Be(1);
        stats.TotalSizeBytes.Should().Be(1024);
    }

    [Fact]
    public void StoreImage_WithMultipleImages_MaintainsLRUOrder()
    {
        // Arrange
        var key1 = "image-1";
        var key2 = "image-2";
        var key3 = "image-3";
        var imageData = new byte[1024];

        // Act
        _cache.StoreImage(key1, imageData);
        _cache.StoreImage(key2, imageData);
        _cache.StoreImage(key3, imageData);

        // All should be retrievable
        var result1 = _cache.TryGetImage(key1, out _);
        var result2 = _cache.TryGetImage(key2, out _);
        var result3 = _cache.TryGetImage(key3, out _);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        // Arrange
        var imageData = new byte[1024];
        _cache.StoreImage("key1", imageData);
        _cache.StoreImage("key2", imageData);

        // Act
        _cache.Clear();
        var stats = _cache.GetStatistics();

        // Assert
        stats.ItemCount.Should().Be(0);
        stats.TotalSizeBytes.Should().Be(0);
    }

    [Fact]
    public void GetStatistics_ReturnsAccurateValues()
    {
        // Arrange
        var imageData = new byte[1024 * 1024]; // 1MB
        _cache.StoreImage("key1", imageData);
        _cache.StoreImage("key2", imageData);

        // Act
        var stats = _cache.GetStatistics();

        // Assert
        stats.ItemCount.Should().Be(2);
        stats.TotalSizeBytes.Should().Be(2 * 1024 * 1024);
        stats.MaxSizeBytes.Should().Be(50L * 1024L * 1024L * 1024L); // 50GB
    }

    [Fact]
    public void StoreImage_WithExistingKey_Replaces()
    {
        // Arrange
        var key = "test-image";
        var oldData = new byte[512];
        var newData = new byte[1024];

        _cache.StoreImage(key, oldData);
        var statsAfterFirst = _cache.GetStatistics();

        // Act
        _cache.StoreImage(key, newData);
        var statsAfterSecond = _cache.GetStatistics();

        // Assert
        statsAfterFirst.TotalSizeBytes.Should().Be(512);
        statsAfterSecond.TotalSizeBytes.Should().Be(1024);
        statsAfterSecond.ItemCount.Should().Be(1);
    }

    [Fact]
    public void GetCacheContents_ReturnsAllEntries()
    {
        // Arrange
        var imageData = new byte[1024];
        _cache.StoreImage("key1", imageData);
        _cache.StoreImage("key2", imageData);
        _cache.StoreImage("key3", imageData);

        // Act
        var contents = _cache.GetCacheContents();

        // Assert
        contents.Should().HaveCount(3);
        contents.Select(x => x.Key).Should().Contain("key1", "key2", "key3");
    }

    [Fact]
    public void StoreImage_WithNullImage_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _cache.StoreImage("key", null!));
    }

    [Fact]
    public void TryGetImage_WithEmptyKey_ReturnsFalse()
    {
        // Act
        var result = _cache.TryGetImage(string.Empty, out _);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetStatistics_CalculatesCacheUtilization()
    {
        // Arrange
        var oneGigaByte = 1L * 1024L * 1024L * 1024L;
        var imageData = new byte[oneGigaByte];

        _cache.StoreImage("large-image", imageData);

        // Act
        var stats = _cache.GetStatistics();

        // Assert
        stats.UtilizationPercent.Should().BeGreaterThan(0);
        stats.UtilizationPercent.Should().BeLessThanOrEqualTo(100);
    }
}

public class BackgroundUSBBuilderTests
{
    private readonly Mock<ILogger<BackgroundUSBBuilder>> _mockLogger;
    private readonly Mock<IUSBImageCache> _mockCache;
    private readonly BackgroundUSBBuilder _builder;

    public BackgroundUSBBuilderTests()
    {
        _mockLogger = new Mock<ILogger<BackgroundUSBBuilder>>();
        _mockCache = new Mock<IUSBImageCache>();
        _builder = new BackgroundUSBBuilder(_mockLogger.Object, _mockCache.Object);
    }

    [Fact]
    public async Task InitializeAsync_SetsIsInitializedTrue()
    {
        // Act
        await _builder.InitializeAsync();

        // Assert
        _builder.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task QueueUSBBuildAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _builder.QueueUSBBuildAsync(null!));
    }

    [Fact]
    public async Task QueueUSBBuildAsync_WithValidRequest_Succeeds()
    {
        // Arrange
        await _builder.InitializeAsync();
        var request = new USBCreationRequest
        {
            DeviceName = "TestUSB",
            TargetDisk = "E:\\",
            Profile = "Gamer"
        };

        // Act
        await _builder.QueueUSBBuildAsync(request);

        // Assert - should not throw
    }

    [Fact]
    public async Task GetMetrics_ReturnsInitialMetrics()
    {
        // Act
        var metrics = _builder.GetMetrics();

        // Assert
        metrics.CacheHits.Should().Be(0);
        metrics.CacheMisses.Should().Be(0);
        metrics.CacheHitRate.Should().Be(0);
        metrics.QueuedItems.Should().Be(0);
    }

    [Fact]
    public async Task DisposeAsync_DisposesSuccessfully()
    {
        // Arrange
        await _builder.InitializeAsync();

        // Act
        await _builder.DisposeAsync();

        // Assert - should not throw
    }

    [Fact]
    public async Task QueueUSBBuildAsync_WithMultipleRequests_IncreasesQueueCount()
    {
        // Arrange
        await _builder.InitializeAsync();

        var requests = new[]
        {
            new USBCreationRequest { DeviceName = "USB1", TargetDisk = "E:\\", Profile = "Gamer" },
            new USBCreationRequest { DeviceName = "USB2", TargetDisk = "F:\\", Profile = "Developer" },
            new USBCreationRequest { DeviceName = "USB3", TargetDisk = "G:\\", Profile = "Secure" }
        };

        // Act
        foreach (var request in requests)
        {
            await _builder.QueueUSBBuildAsync(request);
        }

        var metrics = _builder.GetMetrics();

        // Assert
        metrics.QueuedItems.Should().Be(3);
    }
}
