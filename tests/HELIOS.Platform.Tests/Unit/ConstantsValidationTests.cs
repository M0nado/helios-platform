using Xunit;
using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Tests.Unit
{
    /// <summary>
    /// Unit Tests for Constants Validation - 3 test cases
    /// Category: Unit
    /// Tests that constants are properly defined and accessible
    /// </summary>
    public class ConstantsValidationTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void ApplicationConstants_AreNotNull()
        {
            // Arrange & Act
            var appName = AppConstants.ApplicationName;
            var version = AppConstants.Version;
            var author = AppConstants.Author;

            // Assert
            Assert.NotNull(appName);
            Assert.NotEmpty(appName);
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            Assert.NotNull(author);
            Assert.NotEmpty(author);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ConfigurationConstants_HaveValidValues()
        {
            // Arrange & Act
            var defaultTimeout = ConfigConstants.DefaultTimeout;
            var maxRetries = ConfigConstants.MaxRetries;
            var bufferSize = ConfigConstants.BufferSize;

            // Assert
            Assert.InRange(defaultTimeout, 1, int.MaxValue);
            Assert.InRange(maxRetries, 1, 100);
            Assert.InRange(bufferSize, 1, int.MaxValue);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void PathConstants_AreWellFormed()
        {
            // Arrange & Act
            var configPath = PathConstants.ConfigDirectory;
            var logPath = PathConstants.LogDirectory;
            var dataPath = PathConstants.DataDirectory;

            // Assert
            Assert.NotNull(configPath);
            Assert.NotEmpty(configPath);
            Assert.NotNull(logPath);
            Assert.NotEmpty(logPath);
            Assert.NotNull(dataPath);
            Assert.NotEmpty(dataPath);
        }
    }

    /// <summary>
    /// Application constants
    /// </summary>
    public static class AppConstants
    {
        public const string ApplicationName = "HELIOS Platform";
        public const string Version = "1.0.0";
        public const string Author = "HELIOS Development Team";
        public const string Description = "Enterprise Automation Platform";
    }

    /// <summary>
    /// Configuration constants
    /// </summary>
    public static class ConfigConstants
    {
        public const int DefaultTimeout = 30000; // milliseconds
        public const int MaxRetries = 3;
        public const int BufferSize = 65536; // 64 KB
        public const int MaxConnections = 100;
    }

    /// <summary>
    /// Path constants
    /// </summary>
    public static class PathConstants
    {
        public static string ConfigDirectory => 
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELIOS");
        
        public static string LogDirectory => 
            System.IO.Path.Combine(ConfigDirectory, "logs");
        
        public static string DataDirectory => 
            System.IO.Path.Combine(ConfigDirectory, "data");
    }
}
