import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';
import { OrderDetailResponse, OrderItem, OrderService } from '../order.service';
import { CustomerReviewService } from '../../reviews/review.service';

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
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.scss',
})
export class OrderDetailComponent implements OnInit {
  readonly order = signal<OrderDetailResponse | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  readonly cancelling = signal(false);

  // --- Đánh giá sản phẩm (UC-11 / TH_P601) ---
  readonly showReviewModal = signal(false);
  readonly reviewingItem = signal<OrderItem | null>(null);
  readonly submittingReview = signal(false);
  // FIX: trước đây đây là NGUỒN SỰ THẬT DUY NHẤT cho trạng thái "đã đánh giá" -> mất
  // trạng thái sau khi F5 (set rỗng lại từ đầu), khiến nút "Đánh giá" hiện sai và bị
  // backend từ chối. Giờ nguồn sự thật là item.hasReviewed (backend trả về dựa trên
  // bảng Review). Set này chỉ còn dùng để cập nhật UI NGAY LẬP TỨC sau khi gửi thành
  // công, tránh phải gọi lại API loadOrder().
  readonly reviewedItemIds = signal<Set<number>>(new Set());
  reviewRating = 5;
  reviewComment = '';
  readonly starOptions = [1, 2, 3, 4, 5];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly auth: AuthService,
    private readonly orderService: OrderService,
    private readonly reviewService: CustomerReviewService,
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
        // Mỗi lần tải lại đơn hàng từ backend, trạng thái đã đánh giá là chính xác
        // nên có thể xoá sạch set optimistic-update cũ.
        this.reviewedItemIds.set(new Set());
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

  // --- Đánh giá sản phẩm ---
  canReview(item: OrderItem): boolean {
    // FIX: kiểm tra cả trạng thái thật từ backend (item.hasReviewed) lẫn set
    // optimistic-update cục bộ, thay vì chỉ dựa vào set cục bộ như trước.
    return this.order()?.status === 'Delivered' && !item.hasReviewed && !this.reviewedItemIds().has(item.id);
  }

  openReviewModal(item: OrderItem): void {
    this.reviewingItem.set(item);
    this.reviewRating = 5;
    this.reviewComment = '';
    this.showReviewModal.set(true);
  }

  closeReviewModal(): void {
    this.showReviewModal.set(false);
    this.reviewingItem.set(null);
  }

  setRating(star: number): void {
    this.reviewRating = star;
  }

  submitReview(): void {
    const item = this.reviewingItem();
    const token = this.getToken();
    if (!item || !token) return;

    this.submittingReview.set(true);
    this.reviewService
      .createReview(token, item.productId, {
        orderItemId: item.id,
        productId: item.productId,
        rating: this.reviewRating,
        comment: this.reviewComment.trim() || undefined,
      })
      .subscribe({
        next: () => {
          this.submittingReview.set(false);
          this.showReviewModal.set(false);
          this.reviewedItemIds.update(set => new Set(set).add(item.id));
          this.message.set('Cảm ơn bạn đã đánh giá! Đánh giá của bạn đang chờ Admin duyệt.');
          this.reviewingItem.set(null);
        },
        error: (err) => {
          this.submittingReview.set(false);
          this.error.set(err.error?.message ?? 'Gửi đánh giá thất bại, vui lòng thử lại.');
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