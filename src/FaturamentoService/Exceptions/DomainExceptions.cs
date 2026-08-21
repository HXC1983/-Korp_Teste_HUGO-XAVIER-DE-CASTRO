namespace FaturamentoService.Exceptions
{
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
    }

    public class NotaFiscalNaoEncontradaException : DomainException
    {
        public NotaFiscalNaoEncontradaException(int numero)
            : base($"Nota fiscal número {numero} não foi encontrada.") { }
    }

    public class NotaFiscalNaoEstaAbertaException : DomainException
    {
        public NotaFiscalNaoEstaAbertaException(int numero)
            : base($"A nota fiscal número {numero} não pode ser impressa " +
                   "porque não está com status 'Aberta'.") { }
    }

    /// <summary>
    /// Lançada quando o EstoqueService está indisponível (fora do ar,
    /// timeout, ou recusando conexão) mesmo após as tentativas de retry
    /// do Polly terem se esgotado. É o requisito de "tratamento de falhas"
    /// do edital: o Faturamento continua de pé, mas informa claramente
    /// ao usuário que a operação não pôde ser concluída por indisponibilidade
    /// de um serviço dependente - a nota permanece Aberta, nada é perdido.
    /// </summary>
    public class ServicoEstoqueIndisponivelException : DomainException
    {
        public ServicoEstoqueIndisponivelException()
            : base("O serviço de Estoque está indisponível no momento. " +
                   "A nota fiscal continua com status 'Aberta' e pode ser " +
                   "impressa novamente assim que o serviço voltar.") { }
    }

    /// <summary>
    /// Lançada quando o EstoqueService respondeu, mas recusou o débito
    /// (ex: saldo insuficiente em algum produto da nota).
    /// </summary>
    public class FalhaAoDebitarEstoqueException : DomainException
    {
        public FalhaAoDebitarEstoqueException(string motivo)
            : base($"Não foi possível debitar o estoque: {motivo}") { }
    }
}
