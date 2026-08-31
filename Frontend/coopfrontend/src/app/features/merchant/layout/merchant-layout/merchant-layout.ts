import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';
import { catchError, filter, interval, of, startWith, switchMap } from 'rxjs';

import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { TokenStorageService } from '../../../../core/services/token-storage.service';

const UNREAD_COUNT_POLL_MS = 60000;

@Component({
  selector: 'app-merchant-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './merchant-layout.html',
  styleUrl: './merchant-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MerchantLayoutComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly notifications = inject(NotificationService);

  readonly pageTitle = signal('');
  readonly unreadCount = signal(0);
  readonly menuOpen = signal(false);
  readonly userName = computed(() => this.auth.user()?.fullName ?? '');

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe(() => this.updatePageTitle());

    interval(UNREAD_COUNT_POLL_MS)
      .pipe(
        startWith(0),
        switchMap(() =>
          this.notifications.getUnreadCount().pipe(catchError(() => of({ unreadCount: 0 })))
        ),
        takeUntilDestroyed()
      )
      .subscribe((res) => this.unreadCount.set(res.unreadCount));
  }

  ngOnInit(): void {
    this.updatePageTitle();
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
      next: () => this.router.navigateByUrl('/login'),
      error: () => this.router.navigateByUrl('/login')
    });
  }

  private updatePageTitle(): void {
    let route = this.activatedRoute;

    while (route.firstChild) {
      route = route.firstChild;
    }

    this.pageTitle.set((route.snapshot?.data?.['title'] as string) ?? '');
  }
}
