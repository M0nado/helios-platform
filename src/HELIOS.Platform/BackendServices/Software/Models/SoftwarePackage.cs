using System;
using System.Collections.Generic;

namespace HELIOS.Platform.BackendServices.Software.Models
{
    /// <summary>
    /// Represents a software package that can be installed, updated, or managed
    /// </summary>
    public class SoftwarePackage
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public string Category { get; set; }
        public string Publisher { get; set; }
        public string Homepage { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Installation methods: "chocolatey", "winget", "official", "portable", "docker", "wsl", "steam"
        /// </summary>
        public List<string> InstallationMethods { get; set; } = new List<string>();
        
        /// <summary>
        /// Preferred installation method
        /// </summary>
        public string PreferredMethod { get; set; }
        
        public string DownloadUrl { get; set; }
        public string InstallCommand { get; set; }
        public string UninstallCommand { get; set; }
        public string UpdateCommand { get; set; }
        public string VerifyCommand { get; set; }
        
        /// <summary>
        /// Size in MB
        /// </summary>
        public decimal SizeInMB { get; set; }
        
        public bool IsInstalled { get; set; }
        public bool AutoUpdate { get; set; } = true;
        public bool IsPortable { get; set; }
        public bool RequiresAdmin { get; set; }
        
        /// <summary>
        /// System dependencies required for this package
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();
        
        /// <summary>
        /// Configuration options for pre-configuration
        /// </summary>
        public Dictionary<string, object> ConfigurationOptions { get; set; } = new Dictionary<string, object>();
        
        public DateTime LastCheckedUpdate { get; set; } = DateTime.Now;
        public DateTime LastInstalled { get; set; }
        public DateTime LastUpdated { get; set; }
        
        /// <summary>
        /// License type: "free", "commercial", "trial", "open-source"
        /// </summary>
        public string LicenseType { get; set; } = "free";
        
        public string LicenseKey { get; set; }
        public DateTime LicenseExpiry { get; set; }
        
        /// <summary>
        /// Installation status: "not-installed", "installed", "updating", "failed", "rollback"
        /// </summary>
        public string Status { get; set; } = "not-installed";
        
        /// <summary>
        /// Any error or warning messages
        /// </summary>
        public string StatusMessage { get; set; }
        
        public override string ToString()
        {
            return $"{Name} v{CurrentVersion} ({Status})";
        }
    }

    /// <summary>
    /// Category enumeration for software packages
    /// </summary>
    public static class SoftwareCategory
    {
        public const string Development = "Development";
        public const string Gaming = "Gaming";
        public const string Communication = "Communication";
        public const string Productivity = "Productivity";
        public const string Security = "Security";
        public const string Media = "Media";
        public const string SystemTools = "SystemTools";
        public const string Browsers = "Browsers";
        public const string Utilities = "Utilities";
        public const string CloudServices = "CloudServices";
        public const string Virtualization = "Virtualization";

        public static List<string> All => new List<string>
        {
            Development, Gaming, Communication, Productivity, Security,
            Media, SystemTools, Browsers, Utilities, CloudServices, Virtualization
        };
    }

    /// <summary>
    /// Installation method enumeration
    /// </summary>
    public static class InstallationMethod
    {
        public const string Chocolatey = "chocolatey";
        public const string Winget = "winget";
        public const string Official = "official";
        public const string Portable = "portable";
        public const string Docker = "docker";
        public const string WSL = "wsl";
        public const string Steam = "steam";
        public const string Scoop = "scoop";
        public const string NPM = "npm";
        public const string Pip = "pip";
        public const string Cargo = "cargo";

        public static List<string> All => new List<string>
        {
            Chocolatey, Winget, Official, Portable, Docker, WSL, Steam, Scoop, NPM, Pip, Cargo
        };
    }

    /// <summary>
    /// Software status enumeration
    /// </summary>
    public static class SoftwareStatus
    {
        public const string NotInstalled = "not-installed";
        public const string Installed = "installed";
        public const string Updating = "updating";
        public const string Failed = "failed";
        public const string Rollback = "rollback";
        public const string Pending = "pending";
    }
}
