import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';
import { OrderListResponse, OrderResponse, OrderService } from '../order.service';

@Component({
  selector: 'app-order-list',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.scss',
})
export class OrderListComponent implements OnInit {
  readonly orders = signal<OrderListResponse | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly message = signal('');

  currentPage = 1;
  readonly pageSize = 10;
  readonly Math = Math;

  constructor(
    private readonly auth: AuthService,
    private readonly orderService: OrderService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.loadOrders();
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  loadOrders(): void {
    const token = this.getToken();
    if (!token) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.loading.set(true);
    this.orderService.getUserOrders(token, this.currentPage, this.pageSize).subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Không thể tải danh sách đơn hàng.');
        this.loading.set(false);
      },
    });
  }

  viewOrder(orderId: number): void {
    this.router.navigate(['/orders', orderId]);
  }

  cancelOrder(order: OrderResponse): void {
    const token = this.getToken();
    if (!token) return;
    if (!confirm('Bạn có chắc chắn muốn hủy đơn hàng này?')) return;
    const reason = prompt('Lý do hủy đơn (tùy chọn):');
    this.loading.set(true);
    this.orderService.cancelOrder(token, order.id, reason || undefined).subscribe({
      next: () => {
        this.message.set('Đã hủy đơn hàng thành công.');
        this.loadOrders();
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Lỗi hủy đơn hàng.');
        this.loading.set(false);
      },
    });
  }

  getStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      Pending: 'Chờ xử lý',
      Confirmed: 'Đã xác nhận',
      Processing: 'Đang xử lý',
      Shipping: 'Đang giao',
      Delivered: 'Đã giao',
      Cancelled: 'Đã hủy',
    };
    return labels[status] || status;
  }

  getStatusClass(status: string): string {
    return `status-${status.toLowerCase()}`;
  }

  formatVnd(amount: number): string {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadOrders();
    }
  }

  nextPage(): void {
    if (this.orders() && this.currentPage * this.pageSize < this.orders()!.totalCount) {
      this.currentPage++;
      this.loadOrders();
    }
  }
}
