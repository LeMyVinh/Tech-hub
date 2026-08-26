import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface CreateReviewRequest {
  orderItemId: number;
  productId: number;
  rating: number;
  comment?: string;
  imageUrls?: string[];
}

export interface ReviewResponse {
  id: number;
  productId: number;
  userName: string;
  rating: number;
  comment?: string;
  imageUrls: string[];
  status: string;
  rejectReason?: string;
  createdAt: string;
  isDeleted: boolean;
  deletedAt?: string | null;
}

@Injectable({ providedIn: 'root' })
export class CustomerReviewService {
  private readonly baseUrl = 'http://localhost:5159/api/v1/products';

  constructor(private readonly http: HttpClient) {}

  createReview(token: string, productId: number, request: CreateReviewRequest): Observable<ReviewResponse> {
    return this.http.post<ReviewResponse>(`${this.baseUrl}/${productId}/reviews`, request, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }
}
