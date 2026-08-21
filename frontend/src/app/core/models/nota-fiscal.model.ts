export enum StatusNotaFiscal {
  Aberta = 0,
  Fechada = 1,
}

export interface ItemNotaFiscal {
  codigoProduto: string;
  quantidade: number;
}

export interface NotaFiscal {
  numero: number;
  status: StatusNotaFiscal;
  dataCriacao: string;
  dataFechamento: string | null;
  itens: ItemNotaFiscal[];
}

export interface CriarNotaFiscalRequest {
  itens: ItemNotaFiscal[];
}

/**
 * Formato de erro devolvido pelo middleware de exceções do backend
 * (ProblemDetails simplificado). Usado pelo interceptor/serviços para
 * mostrar mensagens de erro amigáveis ao usuário.
 */
export interface ApiProblemDetails {
  title: string;
  status: number;
  detail: string;
  instance: string;
}
