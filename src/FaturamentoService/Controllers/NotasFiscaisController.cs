using FaturamentoService.Data;
using FaturamentoService.DTOs;
using FaturamentoService.Exceptions;
using FaturamentoService.Models;
using FaturamentoService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaturamentoService.Controllers
{
    [ApiController]
    [Route("api/notasfiscais")]
    public class NotasFiscaisController : ControllerBase
    {
        private readonly FaturamentoDbContext _context;
        private readonly IEstoqueClient _estoqueClient;
        private readonly ILogger<NotasFiscaisController> _logger;

        public NotasFiscaisController(
            FaturamentoDbContext context,
            IEstoqueClient estoqueClient,
            ILogger<NotasFiscaisController> logger)
        {
            _context = context;
            _estoqueClient = estoqueClient;
            _logger = logger;
        }

        // GET /api/notasfiscais
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotaFiscalResponse>>> Listar()
        {
            var notas = await _context.NotasFiscais
                .Include(n => n.Itens)
                .OrderByDescending(n => n.Numero)
                .Select(n => MapToResponse(n))
                .ToListAsync();

            return Ok(notas);
        }

        // GET /api/notasfiscais/{numero}
        [HttpGet("{numero:int}")]
        public async Task<ActionResult<NotaFiscalResponse>> ObterPorNumero(int numero)
        {
            var nota = await BuscarNotaOuFalhar(numero);
            return Ok(MapToResponse(nota));
        }

        /// <summary>
        /// Cria uma nova nota fiscal com status inicial "Aberta".
        /// A numeração sequencial é calculada aqui (maior número existente + 1),
        /// dentro de uma transação, pra evitar duas notas com o mesmo número
        /// em caso de requisições concorrentes.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<NotaFiscalResponse>> Criar(CriarNotaFiscalRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // LINQ: agregação para calcular o próximo número sequencial.
            int proximoNumero = (await _context.NotasFiscais.MaxAsync(n => (int?)n.Numero) ?? 0) + 1;

            var nota = new NotaFiscal
            {
                Numero = proximoNumero,
                Status = StatusNotaFiscal.Aberta,
                Itens = request.Itens.Select(i => new ItemNotaFiscal
                {
                    CodigoProduto = i.CodigoProduto,
                    Quantidade = i.Quantidade
                }).ToList()
            };

            _context.NotasFiscais.Add(nota);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(ObterPorNumero), new { numero = nota.Numero }, MapToResponse(nota));
        }

        /// <summary>
        /// "Imprime" a nota: valida que está Aberta, chama o EstoqueService
        /// pra debitar todos os itens de uma vez (all-or-nothing), e só então
        /// marca a nota como Fechada.
        ///
        /// Se o EstoqueService estiver indisponível ou recusar o débito, a
        /// nota permanece Aberta - nenhum estado parcial/inconsistente é salvo.
        /// </summary>
        [HttpPost("{numero:int}/imprimir")]
        public async Task<ActionResult<NotaFiscalResponse>> Imprimir(int numero)
        {
            var nota = await BuscarNotaOuFalhar(numero);

            if (nota.Status != StatusNotaFiscal.Aberta)
                throw new NotaFiscalNaoEstaAbertaException(numero);

            var itensDebito = nota.Itens.Select(i => new ItemDebitoDto(i.CodigoProduto, i.Quantidade));

            // Se isso lançar (ServicoEstoqueIndisponivelException ou
            // FalhaAoDebitarEstoqueException), a execução para aqui e o
            // middleware global converte em resposta HTTP - a nota nunca
            // chega a ser marcada como Fechada.
            await _estoqueClient.DebitarLoteAsync(itensDebito);

            nota.Status = StatusNotaFiscal.Fechada;
            nota.DataFechamento = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Nota fiscal {Numero} impressa e fechada com sucesso", numero);
            return Ok(MapToResponse(nota));
        }

        private async Task<NotaFiscal> BuscarNotaOuFalhar(int numero)
        {
            var nota = await _context.NotasFiscais
                .Include(n => n.Itens)
                .FirstOrDefaultAsync(n => n.Numero == numero);

            return nota ?? throw new NotaFiscalNaoEncontradaException(numero);
        }

        private static NotaFiscalResponse MapToResponse(NotaFiscal n) => new(
            n.Numero,
            n.Status,
            n.DataCriacao,
            n.DataFechamento,
            n.Itens.Select(i => new ItemNotaFiscalResponse(i.CodigoProduto, i.Quantidade)).ToList());
    }
}
