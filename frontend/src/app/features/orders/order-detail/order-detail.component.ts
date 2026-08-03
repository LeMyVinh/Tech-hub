import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';
import { OrderDetailResponse, OrderService } from '../order.service';

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Chờ xử lý',
  Confirmed: 'Đã xác nhận',
  Processing: 'Đang xử lý',
  Shipping: 'Đang giao',
  Delivered: 'Đã giao',
  Cancelled: 'Đã hủy',
};

@Component({
  selector: 'app-order-detail',
  imports: [CommonModule, RouterLink],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.scss',
})
export class OrderDetailComponent implements OnInit {
  readonly order = signal<OrderDetailResponse | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  readonly cancelling = signal(false);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly auth: AuthService,
    private readonly orderService: OrderService,
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.router.navigate(['/orders']);
      return;
    }
    this.loadOrder(id);
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  private loadOrder(id: number): void {
    const token = this.getToken();
    if (!token) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.loading.set(true);
    this.orderService.getOrderDetail(token, id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Không thể tải chi tiết đơn hàng.');
        this.loading.set(false);
      },
    });
  }

  cancelOrder(): void {
    const order = this.order();
    const token = this.getToken();
    if (!order || !token) return;
    if (!confirm('Bạn có chắc chắn muốn hủy đơn hàng này?')) return;
    const reason = prompt('Lý do hủy đơn (tùy chọn):');

    this.cancelling.set(true);
    this.orderService.cancelOrder(token, order.id, reason || undefined).subscribe({
      next: () => {
        this.message.set('Đã hủy đơn hàng thành công.');
        this.cancelling.set(false);
        this.loadOrder(order.id);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Lỗi hủy đơn hàng.');
        this.cancelling.set(false);
      },
    });
  }

  getStatusLabel(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }

  getStatusClass(status: string): string {
    return `status-${status.toLowerCase()}`;
  }

  formatVnd(amount: number): string {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  }
}