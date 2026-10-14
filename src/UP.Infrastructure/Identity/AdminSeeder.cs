using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using UP.Application.Auth;
using UP.Infrastructure.Auth;

namespace UP.Infrastructure.Identity;

internal sealed class AdminSeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<AdminOptions> options,
    TimeProvider timeProvider,
    ILogger<AdminSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        AdminOptions admin = options.Value;
        if (!admin.IsConfigured)
        {
            logger.LogAdminSeedingSkipped();
            return;
        }

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        RoleManager<IdentityRole<Guid>> roleManager =
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureAdminRoleAsync(roleManager);

        ApplicationUser? user = await userManager.FindByEmailAsync(admin.Email);
        if (user is null)
        {
            user = await CreateAdminAsync(userManager, admin);
            logger.LogAdminSeeded(user.Id);
        }
        else
        {
            logger.LogAdminAlreadyExists(user.Id);
        }

        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, Roles.Admin), "assign the admin role");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureAdminRoleAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        if (await roleManager.RoleExistsAsync(Roles.Admin))
        {
            return;
        }

        var role = new IdentityRole<Guid>(Roles.Admin) { Id = Guid.CreateVersion7() };
        EnsureSucceeded(await roleManager.CreateAsync(role), "create the admin role");
    }

    private async Task<ApplicationUser> CreateAdminAsync(UserManager<ApplicationUser> userManager, AdminOptions admin)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(now),
            UserName = admin.Email,
            Email = admin.Email,
            EmailConfirmed = true,
            CreatedAt = now,
        };

        EnsureSucceeded(await userManager.CreateAsync(user, admin.Password), "create the admin user");
        return user;
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            string errors = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not {action}: {errors}");
        }
    }
}
