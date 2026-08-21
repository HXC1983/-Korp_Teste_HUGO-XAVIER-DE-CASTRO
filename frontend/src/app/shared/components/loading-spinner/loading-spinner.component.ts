import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  template: `<span class="spinner" [style.width.px]="size" [style.height.px]="size"></span>`,
  styles: [
    `
      .spinner {
        display: inline-block;
        border: 3px solid rgba(0, 0, 0, 0.1);
        border-top-color: var(--cor-primaria, #1e3a5f);
        border-radius: 50%;
        animation: girar 0.7s linear infinite;
      }
      @keyframes girar {
        to {
          transform: rotate(360deg);
        }
      }
    `,
  ],
})
export class LoadingSpinnerComponent {
  @Input() size = 16;
}
