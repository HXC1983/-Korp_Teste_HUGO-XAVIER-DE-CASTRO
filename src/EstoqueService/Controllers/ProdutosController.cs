using EstoqueService.Data;
using EstoqueService.DTOs;
using EstoqueService.Exceptions;
using EstoqueService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EstoqueService.Controllers
{
    [ApiController]
    [Route("api/produtos")]
    public class ProdutosController : ControllerBase
    {
        private readonly EstoqueDbContext _context;
        private readonly ILogger<ProdutosController> _logger;

        public ProdutosController(EstoqueDbContext context, ILogger<ProdutosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/produtos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProdutoResponse>>> Listar()
        {
            // Uso de LINQ (query syntax + projeção) para trazer só o que a API expõe.
            var produtos = await _context.Produtos
                .OrderBy(p => p.Codigo)
                .Select(p => new ProdutoResponse(p.Id, p.Codigo, p.Descricao, p.Saldo))
                .ToListAsync();

            return Ok(produtos);
        }

        // GET /api/produtos/{codigo}
        [HttpGet("{codigo}")]
        public async Task<ActionResult<ProdutoResponse>> ObterPorCodigo(string codigo)
        {
            var produto = await _context.Produtos
                .Where(p => p.Codigo == codigo)
                .Select(p => new ProdutoResponse(p.Id, p.Codigo, p.Descricao, p.Saldo))
                .FirstOrDefaultAsync();

            if (produto is null)
                throw new ProdutoNaoEncontradoException(codigo);

            return Ok(produto);
        }

        // POST /api/produtos
        [HttpPost]
        public async Task<ActionResult<ProdutoResponse>> Criar(CriarProdutoRequest request)
        {
            bool jaExiste = await _context.Produtos.AnyAsync(p => p.Codigo == request.Codigo);
            if (jaExiste)
                throw new CodigoJaCadastradoException(request.Codigo);

            var produto = new Produto
            {
                Codigo = request.Codigo,
                Descricao = request.Descricao,
                Saldo = request.Saldo
            };

            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();

            var response = new ProdutoResponse(produto.Id, produto.Codigo, produto.Descricao, produto.Saldo);
            return CreatedAtAction(nameof(ObterPorCodigo), new { codigo = produto.Codigo }, response);
        }

        /// <summary>
        /// Debita o saldo de múltiplos produtos de uma vez, de forma atômica
        /// (tudo-ou-nada): usado pelo FaturamentoService ao imprimir uma nota.
        ///
        /// Estratégia de concorrência: cada débito é um UPDATE condicional
        /// (WHERE Saldo >= quantidade) executado diretamente no banco via
        /// ExecuteUpdateAsync, sem carregar a entidade em memória primeiro.
        /// Isso elimina a janela de "leitura -> escrita" onde uma race
        /// condition poderia ocorrer: o próprio SQL Server serializa updates
        /// concorrentes na mesma linha. Se o UPDATE afeta 0 linhas, sabemos
        /// que o saldo era insuficiente (ou o produto não existe) no momento
        /// exato da tentativa - nunca antes, nunca depois.
        ///
        /// Toda a operação roda dentro de uma transação: se qualquer item
        /// falhar por saldo insuficiente, os débitos já aplicados nesta
        /// chamada são revertidos (rollback), garantindo que uma nota nunca
        /// fique "parcialmente" debitada.
        /// </summary>
        [HttpPost("debitar-lote")]
        public async Task<IActionResult> DebitarLote(DebitarLoteRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in request.Itens)
                {
                    int linhasAfetadas = await _context.Produtos
                        .Where(p => p.Codigo == item.Codigo && p.Saldo >= item.Quantidade)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.Saldo, p => p.Saldo - item.Quantidade));

                    if (linhasAfetadas == 0)
                    {
                        bool existe = await _context.Produtos.AnyAsync(p => p.Codigo == item.Codigo);
                        if (!existe)
                            throw new ProdutoNaoEncontradoException(item.Codigo);

                        throw new SaldoInsuficienteException(item.Codigo, item.Quantidade);
                    }
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Débito em lote realizado com sucesso para {Quantidade} itens", request.Itens.Count);
                return NoContent();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw; // o middleware global converte em resposta HTTP apropriada
            }
        }
    }
}
