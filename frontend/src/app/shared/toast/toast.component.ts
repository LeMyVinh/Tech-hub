import { Component, inject, signal } from '@angular/core';
import { ToastService, ToastMessage } from './toast.service';

interface Toast extends ToastMessage {
  id: number;
}

@Component({
  selector: 'app-toast',
  standalone: true,
  template: `
    <div class="toast-container">
      @for (toast of toasts(); track toast.id) {
        <div class="toast toast--{{ toast.type }}">
          <span class="toast-icon material-icons">
            @switch (toast.type) {
              @case ('success') { check_circle }
              @case ('error') { error }
              @case ('info') { info }
            }
          </span>
          <span class="toast-message">{{ toast.text }}</span>
          <button class="toast-close" (click)="dismiss(toast.id)">
            <span class="material-icons">close</span>
          </button>
        </div>
      }
    </div>
  `
})
export class ToastComponent {
  private toastService = inject(ToastService);
  toasts = signal<Toast[]>([]);
  private nextId = 0;

  constructor() {
    this.toastService.toasts$.subscribe((msg) => {
      const id = this.nextId++;
      this.toasts.update(t => [...t, { id, ...msg }]);
      setTimeout(() => this.dismiss(id), 4000);
    });
  }

  dismiss(id: number) {
    this.toasts.update(t => t.filter(item => item.id !== id));
  }
}
