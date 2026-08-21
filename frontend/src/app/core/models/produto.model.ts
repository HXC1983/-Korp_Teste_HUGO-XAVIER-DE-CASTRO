export interface Produto {
  id: number;
  codigo: string;
  descricao: string;
  saldo: number;
}

export interface CriarProdutoRequest {
  codigo: string;
  descricao: string;
  saldo: number;
}
