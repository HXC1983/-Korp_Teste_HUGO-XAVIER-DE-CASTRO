import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CriarProdutoRequest, Produto } from '../models/produto.model';
import { ApiProblemDetails } from '../models/nota-fiscal.model';

@Injectable({ providedIn: 'root' })
export class ProdutoService {
  private readonly baseUrl = `${environment.estoqueApiUrl}/produtos`;

  constructor(private http: HttpClient) {}

  listar(): Observable<Produto[]> {
    return this.http.get<Produto[]>(this.baseUrl).pipe(catchError(this.tratarErro));
  }

  obterPorCodigo(codigo: string): Observable<Produto> {
    return this.http.get<Produto>(`${this.baseUrl}/${codigo}`).pipe(catchError(this.tratarErro));
  }

  criar(request: CriarProdutoRequest): Observable<Produto> {
    return this.http.post<Produto>(this.baseUrl, request).pipe(catchError(this.tratarErro));
  }

  /**
   * Traduz o corpo de erro (ProblemDetails) vindo do backend em uma
   * mensagem de texto simples, ou usa uma mensagem genérica se a API
   * nem respondeu (ex: serviço fora do ar).
   */
  private tratarErro(erro: any) {
    const problema = erro?.error as ApiProblemDetails | undefined;
    const mensagem = problema?.detail ?? 'Não foi possível se comunicar com o serviço de Estoque.';
    return throwError(() => new Error(mensagem));
  }
}
