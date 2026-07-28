import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export interface ToastMessage {
  type: 'success' | 'error' | 'info';
  text: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly toast$ = new Subject<ToastMessage>();

  readonly toasts$ = this.toast$.asObservable();

  success(text: string): void {
    this.toast$.next({ type: 'success', text });
  }

  error(text: string): void {
    this.toast$.next({ type: 'error', text });
  }

  info(text: string): void {
    this.toast$.next({ type: 'info', text });
  }
}
