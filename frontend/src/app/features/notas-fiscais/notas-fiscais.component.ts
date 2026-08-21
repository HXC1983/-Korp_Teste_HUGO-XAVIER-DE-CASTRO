import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subject, catchError, of, startWith, switchMap } from 'rxjs';
import { NotaFiscal, StatusNotaFiscal } from '../../core/models/nota-fiscal.model';
import { Produto } from '../../core/models/produto.model';
import { NotaFiscalService } from '../../core/services/nota-fiscal.service';
import { ProdutoService } from '../../core/services/produto.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-notas-fiscais',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './notas-fiscais.component.html',
  styleUrl: './notas-fiscais.component.css',
})
export class NotasFiscaisComponent implements OnInit {
  private fb = inject(FormBuilder);
  private notaFiscalService = inject(NotaFiscalService);
  private produtoService = inject(ProdutoService);

  readonly StatusNotaFiscal = StatusNotaFiscal;

  private recarregar$ = new Subject<void>();

  notas$ = this.recarregar$.pipe(
    startWith(undefined),
    switchMap(() =>
      this.notaFiscalService.listar().pipe(
        catchError((erro) => {
          this.mensagemErro = erro.message;
          return of<NotaFiscal[]>([]);
        })
      )
    )
  );

  produtosDisponiveis: Produto[] = [];

  form = this.fb.group({
    itens: this.fb.array([this.criarLinhaItem()]),
  });

  get itens() {
    return this.form.controls.itens;
  }

  salvando = false;
  mensagemErro: string | null = null;
  mensagemSucesso: string | null = null;

  /** Controla, por número de nota, se está em andamento a chamada de impressão. */
  private imprimindo = new Set<number>();

  ngOnInit(): void {
    // Carrega os produtos disponíveis uma vez, para popular os <select> do formulário.
    this.produtoService.listar().subscribe({
      next: (produtos) => (this.produtosDisponiveis = produtos),
      error: (erro) => (this.mensagemErro = erro.message),
    });
  }

  private criarLinhaItem() {
    return this.fb.group({
      codigoProduto: ['', Validators.required],
      quantidade: [1, [Validators.required, Validators.min(1)]],
    });
  }

  adicionarItem(): void {
    this.itens.push(this.criarLinhaItem());
  }

  removerItem(index: number): void {
    if (this.itens.length > 1) {
      this.itens.removeAt(index);
    }
  }

  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.salvando = true;
    this.mensagemErro = null;
    this.mensagemSucesso = null;

    const valor = this.form.getRawValue();

    this.notaFiscalService
      .criar({
        itens: valor.itens.map((i) => ({
          codigoProduto: i.codigoProduto!,
          quantidade: i.quantidade!,
        })),
      })
      .subscribe({
        next: (nota) => {
          this.salvando = false;
          this.mensagemSucesso = `Nota fiscal número ${nota.numero} criada com status Aberta.`;
          this.form.setControl('itens', this.fb.array([this.criarLinhaItem()]));
          this.recarregar$.next();
        },
        error: (erro) => {
          this.salvando = false;
          this.mensagemErro = erro.message;
        },
      });
  }

  isImprimindo(numero: number): boolean {
    return this.imprimindo.has(numero);
  }

  imprimir(nota: NotaFiscal): void {
    this.mensagemErro = null;
    this.imprimindo.add(nota.numero);

    this.notaFiscalService.imprimir(nota.numero).subscribe({
      next: () => {
        this.imprimindo.delete(nota.numero);
        this.mensagemSucesso = `Nota fiscal número ${nota.numero} impressa com sucesso.`;
        this.recarregar$.next();
      },
      error: (erro) => {
        this.imprimindo.delete(nota.numero);
        // Se o EstoqueService estiver fora do ar, a mensagem que chega
        // aqui é a que o Polly + o middleware do FaturamentoService
        // geraram (erro 503) - é esse texto que aparece na tela pro
        // usuário, cumprindo o requisito de "feedback apropriado sobre o erro".
        this.mensagemErro = erro.message;
      },
    });
  }
}
