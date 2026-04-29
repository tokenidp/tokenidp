using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using TokenIDP.Core.Foundation.Security;
using TokenIDP.Server.ApplicationSetup;

namespace TokenIDP.Tests.Security;

public sealed class TokenOptionsResolutionTests
{
    [Fact]
    public void ResolveTokenOptions_ShouldUseDevelopmentKey_InStaging_WhenNoCertificateConfigured()
    {
        var configuration = CreateConfiguration();
        var environment = CreateEnvironment(Environments.Staging);

        var options = ApplicationBuilderExtensions.ResolveTokenOptions(
            configuration,
            environment,
            "tokenidp.admin.api",
            configureToken: null);

        options.Key.Should().Be(TokenKeyDefault.DevelopmentKey);
    }

    [Fact]
    public void ResolveTokenOptions_ShouldRequireCertificate_InProduction_WhenNoCertificateConfigured()
    {
        var configuration = CreateConfiguration();
        var environment = CreateEnvironment(Environments.Production);

        var act = () => ApplicationBuilderExtensions.ResolveTokenOptions(
            configuration,
            environment,
            "tokenidp.admin.api",
            configureToken: null);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Token signing certificate is required in production*");
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TokenOptions:Issuer"] = "https://tresorauth.example.com"
            })
            .Build();
    }

    private static IHostEnvironment CreateEnvironment(string environmentName)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(environmentName);
        return environment.Object;
    }
}
