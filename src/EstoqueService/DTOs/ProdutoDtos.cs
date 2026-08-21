using System.ComponentModel.DataAnnotations;

namespace EstoqueService.DTOs
{
    public record ProdutoResponse(int Id, string Codigo, string Descricao, int Saldo);

    public class CriarProdutoRequest
    {
        [Required, MaxLength(50)]
        public string Codigo { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Descricao { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Saldo { get; set; }
    }

    /// <summary>Um item dentro de um pedido de débito em lote.</summary>
    public class ItemDebito
    {
        [Required]
        public string Codigo { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantidade { get; set; }
    }

    /// <summary>
    /// Requisição enviada pelo FaturamentoService ao "imprimir" uma nota:
    /// debitar vários produtos de uma vez, de forma tudo-ou-nada.
    /// </summary>
    public class DebitarLoteRequest
    {
        [Required, MinLength(1)]
        public List<ItemDebito> Itens { get; set; } = new();
    }
}
