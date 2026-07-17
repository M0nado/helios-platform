using System.Net;
using System.Text;
using Azure.Core;
using Helios.Connect.Api;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class AzureInventoryServiceTests
{
    [Fact]
    public async Task Inventory_follows_validated_management_pagination()
    {
        var handler = new SequencedHandler(
            "{\"value\":[{\"id\":\"/one\",\"name\":\"one\",\"type\":\"Microsoft.App/containerApps\"}],\"nextLink\":\"https://management.azure.com/next?page=2\"}",
            "{\"value\":[{\"id\":\"/two\",\"name\":\"two\",\"type\":\"Microsoft.Search/searchServices\"}]}");
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        var resources = await service.ListResourcesAsync(null, CancellationToken.None);

        Assert.Equal(2, resources.Count);
        Assert.Equal(2, handler.RequestCount);
        Assert.All(handler.AuthorizationSchemes, scheme => Assert.Equal("Bearer", scheme));
    }

    [Fact]
    public async Task Inventory_rejects_cross_origin_pagination()
    {
        var handler = new SequencedHandler(
            "{\"value\":[],\"nextLink\":\"https://example.invalid/steal-token\"}");
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ListResourcesAsync(null, CancellationToken.None));
        Assert.Equal(1, handler.RequestCount);
    }

    private static AzureInventoryService CreateService(HttpClient client)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AZURE_TENANT_ID"] = "tenant",
            ["AZURE_SUBSCRIPTION_ID"] = "subscription",
            ["AZURE_RESOURCE_GROUP"] = "resource-group"
        }).Build();
        return new AzureInventoryService(client, configuration, new StaticTokenCredential());
    }

    private sealed class StaticTokenCredential : TokenCredential
    {
        private static readonly AccessToken Token = new("test-token", DateTimeOffset.UtcNow.AddHours(1));
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => Token;
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) => ValueTask.FromResult(Token);
    }

    private sealed class SequencedHandler : HttpMessageHandler
    {
        private readonly Queue<string> _payloads;

        internal SequencedHandler(params string[] payloads)
        {
            _payloads = new Queue<string>(payloads);
        }

        public int RequestCount { get; private set; }
        public List<string?> AuthorizationSchemes { get; } = new List<string?>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            AuthorizationSchemes.Add(request.Headers.Authorization?.Scheme);
            if (_payloads.Count == 0) throw new InvalidOperationException("Unexpected request.");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_payloads.Dequeue(), Encoding.UTF8, "application/json")
            });
        }
    }
}
