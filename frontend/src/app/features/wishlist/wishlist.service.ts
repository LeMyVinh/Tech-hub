import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface WishlistItem {
  id: number;
  productId: number;
  productName: string;
  primaryImageUrl: string | null;
  minPrice: number;
  maxPrice: number;
  createdAt: string;
  isDeleted: boolean;
  deletedAt: string | null;
}

export interface WishlistResponse {
  id: number;
  items: WishlistItem[];
}

@Injectable({ providedIn: 'root' })
export class WishlistService {
  private readonly apiUrl = 'http://localhost:5159/api/v1/wishlist';

  constructor(private readonly http: HttpClient) {}

  getWishlist(token: string, includeDeleted = false): Observable<WishlistResponse> {
    return this.http.get<WishlistResponse>(`${this.apiUrl}?includeDeleted=${includeDeleted}`, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }

  addToWishlist(token: string, productId: number): Observable<WishlistResponse> {
    return this.http.post<WishlistResponse>(
      this.apiUrl,
      { productId },
      { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) }
    );
  }

  removeFromWishlist(token: string, productId: number): Observable<WishlistResponse> {
    return this.http.delete<WishlistResponse>(`${this.apiUrl}/${productId}`, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }

  restoreWishlistItem(token: string, productId: number): Observable<WishlistResponse> {
    return this.http.put<WishlistResponse>(`${this.apiUrl}/${productId}/restore`, {}, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }

  moveToCart(token: string, productId: number): Observable<unknown> {
    return this.http.post(
      `${this.apiUrl}/${productId}/move-to-cart`,
      {},
      { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) }
    );
  }
}
