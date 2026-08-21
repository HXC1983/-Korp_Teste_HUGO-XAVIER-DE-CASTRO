namespace EstoqueService.Exceptions
{
    /// <summary>
    /// Exceção base para erros de domínio conhecidos (esperados),
    /// diferente de erros inesperados (bugs, falha de infraestrutura).
    /// Isso permite ao middleware distinguir "erro de negócio" (400/404/409)
    /// de "erro real" (500).
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
    }

    public class ProdutoNaoEncontradoException : DomainException
    {
        public ProdutoNaoEncontradoException(string codigo)
            : base($"Produto com código '{codigo}' não foi encontrado.") { }
    }

    public class CodigoJaCadastradoException : DomainException
    {
        public CodigoJaCadastradoException(string codigo)
            : base($"Já existe um produto cadastrado com o código '{codigo}'.") { }
    }

    public class SaldoInsuficienteException : DomainException
    {
        public SaldoInsuficienteException(string codigo, int quantidadeSolicitada)
            : base($"Saldo insuficiente para o produto '{codigo}'. " +
                   $"Quantidade solicitada: {quantidadeSolicitada}.") { }
    }
}
