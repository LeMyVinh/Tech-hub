import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LoginResponse } from '../../auth.service';
import { CartService } from '../../features/cart/cart.service';

@Component({
  selector: 'app-header',
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent implements OnChanges {
  @Input() session: LoginResponse | null = null;
  @Output() logoutClick = new EventEmitter<void>();

  readonly cartCount = this.cartService.cartCount;

  constructor(private readonly cartService: CartService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['session'] && this.session?.token && this.session.user.role === 'Customer') {
      this.cartService.getCart(this.session.token).subscribe({ error: () => {} });
    }
  }
}