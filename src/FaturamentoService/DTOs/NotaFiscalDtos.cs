using System.ComponentModel.DataAnnotations;
using FaturamentoService.Models;

namespace FaturamentoService.DTOs
{
    public record ItemNotaFiscalResponse(string CodigoProduto, int Quantidade);

    public record NotaFiscalResponse(
        int Numero,
        StatusNotaFiscal Status,
        DateTime DataCriacao,
        DateTime? DataFechamento,
        List<ItemNotaFiscalResponse> Itens);

    public class ItemNotaFiscalRequest
    {
        [Required, MaxLength(50)]
        public string CodigoProduto { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantidade { get; set; }
    }

    public class CriarNotaFiscalRequest
    {
        [Required, MinLength(1, ErrorMessage = "A nota precisa ter ao menos um item.")]
        public List<ItemNotaFiscalRequest> Itens { get; set; } = new();
    }
}
