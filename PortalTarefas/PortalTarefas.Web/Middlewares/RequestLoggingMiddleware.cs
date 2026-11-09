using System.Diagnostics;

namespace PortalTarefas.Web.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var headerValue = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();

        var correlationId = Guid.TryParse(headerValue, out var parsedId)
            ? parsedId.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers["X-Correlation-ID"] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier
        }))
        {
            _logger.LogInformation(
                "Início da requisição {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Falha na requisição {Method} {Path}. Tipo: {ExceptionType}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.GetType().Name);

                throw;
            }
            finally
            {
                stopwatch.Stop();

                _logger.LogInformation(
                    "Fim da requisição {Method} {Path}. Status: {StatusCode}. Duração: {ElapsedMilliseconds} ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
