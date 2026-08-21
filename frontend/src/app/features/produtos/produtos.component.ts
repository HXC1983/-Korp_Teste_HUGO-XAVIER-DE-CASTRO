import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  BehaviorSubject,
  Subject,
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  finalize,
  map,
  of,
  startWith,
  switchMap,
} from 'rxjs';
import { Produto } from '../../core/models/produto.model';
import { ProdutoService } from '../../core/services/produto.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-produtos',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './produtos.component.html',
  styleUrl: './produtos.component.css',
})
export class ProdutosComponent implements OnInit {
  private fb = inject(FormBuilder);
  private produtoService = inject(ProdutoService);

  /**
   * Subject "gatilho": sempre que emite um valor, a lista é recarregada
   * do backend. Usamos isso (em vez de simplesmente chamar o serviço de
   * novo manualmente após criar um produto) para manter um único fluxo
   * reativo de "fonte da verdade" para a tabela.
   */
  private recarregar$ = new Subject<void>();

  private produtos$ = this.recarregar$.pipe(
    startWith(undefined), // dispara a primeira carga assim que o componente inicia
    switchMap(() =>
      this.produtoService.listar().pipe(
        catchError((erro) => {
          this.mensagemErro = erro.message;
          return of<Produto[]>([]);
        })
      )
    )
  );

  /** Campo de busca. debounceTime + distinctUntilChanged evitam refiltrar a cada tecla. */
  filtroCtrl = this.fb.control('');

  private filtro$ = this.filtroCtrl.valueChanges.pipe(
    startWith(''),
    debounceTime(250),
    distinctUntilChanged()
  );

  /**
   * combineLatest: sempre que a lista OU o filtro mudar, recalcula o
   * resultado exibido. Exemplo prático de composição de streams em RxJS.
   */
  produtosFiltrados$ = combineLatest([this.produtos$, this.filtro$]).pipe(
    map(([produtos, filtro]) => {
      const termo = (filtro ?? '').trim().toLowerCase();
      if (!termo) return produtos;
      return produtos.filter(
        (p) => p.codigo.toLowerCase().includes(termo) || p.descricao.toLowerCase().includes(termo)
      );
    })
  );

  form = this.fb.group({
    codigo: ['', [Validators.required, Validators.maxLength(50)]],
    descricao: ['', [Validators.required, Validators.maxLength(200)]],
    saldo: [0, [Validators.required, Validators.min(0)]],
  });

  salvando = false;
  mensagemErro: string | null = null;
  mensagemSucesso: string | null = null;

  ngOnInit(): void {
    // A carga inicial já acontece via startWith() no produtos$ acima;
    // nada a fazer aqui além de deixar o hook explícito para o
    // detalhamento técnico do teste (ciclo de vida OnInit em uso).
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

    this.produtoService
      .criar({
        codigo: valor.codigo!,
        descricao: valor.descricao!,
        saldo: valor.saldo!,
      })
      .pipe(finalize(() => (this.salvando = false)))
      .subscribe({
        next: () => {
          this.mensagemSucesso = `Produto "${valor.codigo}" cadastrado com sucesso.`;
          this.form.reset({ codigo: '', descricao: '', saldo: 0 });
          this.recarregar$.next();
        },
        error: (erro) => {
          this.mensagemErro = erro.message;
        },
      });
  }
}
