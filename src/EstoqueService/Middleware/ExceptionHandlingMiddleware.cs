using EstoqueService.Exceptions;
using System.Text.Json;

namespace EstoqueService.Middleware
{
    /// <summary>
    /// Middleware global de tratamento de exceções.
    /// Centraliza a conversão de exceções de domínio em respostas HTTP
    /// padronizadas (formato ProblemDetails), e garante que erros
    /// inesperados nunca vazem stack trace pro cliente.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ProdutoNaoEncontradoException ex)
            {
                await WriteProblem(context, StatusCodes.Status404NotFound, "Produto não encontrado", ex.Message);
            }
            catch (CodigoJaCadastradoException ex)
            {
                await WriteProblem(context, StatusCodes.Status409Conflict, "Código duplicado", ex.Message);
            }
            catch (SaldoInsuficienteException ex)
            {
                await WriteProblem(context, StatusCodes.Status409Conflict, "Saldo insuficiente", ex.Message);
            }
            catch (DomainException ex)
            {
                await WriteProblem(context, StatusCodes.Status400BadRequest, "Erro de validação", ex.Message);
            }
            catch (Exception ex)
            {
                // Erro inesperado (bug, banco fora do ar, etc.) - loga completo,
                // mas devolve mensagem genérica pro cliente.
                _logger.LogError(ex, "Erro não tratado no EstoqueService");
                await WriteProblem(context, StatusCodes.Status500InternalServerError,
                    "Erro interno", "Ocorreu um erro inesperado. Tente novamente mais tarde.");
            }
        }

        private static async Task WriteProblem(HttpContext context, int statusCode, string title, string detail)
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            var problem = new
            {
                title,
                status = statusCode,
                detail,
                instance = context.Request.Path.Value
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
