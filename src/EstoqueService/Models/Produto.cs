using System.ComponentModel.DataAnnotations;

namespace EstoqueService.Models
{
    /// <summary>
    /// Representa um produto cadastrado no estoque.
    /// Campos obrigatórios conforme especificação: Código, Descrição, Saldo.
    /// </summary>
    public class Produto
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Descricao { get; set; } = string.Empty;

        /// <summary>
        /// Quantidade disponível em estoque. Nunca deve ficar negativo -
        /// a garantia disso é feita no banco via UPDATE condicional
        /// (ver EstoqueDbContext / ProdutosController.DebitarLote), não em memória.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int Saldo { get; set; }
    }
}
