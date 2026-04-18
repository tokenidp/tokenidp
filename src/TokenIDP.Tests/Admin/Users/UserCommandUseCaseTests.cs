using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Users;
using TokenIDP.Core.Admin.Users.UseCases;
using TokenIDP.Domain.AggregateRoots.Tenants;
using TokenIDP.Domain.AggregateRoots.Users;

namespace TokenIDP.Tests.Users;

public class UserCommandUseCaseTests
{
    [Fact]
    public async Task CreateUser_ShouldMarkEmailConfirmed_WhenTenantDoesNotRequireVerification()
    {
        User? createdUser = null;
        var sut = CreateSut(
            requireEmailVerification: false,
            capturedUser: user => createdUser = user);

        var request = CreateRequest(emailConfirmed: false);

        var result = await sut.CreateUser(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        createdUser.Should().NotBeNull();
        createdUser!.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_ShouldKeepEmailUnconfirmed_WhenTenantRequiresVerification()
    {
        User? createdUser = null;
        var sut = CreateSut(
            requireEmailVerification: true,
            capturedUser: user => createdUser = user);

        var request = CreateRequest(emailConfirmed: true);

        var result = await sut.CreateUser(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        createdUser.Should().NotBeNull();
        createdUser!.EmailConfirmed.Should().BeFalse();
    }

    private static UserCommandUseCase CreateSut(
        bool requireEmailVerification,
        Action<User> capturedUser)
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.SetupGet(x => x.TenantId).Returns(7);
        currentUserService.SetupGet(x => x.UserId).Returns(42);

        var tenantAuth = TenantAuthSetting.Create(7);
        if (requireEmailVerification)
        {
            tenantAuth.RequireVerifiedEmail();
        }
        else
        {
            tenantAuth.AllowUnverifiedEmail();
        }

        var tenantRepository = new Mock<ITenantRepository>();
        tenantRepository
            .Setup(x => x.GetTenantAuthSettingAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantAuth);

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.UserNameExistsAsync(7, 0, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userRepository
            .Setup(x => x.EmailExistsAsync(7, 0, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userRepository
            .Setup(x => x.CreateUser(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((user, _) => capturedUser(user))
            .ReturnsAsync(1);

        var logger = new Mock<IAppLogger<UserCommandUseCase>>();

        var userCodeGenerator = new Mock<ICodeSequenceGenerator>();
        userCodeGenerator
            .Setup(x => x.NextUserCodeAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(123);

        var normalizer = new Mock<ILookupNormalizer>();
        normalizer.Setup(x => x.NormalizeName(It.IsAny<string>()))
            .Returns<string>(value => value.Trim().ToUpperInvariant());
        normalizer.Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(value => value.Trim().ToUpperInvariant());

        return new UserCommandUseCase(
            currentUserService.Object,
            tenantRepository.Object,
            logger.Object,
            userCodeGenerator.Object,
            new UserNormalizationService(normalizer.Object),
            normalizer.Object,
            userRepository.Object);
    }

    private static UserDetail CreateRequest(bool emailConfirmed)
    {
        return new UserDetail
        {
            FirstName = "Ava",
            LastName = "Admin",
            UserName = "ava.admin",
            Email = "ava.admin@example.com",
            Phone = "+1-555-1000",
            Password = "Pass@word1",
            Roles = new[] { 3 },
            Status = UserStatus.Active.ToString(),
            EmailConfirmed = emailConfirmed,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = false,
            AccessFailedCount = 0,
            Addresses = new List<UserAddressDetail>(),
            Contacts = new List<UserContactDetail>()
        };
    }
}
