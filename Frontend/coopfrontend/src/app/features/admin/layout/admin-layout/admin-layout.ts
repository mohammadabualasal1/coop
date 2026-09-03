import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, HostListener, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { catchError, interval, of, startWith, switchMap } from 'rxjs';

import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { PageTitleService } from '../../../../core/services/page-title.service';
import { TokenStorageService } from '../../../../core/services/token-storage.service';

const UNREAD_COUNT_POLL_MS = 60000;

@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminLayoutComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly notifications = inject(NotificationService);
  private readonly pageTitleService = inject(PageTitleService);

  readonly pageTitle = this.pageTitleService.title;
  readonly unreadCount = this.notifications.unreadCount;
  readonly menuOpen = signal(false);
  readonly userName = computed(() => this.auth.user()?.fullName ?? '');

  constructor() {
    interval(UNREAD_COUNT_POLL_MS)
      .pipe(
        startWith(0),
        switchMap(() => this.notifications.getUnreadCount().pipe(catchError(() => of(null)))),
        takeUntilDestroyed()
      )
      .subscribe();
  }

  ngOnInit(): void {
    this.pageTitleService.refresh();
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
}
