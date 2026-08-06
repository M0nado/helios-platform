using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using HELIOS.Platform.Contracts.MonadoEnterprise.V2;

namespace HELIOS.Platform.Contracts.Tests;

public class MonadoEnterpriseExperienceV2ContractsTests
{
    [Fact]
    public void ValidateProfileCatalog_AcceptsCanonicalProfileSet()
    {
        var profiles = new[]
        {
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Core, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Developer, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Gamer, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Studio, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Personal, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.SysOps, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.AiServer, false, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.SysAdmin, true, true, true, false)
        };

        MonadoEnterpriseExperienceContractValidator.ValidateProfileCatalog(profiles);
    }

    [Fact]
    public void ValidateProfileCatalog_RejectsMissingPermanentProfile()
    {
        var profiles = new[]
        {
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Core, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Developer, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Gamer, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Studio, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Personal, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.SysOps, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.SysAdmin, true, true, true, false)
        };

        Assert.Throws<InvalidOperationException>(() =>
            MonadoEnterpriseExperienceContractValidator.ValidateProfileCatalog(profiles));
    }

    [Fact]
    public void ValidateProfileCatalog_RejectsAdditionalAdministrator()
    {
        var profiles = new[]
        {
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Core, true, true, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Developer, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Gamer, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Studio, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.Personal, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.SysOps, true, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.AiServer, false, false, false, true),
            new MonadoEnterpriseProfileDefinition(MonadoEnterpriseProfileId.SysAdmin, true, true, true, false)
        };

        Assert.Throws<InvalidOperationException>(() =>
            MonadoEnterpriseExperienceContractValidator.ValidateProfileCatalog(profiles));
    }

    [Fact]
    public void ValidateStorageCheckpoint_RejectsUnsafeVaultAutomount()
    {
        var checkpoint = new MonadoEnterpriseStorageCheckpoint(
            HasCoreCrossPhysicalVolume: true,
            HasDynamicDevDriveVhdx: true,
            HasBitLockerVaultVhdx: true,
            VaultAutoMountEnabled: true);

        Assert.Throws<InvalidOperationException>(() =>
            MonadoEnterpriseExperienceContractValidator.ValidateStorageCheckpoint(checkpoint));
    }

    [Fact]
    public void ValidateSyncCheckpoint_RejectsNonProposalMode()
    {
        var checkpoint = new MonadoEnterpriseSyncCheckpoint(
            ExecutionMode: "apply",
            DirectExternalDeliveryEnabled: false,
            AzureDevOpsReadOnly: true,
            AdobeDesignWritesEnabled: false);

        Assert.Throws<InvalidOperationException>(() =>
            MonadoEnterpriseExperienceContractValidator.ValidateSyncCheckpoint(checkpoint));
    }

    [Fact]
    public void XmlProfileManifests_ValidateAgainstV2Xsd()
    {
        var repositoryRoot = FindRepositoryRoot();
        var xsdPath = Path.Combine(repositoryRoot, "schemas", "monado-enterprise", "v2", "profile-manifest.v2.xsd");
        var manifestDirectory = Path.Combine(repositoryRoot, "config", "monado-enterprise", "v2", "profile-manifests");
        Assert.True(File.Exists(xsdPath), "Missing profile-manifest.v2.xsd.");
        Assert.True(Directory.Exists(manifestDirectory), "Missing profile manifest directory.");

        var manifestFiles = Directory.GetFiles(manifestDirectory, "*.xml");
        Assert.Equal(8, manifestFiles.Length);

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
