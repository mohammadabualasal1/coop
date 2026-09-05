import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { UserRole } from '../../../core/enums';
import { AppNotification } from '../../../core/models/notification.models';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { extractErrorMessage } from '../../../core/utils/http-error';
import { CoopDatePipe } from '../../pipes/coop-date.pipe';
import { UiAlertComponent } from '../ui-alert/ui-alert';
import { UiButtonComponent } from '../ui-button/ui-button';
import { UiCardComponent } from '../ui-card/ui-card';
import { UiConfirmModalComponent } from '../ui-confirm-modal/ui-confirm-modal';
import { UiEmptyStateComponent } from '../ui-empty-state/ui-empty-state';
import { UiSpinnerComponent } from '../ui-spinner/ui-spinner';

type PageStatus = 'loading' | 'error' | 'loaded';
type NotificationFilter = 'all' | 'unread';

const ROUTES_BY_TYPE: Record<string, { merchant?: string; admin?: string }> = {
  Order: { merchant: '/merchant/orders' },
  Offer: { merchant: '/merchant/offers', admin: '/admin/offers' },
  Complaint: { merchant: '/merchant/complaints', admin: '/admin/complaints' },
  Merchant: { merchant: '/merchant/profile', admin: '/admin/merchants' }
};

@Component({
  selector: 'coop-notifications-page',
  imports: [
    CoopDatePipe,
    UiAlertComponent,
    UiButtonComponent,
    UiCardComponent,
    UiConfirmModalComponent,
    UiEmptyStateComponent,
    UiSpinnerComponent
  ],
  templateUrl: './notifications-page.html',
  styleUrl: './notifications-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NotificationsPageComponent implements OnInit {
  private readonly notificationService = inject(NotificationService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly notifications = signal<AppNotification[]>([]);
  readonly filter = signal<NotificationFilter>('all');

  readonly unreadCount = this.notificationService.unreadCount;

  readonly visibleNotifications = computed(() =>
    this.filter() === 'all' ? this.notifications() : this.notifications().filter((n) => !n.isRead)
  );

  readonly confirmDeleteOpen = signal(false);
  readonly notificationToDelete = signal<AppNotification | null>(null);
  readonly deleteSaving = signal(false);
  readonly deleteErrorMessage = signal<string | null>(null);

  readonly actionErrorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
    this.notificationService.getUnreadCount().subscribe({
      error: (err: unknown) => this.actionErrorMessage.set(extractErrorMessage(err))
    });
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.notificationService.getAll().subscribe({
      next: (notifications) => {
        this.notifications.set(notifications);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  setFilter(value: NotificationFilter): void {
    this.filter.set(value);
  }

  markAllRead(): void {
    if (this.unreadCount() === 0) {
      return;
    }

    this.actionErrorMessage.set(null);

    this.notificationService.markAllRead().subscribe({
      next: () => this.notifications.update((list) => list.map((n) => ({ ...n, isRead: true }))),
      error: (err: unknown) => this.actionErrorMessage.set(extractErrorMessage(err))
    });
  }

  private routeFor(notification: AppNotification): string | null {
    if (!notification.relatedEntityType || !notification.relatedEntityId) {
      return null;
    }

    const routes = ROUTES_BY_TYPE[notification.relatedEntityType];

    if (!routes) {
      return null;
    }

    return this.auth.role() === UserRole.Admin ? (routes.admin ?? null) : (routes.merchant ?? null);
  }

  isClickable(notification: AppNotification): boolean {
    return this.routeFor(notification) !== null;
  }

  onRowClick(notification: AppNotification): void {
    if (!notification.isRead) {
      this.notificationService.markRead(notification.id).subscribe({
        next: () =>
          this.notifications.update((list) =>
            list.map((n) => (n.id === notification.id ? { ...n, isRead: true } : n))
          ),
        error: (err: unknown) => this.actionErrorMessage.set(extractErrorMessage(err))
      });
    }

    const link = this.routeFor(notification);

    if (link) {
      this.router.navigateByUrl(link);
    }
  }

  openDeleteConfirm(event: Event, notification: AppNotification): void {
    event.stopPropagation();
    this.notificationToDelete.set(notification);
    this.deleteErrorMessage.set(null);
    this.confirmDeleteOpen.set(true);
  }

  closeDeleteConfirm(): void {
    if (this.deleteSaving()) {
      return;
    }

    this.confirmDeleteOpen.set(false);
    this.notificationToDelete.set(null);
  }

  confirmDelete(): void {
    const notification = this.notificationToDelete();

    if (!notification || this.deleteSaving()) {
      return;
    }

    this.deleteSaving.set(true);
    this.deleteErrorMessage.set(null);

    this.notificationService.remove(notification.id, !notification.isRead).subscribe({
      next: () => {
        this.deleteSaving.set(false);
        this.confirmDeleteOpen.set(false);
        this.notificationToDelete.set(null);
        this.notifications.update((list) => list.filter((n) => n.id !== notification.id));
      },
      error: (err: unknown) => {
        this.deleteSaving.set(false);
        this.deleteErrorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
