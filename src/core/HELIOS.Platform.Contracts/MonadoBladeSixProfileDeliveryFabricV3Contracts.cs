namespace HELIOS.Platform.Contracts.MonadoBlade.DeliveryFabric.V3;

/// <summary>
/// Canonical Monadoblade six-profile set for delivery fabric v3.
/// </summary>
public enum MonadoBladeSixProfileId
{
    Core,
    Developer,
    Studio,
    Gamer,
    AiServer,
    SysAdmin
}

/// <summary>
/// Security boundary for a v3 profile.
/// </summary>
public sealed record MonadoBladeProfileBoundary(
    MonadoBladeSixProfileId Id,
    string Glyph,
    bool Interactive,
    bool Administrator,
    bool Hidden,
    bool EnabledByDefault,
    bool LocalOnly,
    bool OfflineRequired);

/// <summary>
/// Post-auth shell state model. This presentation layer can only run after Windows authentication.
/// </summary>
public sealed record MonadoBladeShellStateModel(
    bool RunsAfterWindowsAuthentication,
    bool ReplacesWindowsCredentialProvider,
    IReadOnlyCollection<string> States);

/// <summary>
/// ALVIS tool-class policy for v3.
/// </summary>
public sealed record MonadoBladeAlvisToolClasses(
    IReadOnlyCollection<string> ReadOnlyPrefixes,
    IReadOnlyCollection<string> PlanOnlyPrefixes,
    IReadOnlyCollection<string> ApprovalPendingPrefixes,
    bool ExecutorToolsAllowed);

/// <summary>
/// USB Wizard request boundaries for v3.
/// </summary>
public sealed record MonadoBladeUsbWizardRouteBoundary(
    bool InventoryDryRunOnly,
    bool RequestStoragePlanOnly,
    bool ApplyRouteEnabled,
    bool PhysicalWriteAllowed,
    string RecoveryWorkflowOwner,
    string QuarantineWorkflowOwner);

/// <summary>
/// Reusable library surface reference for policy/evidence/control/shell/renderer/chroma/wyvern/USB contracts.
/// </summary>
public sealed record MonadoBladeReusableLibrarySurface(
    string Surface,
    string ContractPath,
    string Language,
    bool Versioned,
    bool Reusable);

/// <summary>
/// Combined v3 boundary snapshot used by typed validators and tests.
/// </summary>
public sealed record MonadoBladeDeliveryFabricSnapshot(
    IReadOnlyCollection<MonadoBladeProfileBoundary> Profiles,
    MonadoBladeShellStateModel ShellStateModel,
    MonadoBladeAlvisToolClasses AlvisToolClasses,
    MonadoBladeUsbWizardRouteBoundary UsbWizardBoundary,
    IReadOnlyCollection<MonadoBladeReusableLibrarySurface> Libraries);

/// <summary>
/// Fail-closed validator for Monadoblade six-profile delivery fabric v3.
/// </summary>
public static class MonadoBladeDeliveryFabricV3Validator
{
    private static readonly HashSet<MonadoBladeSixProfileId> ExpectedProfiles = new()
    {
        MonadoBladeSixProfileId.Core,
        MonadoBladeSixProfileId.Developer,
        MonadoBladeSixProfileId.Studio,
        MonadoBladeSixProfileId.Gamer,
        MonadoBladeSixProfileId.AiServer,
        MonadoBladeSixProfileId.SysAdmin
    };

    private static readonly HashSet<string> RequiredShellStates = new(StringComparer.Ordinal)
    {
        "safe-boot",
        "identity-verified",
        "wheel-select",
        "shell-active",
        "safe-neutral-blocked"
    };

    private static readonly HashSet<string> RequiredLibrarySurfaces = new(StringComparer.Ordinal)
    {
        "policy",
        "evidence",
        "control-client",
        "shellkit",
        "renderer",
        "chroma",
        "wyvern",
        "usb-device-broker-requests"
    };

    public static void ValidateProfileSet(IReadOnlyCollection<MonadoBladeProfileBoundary> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var ids = profiles.Select(profile => profile.Id).ToArray();
        var idSet = ids.ToHashSet();
        if (!idSet.SetEquals(ExpectedProfiles) || ids.Length != ExpectedProfiles.Count)
        {
            throw new InvalidOperationException("Profile set must contain exactly the six canonical v3 profiles.");
        }

        var admins = profiles.Where(profile => profile.Administrator).ToArray();
        if (admins.Length != 1 || admins[0].Id != MonadoBladeSixProfileId.SysAdmin)
        {
            throw new InvalidOperationException("SysAdmin must be the only administrator profile.");
        }

        var sysAdmin = admins[0];
        if (!sysAdmin.Hidden || sysAdmin.EnabledByDefault)
        {
            throw new InvalidOperationException("SysAdmin must be hidden and disabled by default.");
        }

        if (!sysAdmin.LocalOnly || !sysAdmin.OfflineRequired)
        {
            throw new InvalidOperationException("SysAdmin must remain local-only and offline-required.");
        }
    }

