using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using HELIOS.Platform.Contracts.MonadoBlade.DeliveryFabric.V3;

namespace HELIOS.Platform.Contracts.Tests;

public class MonadoBladeSixProfileDeliveryFabricV3ContractsTests
{
    [Fact]
    public void ValidateSnapshot_AcceptsCanonicalSixProfileBoundaries()
    {
        var snapshot = CreateCanonicalSnapshot();
        MonadoBladeDeliveryFabricV3Validator.ValidateSnapshot(snapshot);
    }

    [Fact]
    public void ValidateProfileSet_RejectsMissingProfile()
    {
        var snapshot = CreateCanonicalSnapshot();
        var profiles = snapshot.Profiles.Where(profile => profile.Id != MonadoBladeSixProfileId.Gamer).ToArray();

        Assert.Throws<InvalidOperationException>(() =>
            MonadoBladeDeliveryFabricV3Validator.ValidateProfileSet(profiles));
    }

    [Fact]
    public void ValidateShellStateModel_RejectsPreAuthShell()
    {
        var shell = new MonadoBladeShellStateModel(
            RunsAfterWindowsAuthentication: false,
            ReplacesWindowsCredentialProvider: false,
            States: new[] { "safe-boot", "identity-verified", "wheel-select", "shell-active", "safe-neutral-blocked" });

        Assert.Throws<InvalidOperationException>(() =>
            MonadoBladeDeliveryFabricV3Validator.ValidateShellStateModel(shell));
    }

    [Fact]
    public void ValidateAlvisToolClasses_RejectsExecutorEnablement()
    {
        var classes = new MonadoBladeAlvisToolClasses(
            ReadOnlyPrefixes: new[] { "search_", "fetch_" },
            PlanOnlyPrefixes: new[] { "plan_" },
            ApprovalPendingPrefixes: new[] { "request_" },
            ExecutorToolsAllowed: true);

        Assert.Throws<InvalidOperationException>(() =>
            MonadoBladeDeliveryFabricV3Validator.ValidateAlvisToolClasses(classes));
    }

    [Fact]
    public void ValidateUsbWizardBoundary_RejectsApplyRoute()
    {
        var boundary = new MonadoBladeUsbWizardRouteBoundary(
            InventoryDryRunOnly: true,
            RequestStoragePlanOnly: true,
            ApplyRouteEnabled: true,
            PhysicalWriteAllowed: false,
            RecoveryWorkflowOwner: "sysadmin",
            QuarantineWorkflowOwner: "sysadmin");

        Assert.Throws<InvalidOperationException>(() =>
            MonadoBladeDeliveryFabricV3Validator.ValidateUsbWizardBoundary(boundary));
    }

    [Fact]
    public void ValidateLibrarySurfaces_RejectsMissingSurface()
    {
        var snapshot = CreateCanonicalSnapshot();
        var libraries = snapshot.Libraries
            .Where(library => !string.Equals(library.Surface, "wyvern", StringComparison.Ordinal))
            .ToArray();

        Assert.Throws<InvalidOperationException>(() =>
            MonadoBladeDeliveryFabricV3Validator.ValidateLibrarySurfaces(libraries));
    }

    [Fact]
    public void XmlProfileManifestsV3_ValidateAgainstXsd()
    {
        var repositoryRoot = FindRepositoryRoot();
        var xsdPath = Path.Combine(repositoryRoot, "schemas", "monado-enterprise", "v3", "profile-manifest.v3.xsd");
        var manifestDirectory = Path.Combine(repositoryRoot, "config", "monado-enterprise", "v3", "profile-manifests");

        Assert.True(File.Exists(xsdPath), "Missing profile-manifest.v3.xsd.");
        Assert.True(Directory.Exists(manifestDirectory), "Missing v3 profile manifest directory.");

        var manifestFiles = Directory.GetFiles(manifestDirectory, "*.xml");
        Assert.Equal(6, manifestFiles.Length);

        var schemaSet = new XmlSchemaSet();
        schemaSet.Add(null, xsdPath);

        foreach (var manifestPath in manifestFiles)
        {
            var validationErrors = new List<string>();
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemaSet
            };
            settings.ValidationEventHandler += (_, args) => validationErrors.Add(args.Message);

            using var reader = XmlReader.Create(manifestPath, settings);
            while (reader.Read())
            {
            }

            Assert.True(
                validationErrors.Count == 0,
                $"XML schema validation failed for {Path.GetFileName(manifestPath)}: {string.Join("; ", validationErrors)}");
        }
    }

    private static MonadoBladeDeliveryFabricSnapshot CreateCanonicalSnapshot()
    {
        var profiles = new[]
        {
            new MonadoBladeProfileBoundary(MonadoBladeSixProfileId.Core, "核", true, false, false, true, false, false),
            new MonadoBladeProfileBoundary(MonadoBladeSixProfileId.Developer, "創", true, false, false, true, false, false),
            new MonadoBladeProfileBoundary(MonadoBladeSixProfileId.Studio, "響", true, false, false, true, false, false),
            new MonadoBladeProfileBoundary(MonadoBladeSixProfileId.Gamer, "迅", true, false, false, true, false, false),
            new MonadoBladeProfileBoundary(MonadoBladeSixProfileId.AiServer, "智", false, false, false, true, false, false),
            new MonadoBladeProfileBoundary(MonadoBladeSixProfileId.SysAdmin, "統", true, true, true, false, true, true)
        };

        var shell = new MonadoBladeShellStateModel(
            RunsAfterWindowsAuthentication: true,
            ReplacesWindowsCredentialProvider: false,
            States: new[]
            {
                "safe-boot",
                "identity-verified",
                "wheel-select",
                "shell-active",
                "safe-neutral-blocked"
            });

        var alvis = new MonadoBladeAlvisToolClasses(
            ReadOnlyPrefixes: new[] { "search_", "fetch_" },
            PlanOnlyPrefixes: new[] { "plan_" },
            ApprovalPendingPrefixes: new[] { "request_" },
            ExecutorToolsAllowed: false);

        var usb = new MonadoBladeUsbWizardRouteBoundary(
            InventoryDryRunOnly: true,
            RequestStoragePlanOnly: true,
            ApplyRouteEnabled: false,
            PhysicalWriteAllowed: false,
            RecoveryWorkflowOwner: "sysadmin",
            QuarantineWorkflowOwner: "sysadmin");

        var libraries = new[]
        {
            new MonadoBladeReusableLibrarySurface("policy", "contracts/policy", "csharp", true, true),
            new MonadoBladeReusableLibrarySurface("evidence", "contracts/evidence", "csharp", true, true),
            new MonadoBladeReusableLibrarySurface("control-client", "contracts/control-client", "csharp", true, true),
            new MonadoBladeReusableLibrarySurface("shellkit", "contracts/shellkit", "csharp", true, true),
            new MonadoBladeReusableLibrarySurface("renderer", "contracts/renderer", "csharp", true, true),
            new MonadoBladeReusableLibrarySurface("chroma", "contracts/chroma", "csharp", true, true),
            new MonadoBladeReusableLibrarySurface("wyvern", "contracts/wyvern", "csharp", true, true),
            new MonadoBladeReusableLibrarySurface("usb-device-broker-requests", "contracts/usb", "csharp", true, true)
        };

        return new MonadoBladeDeliveryFabricSnapshot(profiles, shell, alvis, usb, libraries);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var gitPath = Path.Combine(directory.FullName, ".git");
            if (File.Exists(gitPath) || Directory.Exists(gitPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test base directory.");
    }
}
