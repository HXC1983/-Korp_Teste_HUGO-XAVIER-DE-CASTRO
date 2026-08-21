using System.ComponentModel.DataAnnotations;

namespace FaturamentoService.Models
{
    public enum StatusNotaFiscal
    {
        Aberta = 0,
        Fechada = 1
    }

    /// <summary>
    /// Nota fiscal: numeração sequencial, status (Aberta/Fechada) e
    /// uma coleção de itens (produto + quantidade).
    /// </summary>
    public class NotaFiscal
    {
        public int Id { get; set; }

        /// <summary>
        /// Numeração sequencial da nota. Gerada pelo serviço no momento
        /// da criação (ver NotasFiscaisController.Criar) - nunca informada
        /// pelo cliente, pra garantir que a sequência não tenha furos nem
        /// duplicidade mesmo com requisições concorrentes.
        /// </summary>
        public int Numero { get; set; }

        public StatusNotaFiscal Status { get; set; } = StatusNotaFiscal.Aberta;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public DateTime? DataFechamento { get; set; }

        public List<ItemNotaFiscal> Itens { get; set; } = new();
    }

    /// <summary>
    /// Um item dentro da nota: qual produto (referenciado só pelo Código,
    /// já que o produto "de verdade" mora no EstoqueService - este serviço
    /// não tem tabela de Produtos própria, só guarda a referência) e a
    /// quantidade utilizada.
    /// </summary>
    public class ItemNotaFiscal
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string CodigoProduto { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantidade { get; set; }

        public int NotaFiscalId { get; set; }
        public NotaFiscal? NotaFiscal { get; set; }
    }
}
