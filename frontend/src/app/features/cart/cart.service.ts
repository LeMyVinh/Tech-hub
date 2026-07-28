import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

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

  // Số lượng sản phẩm trong giỏ, dùng để hiển thị badge trên header toàn app
  readonly cartCount = signal(0);

  constructor(private readonly http: HttpClient) {}

  getCart(token: string): Observable<CartResponse> {
    return this.http
      .get<CartResponse>(this.apiUrl, {
        headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
      })
      .pipe(tap(cart => this.updateCount(cart)));
  }

  addToCart(token: string, variantId: number, quantity: number): Observable<CartResponse> {
    return this.http
      .post<CartResponse>(
        `${this.apiUrl}/items`,
        { variantId, quantity },
        { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) }
      )
      .pipe(tap(cart => this.updateCount(cart)));
  }

  updateCartItem(token: string, itemId: number, quantity: number): Observable<CartResponse> {
    return this.http
      .put<CartResponse>(
        `${this.apiUrl}/items/${itemId}`,
        { quantity },
        { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) }
      )
      .pipe(tap(cart => this.updateCount(cart)));
  }

  removeFromCart(token: string, itemId: number): Observable<CartResponse> {
    return this.http
      .delete<CartResponse>(`${this.apiUrl}/items/${itemId}`, {
        headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
      })
      .pipe(tap(cart => this.updateCount(cart)));
  }

  clearCart(token: string): Observable<CartResponse> {
    return this.http
      .delete<CartResponse>(this.apiUrl, {
        headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
      })
      .pipe(tap(cart => this.updateCount(cart)));
  }

  private updateCount(cart: CartResponse): void {
    this.cartCount.set(cart.items.reduce((sum, item) => sum + item.quantity, 0));
  }
}