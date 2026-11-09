using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace PortalTarefas.Web.Data;

public static class IdentitySeeder
{
    public static async Task InicializarAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<IdentityUser>>();

        var adminEmail = configuration["SeedUsers:AdminEmail"];
        var adminPassword = configuration["SeedUsers:AdminPassword"];
        var userEmail = configuration["SeedUsers:UserEmail"];
        var userPassword = configuration["SeedUsers:UserPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword) ||
            string.IsNullOrWhiteSpace(userEmail) ||
            string.IsNullOrWhiteSpace(userPassword))
        {
            logger.LogWarning(
                "Usuários de teste não foram criados. Configure os User Secrets.");
            return;
        }

        foreach (var role in new[] { "Usuario", "Administrador" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var resultado = await roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!resultado.Succeeded)
                    throw new Exception(string.Join("; ",
                        resultado.Errors.Select(e => e.Description)));
            }
        }

        await CriarUsuarioAsync(
            userManager, userEmail, userPassword, "Usuario");

        var admin = await CriarUsuarioAsync(
            userManager, adminEmail, adminPassword, "Administrador");

        var claims = await userManager.GetClaimsAsync(admin);

        if (!claims.Any(c =>
            c.Type == "Permissao" &&
            c.Value == "EditarTarefasEquipe"))
        {
            await userManager.AddClaimAsync(
                admin,
                new Claim("Permissao", "EditarTarefasEquipe"));
        }

        logger.LogInformation("Usuários e permissões de teste verificados.");
    }

    private static async Task<IdentityUser> CriarUsuarioAsync(
        UserManager<IdentityUser> userManager,
        string email,
        string password,
        string role)
    {
        var usuario = await userManager.FindByEmailAsync(email);

        if (usuario is null)
        {
            usuario = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var criacao = await userManager.CreateAsync(usuario, password);

            if (!criacao.Succeeded)
                throw new Exception(string.Join("; ",
                    criacao.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(usuario, role))
        {
            var resultado = await userManager.AddToRoleAsync(usuario, role);

            if (!resultado.Succeeded)
                throw new Exception(string.Join("; ",
                    resultado.Errors.Select(e => e.Description)));
        }

        return usuario;
    }
}
