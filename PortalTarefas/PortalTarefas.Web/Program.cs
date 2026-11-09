using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PortalTarefas.Web.Observability;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using PortalTarefas.Web.Middlewares;
using PortalTarefas.Web.Models;
using PortalTarefas.Web.Services;
using PortalTarefas.Web.Filters;
using PortalTarefas.Web.Data;
using PortalTarefas.Web.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PortalTarefas");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "A configuração obrigatória da conexão com o banco não foi fornecida.");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
});


// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendLocal", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "https://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Observabilidade com OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter(PortalTelemetry.MeterName)
        .AddConsoleExporter())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(PortalTelemetry.ActivitySourceName)
        .AddConsoleExporter());

// Serviços da aplicação
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
builder.Services.AddScoped<TarefaService>();
builder.Services.AddScoped<ActionLogFilter>();

// Banco de dados
builder.Services.AddDbContextPool<PortalTarefasDbContext>(options =>
    options.UseSqlite(
        connectionString));

// Verificações de saúde da aplicação e do banco
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PortalTarefasDbContext>(
        "banco-de-dados",
        tags: new[] { "ready" });

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<PortalTarefasDbContext>()
.AddDefaultTokenProviders();

// Configuração do cookie de autenticação
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-PortalTarefas.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// Proteção contra CSRF
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-PortalTarefas.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
});

// Políticas de autorização
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TarefasPrivadas", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("CriarTarefa", policy =>
        policy.RequireRole("Usuario", "Administrador"));

    options.AddPolicy("GerenciarTarefas", policy =>
        policy.RequireRole("Administrador")
              .RequireClaim("Permissao", "EditarTarefasEquipe"));
});

// Limitação de requisições da API
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("tarefas", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromSeconds(10);
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Inicialização do banco e carga inicial
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<PortalTarefasDbContext>();

    await context.Database.MigrateAsync();
    await DatabaseInitializer.InicializarAsync(context);
}

// Criação dos usuários e perfis de teste somente em desenvolvimento
if (app.Environment.IsDevelopment())
{
    await IdentitySeeder.InicializarAsync(
        app.Services,
        app.Configuration,
        app.Logger);
}

app.UseExceptionHandler("/erro");
app.UseRequestLogging();
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("FrontendLocal");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Rotas gerais
app.MapGet("/erro", () =>
    Results.Problem("Ocorreu um erro interno na aplicacao."));

app.MapGet("/", () => "Portal de Tarefas funcionando!");

// Listar tarefas (rota antiga protegida)
app.MapGet("/tarefas", async (
    PortalTarefasDbContext context,
    string? situacao) =>
{
    var consulta = context.Tarefas.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(situacao))
        consulta = consulta.Where(t => t.Situacao == situacao);

    return Results.Ok(await consulta.ToListAsync());
}).RequireAuthorization("TarefasPrivadas");

// Buscar tarefa por ID (rota antiga protegida)
app.MapGet("/tarefas/{id:int}", async (
    int id,
    PortalTarefasDbContext context) =>
{
    var tarefa = await context.Tarefas.FindAsync(id);

    return tarefa is null
        ? Results.NotFound()
        : Results.Ok(tarefa);
}).RequireAuthorization("TarefasPrivadas");

// Cadastrar tarefa (rota antiga protegida e com CSRF)
app.MapPost("/tarefas", async (
    Tarefa tarefa,
    HttpRequest request,
    PortalTarefasDbContext context) =>
{
    var cliente = request.Headers["X-Cliente"].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(tarefa.Titulo))
        return Results.BadRequest("O titulo e obrigatorio.");

    tarefa.Id = 0;

    await context.Tarefas.AddAsync(tarefa);
    await context.SaveChangesAsync();

    return Results.Created($"/tarefas/{tarefa.Id}", new
    {
        Cliente = cliente,
        Tarefa = tarefa
    });
})
.AddEndpointFilter(async (context, next) =>
{
    var antiforgery = context.HttpContext.RequestServices
        .GetRequiredService<IAntiforgery>();

    try
    {
        await antiforgery.ValidateRequestAsync(context.HttpContext);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.Problem(
            title: "Token de segurança inválido",
            statusCode: 400);
    }

    return await next(context);
})
.RequireAuthorization("CriarTarefa");

// Health Checks: respostas públicas sem detalhes internos
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString()
        });
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString()
        });
    }
});

// APIs
app.MapAuthEndpoints();
app.MapTarefaEndpoints();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
