import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';
import { ReviewItem, ReviewListResponse, ReviewService } from './review.service';

@Component({
  selector: 'app-review-manage',
  imports: [CommonModule, RouterLink],
  templateUrl: './review-manage.component.html',
  styleUrl: './review-manage.component.scss',
})
export class ReviewManageComponent implements OnInit {
  readonly reviews = signal<ReviewListResponse | null>(null);
  readonly loading = signal(false);
  readonly message = signal('');
  readonly error = signal('');
  readonly processingId = signal<number | null>(null);

  currentPage = 1;
  readonly pageSize = 10;
  readonly Math = Math;

  constructor(
    private readonly auth: AuthService,
    private readonly reviewService: ReviewService,
  ) {}

  ngOnInit(): void {
    this.loadReviews();
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  loadReviews(): void {
    const token = this.getToken();
    if (!token) return;
    this.loading.set(true);
    this.reviewService.getPendingReviews(token, this.currentPage, this.pageSize).subscribe({
      next: res => { this.reviews.set(res); this.loading.set(false); },
      error: () => { this.error.set('Không thể tải danh sách đánh giá chờ duyệt.'); this.loading.set(false); },
    });
  }

  approve(review: ReviewItem): void {
    const token = this.getToken();
    if (!token) return;
    if (!confirm(`Duyệt đánh giá của "${review.userName}" cho sản phẩm #${review.productId}?`)) return;
    this.processingId.set(review.id);
    this.reviewService.approveReview(token, review.id).subscribe({
      next: () => {
        this.message.set('Đã duyệt đánh giá thành công.');
        this.processingId.set(null);
        this.loadReviews();
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Lỗi duyệt đánh giá.');
        this.processingId.set(null);
      },
    });
  }

  reject(review: ReviewItem): void {
    const token = this.getToken();
    if (!token) return;
    const reason = prompt('Lý do từ chối đánh giá này:');
    if (reason === null) return;
    this.processingId.set(review.id);
    this.reviewService.rejectReview(token, review.id, reason || undefined).subscribe({
      next: () => {
        this.message.set('Đã từ chối đánh giá.');
        this.processingId.set(null);
        this.loadReviews();
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Lỗi từ chối đánh giá.');
        this.processingId.set(null);
      },
    });
  }

  getStarArray(rating: number): number[] {
    return Array.from({ length: 5 }, (_, i) => (i < rating ? 1 : 0));
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadReviews();
    }
  }

  nextPage(): void {
    if (this.reviews() && this.currentPage * this.pageSize < this.reviews()!.totalCount) {
      this.currentPage++;
      this.loadReviews();
    }
  }

  clearFeedback(): void {
    this.message.set('');
    this.error.set('');
  }
}