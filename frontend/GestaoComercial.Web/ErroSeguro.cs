using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace GestaoComercial.Web;

public sealed class ErroSeguro(ILogger<ErroSeguro> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested) return false;
        logger.LogError("Falha inesperada. TraceId: {TraceId}; Tipo: {Tipo}", context.TraceIdentifier, exception.GetType().Name);
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = 500, Title = "Erro inesperado", Detail = "Não foi possível concluir a solicitação.", Type = "urn:gestaocomercial:erro:inesperado", Instance = context.Request.Path, Extensions = { ["code"] = "ERRO_INESPERADO", ["traceId"] = context.TraceIdentifier } }, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
