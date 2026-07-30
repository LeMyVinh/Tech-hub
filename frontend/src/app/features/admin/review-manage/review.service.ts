import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface ReviewItem {
  id: number;
  productId: number;
  userName: string;
  rating: number;
  comment?: string;
  imageUrls: string[];
  status: string;
  rejectReason?: string;
  createdAt: string;
}

export interface ReviewListResponse {
  reviews: ReviewItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  averageRating: number;
}

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly apiUrl = 'http://localhost:5159/api/v1';

  constructor(private readonly http: HttpClient) {}

  getPendingReviews(token: string, page = 1, pageSize = 10): Observable<ReviewListResponse> {
    return this.http.get<ReviewListResponse>(
      `${this.apiUrl}/admin/reviews/pending?page=${page}&pageSize=${pageSize}`,
      { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) }
    );
  }

  approveReview(token: string, id: number): Observable<ReviewItem> {
    return this.http.put<ReviewItem>(
      `${this.apiUrl}/admin/reviews/${id}/approve`,
      {},
      { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) }
    );
  }

  rejectReview(token: string, id: number, reason?: string): Observable<ReviewItem> {
    return this.http.put<ReviewItem>(
      `${this.apiUrl}/admin/reviews/${id}/reject`,
      { reason },
      { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) }
    );
  }
}