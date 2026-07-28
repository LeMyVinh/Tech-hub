import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface CartItem {
  id: number;
  variantId: number;
  productName: string;
  variantName: string;
  sku: string;
  price: number;
  quantity: number;
  stockQuantity: number;
  imageUrl: string | null;
  subtotal: number;
}

export interface CartResponse {
  id: number;
  items: CartItem[];
  totalAmount: number;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly apiUrl = 'http://localhost:5159/api/v1/cart';

  constructor(private readonly http: HttpClient) {}

  getCart(token: string): Observable<CartResponse> {
    return this.http.get<CartResponse>(this.apiUrl, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }

  addToCart(token: string, variantId: number, quantity: number): Observable<CartResponse> {
    return this.http.post<CartResponse>(
      `${this.apiUrl}/items`,
      { variantId, quantity },
      { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) }
    );
  }

  updateCartItem(token: string, itemId: number, quantity: number): Observable<CartResponse> {
    return this.http.put<CartResponse>(
      `${this.apiUrl}/items/${itemId}`,
      { quantity },
      { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) }
    );
  }

  removeFromCart(token: string, itemId: number): Observable<CartResponse> {
    return this.http.delete<CartResponse>(`${this.apiUrl}/items/${itemId}`, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }

  clearCart(token: string): Observable<CartResponse> {
    return this.http.delete<CartResponse>(this.apiUrl, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }
}
