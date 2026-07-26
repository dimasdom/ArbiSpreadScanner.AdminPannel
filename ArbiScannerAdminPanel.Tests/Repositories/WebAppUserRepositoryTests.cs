using ArbiScannerAdminPanel.Infrastructure.Repositories;
using ArbiScannerAdminPanel.Tests.Helpers;
using ArbiScannerWeb.Domain.Models;
using FluentAssertions;

namespace ArbiScannerAdminPanel.Tests.Repositories;

public class WebAppUserRepositoryTests
{
    private static AccountModel CreateUser(string id, string userName, string email) => new()
    {
        Id = id,
        UserName = userName,
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        NormalizedUserName = userName.ToUpperInvariant(),
        UserSettings = new UserSettingsModel { AccountId = id }
    };

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        var sut = new WebAppUserRepository(context);

        var result = await sut.GetById("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetById_Found_MapsDto()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        context.Users.Add(CreateUser("u1", "alice", "alice@test.com"));
        await context.SaveChangesAsync();
        var sut = new WebAppUserRepository(context);

        var result = await sut.GetById("u1");

        result.Should().NotBeNull();
        result!.UserName.Should().Be("alice");
        result.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public async Task GetByEmail_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        var sut = new WebAppUserRepository(context);

        var result = await sut.GetByEmail("missing@test.com");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmail_IsCaseInsensitive()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        context.Users.Add(CreateUser("u1", "alice", "Alice@Test.com"));
        await context.SaveChangesAsync();
        var sut = new WebAppUserRepository(context);

        var result = await sut.GetByEmail("alice@test.com");

        result.Should().NotBeNull();
        result!.Id.Should().Be("u1");
    }

    [Fact]
    public async Task GetUsers_PagesResultsOrderedByUserName()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        for (var i = 0; i < 25; i++)
        {
            context.Users.Add(CreateUser($"u{i}", $"user{i:D2}", $"user{i}@test.com"));
        }
        await context.SaveChangesAsync();
        var sut = new WebAppUserRepository(context);

        var page1 = await sut.GetUsers(1);
        var page2 = await sut.GetUsers(2);

        page1.Should().HaveCount(20);
        page2.Should().HaveCount(5);
        page1[0].UserName.Should().Be("user00");
    }

    [Fact]
    public async Task GetUsers_NonPositivePage_NormalizesToFirstPage()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        context.Users.Add(CreateUser("u1", "alice", "alice@test.com"));
        await context.SaveChangesAsync();
        var sut = new WebAppUserRepository(context);

        var result = await sut.GetUsers(0);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchByEmail_ReturnsMatchesOrderedByEmail_CaseInsensitive()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        context.Users.AddRange(
            CreateUser("u1", "bob", "bob@example.com"),
            CreateUser("u2", "alice", "ALICE@example.com"),
            CreateUser("u3", "carol", "carol@other.com"));
        await context.SaveChangesAsync();
        var sut = new WebAppUserRepository(context);

        var result = await sut.SearchByEmail("example.com");

        result.Should().HaveCount(2);
        result.Select(u => u.Id).Should().Contain(new[] { "u1", "u2" });
    }

    [Fact]
    public async Task UpdateUser_NotFound_Throws()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        var sut = new WebAppUserRepository(context);

        var act = () => sut.UpdateUser("missing", "a@test.com", "name");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateUser_UpdatesEmailAndUserName()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        context.Users.Add(CreateUser("u1", "alice", "alice@test.com"));
        await context.SaveChangesAsync();
        var sut = new WebAppUserRepository(context);

        await sut.UpdateUser("u1", "new@test.com", "new-name");

        var updated = context.Users.Single(u => u.Id == "u1");
        updated.Email.Should().Be("new@test.com");
        updated.NormalizedEmail.Should().Be("NEW@TEST.COM");
        updated.UserName.Should().Be("new-name");
        updated.NormalizedUserName.Should().Be("NEW-NAME");
    }

    [Fact]
    public async Task UpdateUser_NullFields_LeavesExistingValues()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        context.Users.Add(CreateUser("u1", "alice", "alice@test.com"));
        await context.SaveChangesAsync();
        var sut = new WebAppUserRepository(context);

        await sut.UpdateUser("u1", null, null);

        var updated = context.Users.Single(u => u.Id == "u1");
        updated.Email.Should().Be("alice@test.com");
        updated.UserName.Should().Be("alice");
    }

    [Fact]
    public async Task DeleteUser_NotFound_Throws()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        var sut = new WebAppUserRepository(context);

        var act = () => sut.DeleteUser("missing");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteUser_RemovesUser()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        context.Users.Add(CreateUser("u1", "alice", "alice@test.com"));
        await context.SaveChangesAsync();
        var sut = new WebAppUserRepository(context);

        await sut.DeleteUser("u1");

        context.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteUsers_NoMatches_Throws()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        var sut = new WebAppUserRepository(context);

        var act = () => sut.DeleteUsers(new List<string> { "missing" });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteUsers_RemovesMatchingUsers()
    {
        using var context = DbContextFactory.CreateAppDbContext();
        context.Users.AddRange(
            CreateUser("u1", "alice", "alice@test.com"),
            CreateUser("u2", "bob", "bob@test.com"));
        await context.SaveChangesAsync();
        var sut = new WebAppUserRepository(context);

        await sut.DeleteUsers(new List<string> { "u1" });

        context.Users.Should().ContainSingle(u => u.Id == "u2");
    }
}
