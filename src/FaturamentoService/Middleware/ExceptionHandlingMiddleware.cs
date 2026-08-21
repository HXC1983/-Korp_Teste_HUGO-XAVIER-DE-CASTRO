using FaturamentoService.Exceptions;
using System.Text.Json;

namespace FaturamentoService.Middleware
{
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
            catch (NotaFiscalNaoEncontradaException ex)
            {
                await WriteProblem(context, StatusCodes.Status404NotFound, "Nota fiscal não encontrada", ex.Message);
            }
            catch (NotaFiscalNaoEstaAbertaException ex)
            {
                await WriteProblem(context, StatusCodes.Status409Conflict, "Operação não permitida", ex.Message);
            }
            catch (ServicoEstoqueIndisponivelException ex)
            {
                // 503 Service Unavailable é o código HTTP correto pra "dependência fora do ar" -
                // sinaliza pro cliente (o Angular) que é um problema temporário, e não um erro
                // definitivo, então vale mostrar "tente novamente" na tela.
                await WriteProblem(context, StatusCodes.Status503ServiceUnavailable, "Serviço indisponível", ex.Message);
            }
            catch (FalhaAoDebitarEstoqueException ex)
            {
                await WriteProblem(context, StatusCodes.Status409Conflict, "Falha ao debitar estoque", ex.Message);
            }
            catch (DomainException ex)
            {
                await WriteProblem(context, StatusCodes.Status400BadRequest, "Erro de validação", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro não tratado no FaturamentoService");
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
