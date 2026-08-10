using HELIOS.Platform.Contracts.Monadoblade;

static void RequireThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

static MonadobladeIdentityDefinition Identity(
    MonadobladeIdentityId id,
    string kanji,
    string color,
    string cue,
    bool sysAdmin = false)
{
    return new MonadobladeIdentityDefinition(
        Id: id,
        DisplayName: id.ToString(),
        Energy: new MonadobladeEnergySignature(kanji, color, cue),
        BackgroundPreset: $"{id}-environment",
        UiGrammar: $"{id}-grammar",
        WindowsRole: sysAdmin ? "local-admin" : "standard-user",
        Interactive: true,
        IsAdministrator: sysAdmin,
        Hidden: sysAdmin,
        EnabledByDefault: !sysAdmin,
        NetworkMode: sysAdmin ? "offline-local-only" : "policy-managed",
        AlvisMode: sysAdmin ? "local-offline-read-plan" : "read-plan-request",
        AllowedOverlays: Array.Empty<MonadobladeCapabilityOverlayId>(),
        ResourceIntent: new[] { "balanced" },
        ActivationPolicy: sysAdmin
            ? new ProfileActivationPolicy(
                PhysicalPresenceRequired: true,
                MinimumFactors: 2,
                AllowedFactors: new[] { "security-key", "local-pin" },
                RemoteActivationDenied: true,
                CloudActivationDenied: true,
                AiActivationDenied: true)
            : ProfileActivationPolicy.StandardUser);
}

var valid = new[]
{
    Identity(MonadobladeIdentityId.Core, "核", "#53F6FF", "wyvern-core-harmonic"),
    Identity(MonadobladeIdentityId.Developer, "創", "#2F86FF", "wyvern-developer-code-rise"),
    Identity(MonadobladeIdentityId.Studio, "響", "#D64DFF", "wyvern-studio-harmonic-bloom"),
    Identity(MonadobladeIdentityId.Gamer, "迅", "#62FF4A", "wyvern-gamer-velocity-lock"),
    Identity(MonadobladeIdentityId.AiServer, "智", "#7A6BFF", "wyvern-ai-topology-pulse"),
    Identity(MonadobladeIdentityId.SysAdmin, "統", "#FFB000", "wyvern-sysadmin-mechanical-lock", sysAdmin: true),
};

MonadobladeDeliveryFabricCatalog.ValidateIdentities(valid);

var wrongCue = valid.ToArray();
wrongCue[0] = wrongCue[0] with
{
    Energy = wrongCue[0].Energy with { AudioCue = "not-the-canonical-cue" },
};
RequireThrows<InvalidOperationException>(
    () => MonadobladeDeliveryFabricCatalog.ValidateIdentities(wrongCue),
    "The typed catalog accepted a noncanonical audio cue.");

var weakFactors = valid.ToArray();
weakFactors[5] = weakFactors[5] with
{
    ActivationPolicy = weakFactors[5].ActivationPolicy with
    {
        AllowedFactors = new[] { "security-key", " SECURITY-KEY " },
    },
};
RequireThrows<InvalidOperationException>(
    () => MonadobladeDeliveryFabricCatalog.ValidateIdentities(weakFactors),
    "The typed catalog accepted fewer distinct factors than the Sysadmin minimum.");

Console.WriteLine("Monadoblade contract smoke passed.");
