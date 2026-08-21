import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiProblemDetails, CriarNotaFiscalRequest, NotaFiscal } from '../models/nota-fiscal.model';

@Injectable({ providedIn: 'root' })
export class NotaFiscalService {
  private readonly baseUrl = `${environment.faturamentoApiUrl}/notasfiscais`;

  constructor(private http: HttpClient) {}

  listar(): Observable<NotaFiscal[]> {
    return this.http.get<NotaFiscal[]>(this.baseUrl).pipe(catchError(this.tratarErro));
  }

  criar(request: CriarNotaFiscalRequest): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(this.baseUrl, request).pipe(catchError(this.tratarErro));
  }

  imprimir(numero: number): Observable<NotaFiscal> {
    return this.http
      .post<NotaFiscal>(`${this.baseUrl}/${numero}/imprimir`, {})
      .pipe(catchError(this.tratarErro));
  }

  /**
   * Mesma estratégia do ProdutoService: traduz o ProblemDetails do
   * backend em mensagem de texto. Aqui é especialmente importante
   * porque é onde o erro 503 (EstoqueService indisponível, vindo do
   * Polly) precisa chegar de forma clara até a tela.
   */
  private tratarErro(erro: any) {
    const problema = erro?.error as ApiProblemDetails | undefined;
    const mensagem = problema?.detail ?? 'Não foi possível se comunicar com o serviço de Faturamento.';
    return throwError(() => new Error(mensagem));
  }
}
