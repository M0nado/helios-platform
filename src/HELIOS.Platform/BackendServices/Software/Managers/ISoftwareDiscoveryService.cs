using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.BackendServices.Software.Models;

namespace HELIOS.Platform.BackendServices.Software.Managers
{
    public interface ISoftwareDiscoveryService
    {
        Task<List<SoftwarePackage>> DiscoverInstalledSoftwareAsync();
        Task<List<SoftwarePackage>> SearchPackagesAsync(string query);
        Task<SoftwarePackage> GetPackageDetailsAsync(string packageId);
        Task<bool> IsPackageInstalledAsync(string packageName);
        Task<string> GetInstalledVersionAsync(string packageName);
    }
}
