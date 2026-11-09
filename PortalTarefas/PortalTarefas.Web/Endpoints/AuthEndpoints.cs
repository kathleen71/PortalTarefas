using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace PortalTarefas.Web.Endpoints;

public record LoginRequest(string Email, string Senha);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/auth")
            .WithTags("Autenticação")
            .AddEndpointFilter(async (context, next) =>
            {
                if (HttpMethods.IsPost(context.HttpContext.Request.Method))
                {
                    var antiforgery = context.HttpContext.RequestServices
                        .GetRequiredService<IAntiforgery>();

                    try
                    {
                        await antiforgery.ValidateRequestAsync(
                            context.HttpContext);
                    }
                    catch (AntiforgeryValidationException)
                    {
                        return Results.Problem(
                            title: "Token de segurança inválido",
                            detail: "Atualize o token CSRF e tente novamente.",
                            statusCode: 400);
                    }
                }

                return await next(context);
            });

        grupo.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { token = tokens.RequestToken });
        }).AllowAnonymous();

        grupo.MapPost("/login", async (
            LoginRequest request,
            SignInManager<IdentityUser> signIn) =>
        {
            var resultado = await signIn.PasswordSignInAsync(
                request.Email,
                request.Senha,
                isPersistent: false,
                lockoutOnFailure: true);

            return resultado.Succeeded
                ? Results.Ok(new { mensagem = "Login realizado com sucesso." })
                : Results.Unauthorized();
        }).AllowAnonymous();

        grupo.MapGet("/me", async (
            ClaimsPrincipal principal,
            UserManager<IdentityUser> userManager) =>
        {
            if (principal.Identity?.IsAuthenticated != true)
                return Results.Ok(new { autenticado = false });

            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var usuario = id is null
                ? null
                : await userManager.FindByIdAsync(id);

            if (usuario is null)
                return Results.Ok(new { autenticado = false });

            var roles = await userManager.GetRolesAsync(usuario);

            var permissoes = principal
                .FindAll("Permissao")
                .Select(c => c.Value)
                .ToArray();

            return Results.Ok(new
            {
                autenticado = true,
                email = usuario.Email,
                roles,
                permissoes
            });
        }).AllowAnonymous();

        grupo.MapPost("/logout", async (
            SignInManager<IdentityUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.Ok(new { mensagem = "Logout realizado." });
        }).RequireAuthorization();
    }
}
