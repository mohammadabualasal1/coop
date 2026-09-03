import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, HostListener, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { catchError, interval, of, startWith, switchMap } from 'rxjs';

import { UserRole } from '../../../../core/enums';
import { AuthService } from '../../../../core/services/auth.service';
import { CartService } from '../../../../core/services/cart.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { TokenStorageService } from '../../../../core/services/token-storage.service';
import { UiButtonComponent } from '../../../../shared/components';

const UNREAD_COUNT_POLL_MS = 60000;

@Component({
  selector: 'app-shop-layout',
  imports: [ReactiveFormsModule, RouterOutlet, RouterLink, UiButtonComponent],
  templateUrl: './shop-layout.html',
  styleUrl: './shop-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShopLayoutComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly notifications = inject(NotificationService);
  private readonly cart = inject(CartService);

  readonly UserRole = UserRole;
  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly role = this.auth.role;
  readonly userName = computed(() => this.auth.user()?.fullName ?? '');
  readonly isCustomer = computed(() => this.role() === UserRole.Customer);
  readonly isStaff = computed(() => this.role() === UserRole.Merchant || this.role() === UserRole.Admin);
  readonly dashboardLink = computed(() => (this.role() === UserRole.Admin ? '/admin' : '/merchant'));

  readonly cartCount = this.cart.itemCount;
  readonly unreadCount = this.notifications.unreadCount;

  readonly menuOpen = signal(false);

  readonly searchForm = this.fb.nonNullable.group({
    q: ['']
  });

  constructor() {
    interval(UNREAD_COUNT_POLL_MS)
      .pipe(
        startWith(0),
        switchMap(() =>
          this.isCustomer()
            ? this.notifications.getUnreadCount().pipe(catchError(() => of(null)))
            : of(null)
        ),
        takeUntilDestroyed()
      )
      .subscribe();
  }

  submitSearch(): void {
    const term = this.searchForm.getRawValue().q.trim();
    this.router.navigate(['/shop/search'], { queryParams: term ? { q: term } : {} });
  }

  toggleMenu(event: Event): void {
    event.stopPropagation();
    this.menuOpen.update((value) => !value);
  }

  @HostListener('document:click')
  closeMenu(): void {
    this.menuOpen.set(false);
  }

  logout(): void {
    const refreshToken = this.tokenStorage.refreshToken ?? '';

    this.auth.logout({ refreshToken }).subscribe({
      next: () => this.router.navigateByUrl('/shop'),
      error: () => this.router.navigateByUrl('/shop')
    });
  }
}
