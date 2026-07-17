using System.Text.Json;
using HELIOS.Fabric.Broker.Security;
using HELIOS.Fabric.Contracts;
using HELIOS.Fabric.Worker.Delivery;

namespace HELIOS.Fabric.Tests;

public sealed class FabricTests
{
    [Fact]
    public void CanonicalHash_IsStable()
    {
        var envelope = Event("deployment.plan.created");
        var first = CanonicalHash.ComputeEventContentSha256(envelope);
        var second = CanonicalHash.ComputeEventContentSha256(envelope);
        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public void FixedEquals_FailsClosed()
    {
        Assert.True(HmacVerifier.FixedEquals("expected", "expected"));
        Assert.False(HmacVerifier.FixedEquals("expected", "different"));
        Assert.False(HmacVerifier.FixedEquals("", ""));
    }

    [Fact]
    public void DisabledConnector_IsNotRouted()
    {
        var directory = Directory.CreateTempSubdirectory("helios-fabric-tests");
        try
        {
            var registry = Path.Combine(directory.FullName, "registry.json");
            var routes = Path.Combine(directory.FullName, "routes.json");
            File.WriteAllText(registry, """
            {"defaultMode":"deny","connectors":[{"id":"slack-ops","enabled":false}]}
            """);
            File.WriteAllText(routes, """
            {"routes":[{"id":"route","enabled":true,"priority":1,"match":{"eventTypes":["deployment.*"],"environments":["prod"],"minimumSeverity":"notice","requestedActions":["notify"]},"sinks":["slack-ops"],"approvalPolicy":null,"transform":"test","continue":false}]}
            """);
            var configuration = ConnectorConfiguration.Load(registry, routes);
            Assert.Empty(configuration.Resolve(Event("deployment.plan.created")));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static EventEnvelope Event(string type)
    {
        using var document = JsonDocument.Parse("{}");
        var envelope = new EventEnvelope
        {
            EventId = Guid.Parse("11111111-1111-4111-8111-111111111111"),
            CorrelationId = Guid.Parse("22222222-2222-4222-8222-222222222222"),
            EventType = type,
            Source = "github",
            Subject = "test",
            OccurredAt = DateTimeOffset.Parse("2026-07-17T00:00:00Z"),
            Environment = "prod",
            Severity = "notice",
            Sensitivity = "confidential",
            Summary = "test event",
            Provenance = new EventProvenance
            {
                Repository = "M0nado/helios-platform",
                CommitSha = "bd1120c57af7c19eb8687d22e9124e4e692e15dd",
                Actor = "tests",
                IdempotencyKey = "tests:deployment-plan",
                ContentSha256 = string.Empty
            },
            Payload = document.RootElement.Clone(),
            RequestedActions = ["notify"],
            Links = []
        };
        return envelope with
        {
            Provenance = envelope.Provenance with
            {
                ContentSha256 = CanonicalHash.ComputeEventContentSha256(envelope)
            }
        };
    }
}