    public static void ValidateShellStateModel(MonadoBladeShellStateModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(model.States);

        if (!model.RunsAfterWindowsAuthentication)
        {
            throw new InvalidOperationException("Shell state model must run after Windows authentication.");
        }

        if (model.ReplacesWindowsCredentialProvider)
        {
            throw new InvalidOperationException("Shell state model must not replace the Windows credential provider.");
        }

        var states = model.States.Where(state => !string.IsNullOrWhiteSpace(state)).ToHashSet(StringComparer.Ordinal);
        if (!RequiredShellStates.IsSubsetOf(states))
        {
            throw new InvalidOperationException("Shell state model is missing one or more required post-auth states.");
        }
    }

    public static void ValidateAlvisToolClasses(MonadoBladeAlvisToolClasses toolClasses)
    {
        ArgumentNullException.ThrowIfNull(toolClasses);
        ArgumentNullException.ThrowIfNull(toolClasses.ReadOnlyPrefixes);
        ArgumentNullException.ThrowIfNull(toolClasses.PlanOnlyPrefixes);
        ArgumentNullException.ThrowIfNull(toolClasses.ApprovalPendingPrefixes);

        var readOnly = toolClasses.ReadOnlyPrefixes.ToHashSet(StringComparer.Ordinal);
        var planOnly = toolClasses.PlanOnlyPrefixes.ToHashSet(StringComparer.Ordinal);
        var approvalPending = toolClasses.ApprovalPendingPrefixes.ToHashSet(StringComparer.Ordinal);

        if (!readOnly.SetEquals(new[] { "search_", "fetch_" }))
        {
            throw new InvalidOperationException("ALVIS read-only tools must use search_ and fetch_ prefixes.");
        }

        if (!planOnly.SetEquals(new[] { "plan_" }))
        {
            throw new InvalidOperationException("ALVIS plan-only tools must use the plan_ prefix.");
        }

        if (!approvalPending.SetEquals(new[] { "request_" }))
        {
            throw new InvalidOperationException("ALVIS approval-pending tools must use the request_ prefix.");
        }

        if (toolClasses.ExecutorToolsAllowed)
        {
            throw new InvalidOperationException("ALVIS cannot expose executor/apply tools.");
        }
    }

    public static void ValidateUsbWizardBoundary(MonadoBladeUsbWizardRouteBoundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);

        if (!boundary.InventoryDryRunOnly)
        {
            throw new InvalidOperationException("USB inventory route must remain dry-run only.");
        }

        if (!boundary.RequestStoragePlanOnly)
        {
            throw new InvalidOperationException("USB request route must remain storage-plan-only.");
        }

        if (boundary.ApplyRouteEnabled)
        {
            throw new InvalidOperationException("USB apply route must remain disabled.");
        }

        if (boundary.PhysicalWriteAllowed)
        {
            throw new InvalidOperationException("USB physical writes must remain disallowed.");
        }

        if (!string.Equals(boundary.RecoveryWorkflowOwner, "sysadmin", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(boundary.QuarantineWorkflowOwner, "sysadmin", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Recovery and Quarantine workflows must remain owned by SysAdmin.");
        }
    }

    public static void ValidateLibrarySurfaces(IReadOnlyCollection<MonadoBladeReusableLibrarySurface> libraries)
    {
        ArgumentNullException.ThrowIfNull(libraries);
        if (libraries.Count == 0)
        {
            throw new InvalidOperationException("At least one reusable library surface is required.");
        }

        var surfaces = libraries.Select(library => library.Surface).ToHashSet(StringComparer.Ordinal);
        if (!surfaces.SetEquals(RequiredLibrarySurfaces))
        {
            throw new InvalidOperationException("Library surfaces must match the required v3 policy/evidence/control/shell/renderer/chroma/wyvern/USB set.");
        }

        foreach (var library in libraries)
        {
            if (string.IsNullOrWhiteSpace(library.ContractPath) || string.IsNullOrWhiteSpace(library.Language))
            {
                throw new InvalidOperationException($"Library surface {library.Surface} must declare a path and language.");
            }

            if (!library.Versioned || !library.Reusable)
            {
                throw new InvalidOperationException($"Library surface {library.Surface} must remain versioned and reusable.");
            }
        }
    }

    public static void ValidateSnapshot(MonadoBladeDeliveryFabricSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateProfileSet(snapshot.Profiles);
        ValidateShellStateModel(snapshot.ShellStateModel);
        ValidateAlvisToolClasses(snapshot.AlvisToolClasses);
        ValidateUsbWizardBoundary(snapshot.UsbWizardBoundary);
        ValidateLibrarySurfaces(snapshot.Libraries);
    }
}
