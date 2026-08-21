using System.Net.Http.Json;
using FaturamentoService.Exceptions;
using Polly.CircuitBreaker;

namespace FaturamentoService.Services
{
    public record ItemDebitoDto(string CodigoProduto, int Quantidade);

    public interface IEstoqueClient
    {
        Task DebitarLoteAsync(IEnumerable<ItemDebitoDto> itens, CancellationToken ct = default);
    }

    /// <summary>
    /// Cliente HTTP para o EstoqueService. As políticas de resiliência
    /// (retry + circuit breaker) são configuradas centralmente no
    /// Program.cs via AddPolicyHandler - esta classe não precisa saber
    /// dos detalhes de Polly, só faz a chamada e traduz o resultado
    /// em exceções de domínio que o resto da aplicação entende.
    /// </summary>
    public class EstoqueClient : IEstoqueClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EstoqueClient> _logger;

        public EstoqueClient(HttpClient httpClient, ILogger<EstoqueClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task DebitarLoteAsync(IEnumerable<ItemDebitoDto> itens, CancellationToken ct = default)
        {
            var payload = new
            {
                itens = itens.Select(i => new { codigo = i.CodigoProduto, quantidade = i.Quantidade })
            };

            HttpResponseMessage response;
            try
            {
                // Se o Polly esgotar as tentativas de retry (serviço fora do ar,
                // timeout persistente) ou o circuito estiver aberto, isso lança
                // uma exceção de rede (HttpRequestException / TaskCanceledException)
                // ANTES mesmo de chegar aqui - é capturada abaixo.
                response = await _httpClient.PostAsJsonAsync("api/produtos/debitar-lote", payload, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or BrokenCircuitException)
            {
                _logger.LogWarning(ex, "EstoqueService indisponível após tentativas de retry/circuit breaker");
                throw new ServicoEstoqueIndisponivelException();
            }

            if (!response.IsSuccessStatusCode)
            {
                var detalhe = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("EstoqueService recusou o débito ({Status}): {Detalhe}",
                    response.StatusCode, detalhe);
                throw new FalhaAoDebitarEstoqueException(detalhe);
            }
        }
    }
}
