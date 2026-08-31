using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using TokenIDP.Server.ApplicationSetup;

namespace TokenIDP.Tests.Security;

public sealed class TokenOptionsResolutionTests
{
    [Fact]
    public void ResolveTokenOptions_ShouldGenerateEphemeralKey_InDevelopment_WhenNoSigningMaterialConfigured()
    {
        var configuration = CreateConfiguration();
        var environment = CreateEnvironment(Environments.Development);

        var options = ApplicationBuilderExtensions.ResolveTokenOptions(
            configuration,
            environment,
            "tokenidp.admin.api",
            configureToken: null);

        options.Key.Should().StartWith("-----BEGIN PRIVATE KEY-----");
    }

    [Fact]
    public void ResolveTokenOptions_ShouldGenerateDifferentKeys_ForSeparateDevelopmentStarts()
    {
        var configuration = CreateConfiguration();
        var environment = CreateEnvironment(Environments.Development);

        var first = ApplicationBuilderExtensions.ResolveTokenOptions(
            configuration,
            environment,
            "tokenidp.admin.api",
            configureToken: null);
        var second = ApplicationBuilderExtensions.ResolveTokenOptions(
            configuration,
            environment,
            "tokenidp.admin.api",
            configureToken: null);

        first.Key.Should().NotBe(second.Key);
    }

    [Fact]
    public void ResolveTokenOptions_ShouldRequireSigningMaterial_InStaging()
    {
        var configuration = CreateConfiguration();
        var environment = CreateEnvironment(Environments.Staging);

        var act = () => ApplicationBuilderExtensions.ResolveTokenOptions(
            configuration,
            environment,
            "tokenidp.admin.api",
            configureToken: null);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Token signing material is required outside Development*");
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
                ["TokenOptions:Issuer"] = "https://id.example.com"
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
