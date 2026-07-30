import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';
import {
  OrderDetailResponse,
  OrderListResponse,
  OrderResponse,
  OrderService,
} from '../../orders/order.service';

// Nhãn hiển thị theo BR-03
const STATUS_LABELS: Record<string, string> = {
  Pending: 'Chờ xử lý',
  Confirmed: 'Đã xác nhận',
  Processing: 'Đang xử lý',
  Shipping: 'Đang giao',
  Delivered: 'Đã giao',
  Cancelled: 'Đã hủy',
};

// BR-03 / DD TH_A201 mục 8: bảng ánh xạ chuyển trạng thái hợp lệ, không được chuyển lùi
const NEXT_VALID_STATUSES: Record<string, string[]> = {
  Pending: ['Confirmed', 'Cancelled'],
  Confirmed: ['Processing', 'Cancelled'],
  Processing: ['Shipping'],
  Shipping: ['Delivered'],
  Delivered: [],
  Cancelled: [],
};

@Component({
  selector: 'app-order-manage',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './order-manage.component.html',
  styleUrl: './order-manage.component.scss',
})
export class OrderManageComponent implements OnInit {
  readonly Math = Math;
  readonly statusOptions = Object.keys(STATUS_LABELS);

  readonly orders = signal<OrderListResponse | null>(null);
  readonly loading = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  readonly showModal = signal(false);
  readonly selectedOrder = signal<OrderDetailResponse | null>(null);
  readonly detailLoading = signal(false);
  readonly updating = signal(false);

  selectedNewStatus = '';
  statusNote = '';

  statusFilter = '';
  currentPage = 1;
  readonly pageSize = 10;

  constructor(
    private readonly auth: AuthService,
    private readonly orderService: OrderService,
  ) {}

  ngOnInit(): void {
    this.loadOrders();
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  loadOrders(): void {
    const token = this.getToken();
    if (!token) return;
    this.loading.set(true);
    this.orderService
      .getAllOrders(token, this.currentPage, this.pageSize, this.statusFilter || undefined)
      .subscribe({
        next: res => {
          this.orders.set(res);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Không thể tải danh sách đơn hàng.');
          this.loading.set(false);
        },
      });
  }

  applyFilter(): void {
    this.currentPage = 1;
    this.loadOrders();
  }

  resetFilter(): void {
    this.statusFilter = '';
    this.currentPage = 1;
    this.loadOrders();
  }

  goToPage(page: number): void {
    if (page < 1) return;
    const maxPage = Math.ceil((this.orders()?.totalCount ?? 0) / this.pageSize) || 1;
    if (page > maxPage) return;
    this.currentPage = page;
    this.loadOrders();
  }

  openDetail(order: OrderResponse): void {
    const token = this.getToken();
    if (!token) return;

    this.selectedNewStatus = '';
    this.statusNote = '';
    this.selectedOrder.set(null);
    this.detailLoading.set(true);
    this.showModal.set(true);

    this.orderService.getOrderDetail(token, order.id).subscribe({
      next: detail => {
        this.selectedOrder.set(detail);
        this.detailLoading.set(false);
      },
      error: () => {
        this.error.set('Không thể tải chi tiết đơn hàng.');
        this.detailLoading.set(false);
        this.showModal.set(false);
      },
    });
  }

  closeModal(): void {
    this.showModal.set(false);
    this.selectedOrder.set(null);
  }

  // DD TH_A201 mục 13 (UI Layout): dropdown chỉ liệt kê trạng thái hợp lệ tiếp theo
  getNextStatuses(current: string): string[] {
    return NEXT_VALID_STATUSES[current] ?? [];
  }

  updateStatus(): void {
    const order = this.selectedOrder();
    const token = this.getToken();
    if (!order || !token || !this.selectedNewStatus) return;

    this.updating.set(true);
    this.orderService.updateOrderStatus(token, order.id, this.selectedNewStatus, this.statusNote || undefined).subscribe({
      next: () => {
        this.message.set('Cập nhật trạng thái đơn hàng thành công.');
        this.updating.set(false);
        this.closeModal();
        this.loadOrders();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Lỗi cập nhật trạng thái đơn hàng.');
        this.updating.set(false);
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

  clearFeedback(): void {
    this.message.set('');
    this.error.set('');
  }
}