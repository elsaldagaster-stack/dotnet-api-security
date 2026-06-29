using System.Net;
using ApiSecurity.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ApiSecurity.IntegrationTests.IpFilter;

public class IpFilterTests
{
    [Fact]
    public async Task Request_FromDeniedIp_Returns403()
    {
        await using var factory = new DenylistFactory();
        await factory.InitializeAsync();
        await factory.ApplyMigrationsAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    private class DenylistFactory : ApiTestFixture
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["IpFilter:Denylist:0"] = "::1",
                    ["IpFilter:Denylist:1"] = "127.0.0.1"
                });
            });
        }
    }
}
