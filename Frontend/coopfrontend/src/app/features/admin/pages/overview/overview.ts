import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { ComplaintStatus, UserRole } from '../../../../core/enums';
import { AdminComplaint, PendingOffer } from '../../../../core/models/admin.models';
import { AdminService } from '../../../../core/services/admin.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiCardComponent,
  UiSpinnerComponent,
  UiStatTileComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';

const COUNT_ONLY_PAGE_SIZE = 1;
const OLDEST_PENDING_OFFERS_COUNT = 5;

@Component({
  selector: 'app-admin-overview',
  imports: [
    RouterLink,
    CoopDatePipe,
    UiCardComponent,
    UiButtonComponent,
    UiAlertComponent,
    UiSpinnerComponent,
    UiStatTileComponent
  ],
  templateUrl: './overview.html',
  styleUrl: './overview.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OverviewComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);

  readonly pendingOffers = signal<PendingOffer[]>([]);
  readonly complaints = signal<AdminComplaint[]>([]);
  readonly totalUsers = signal(0);
  readonly totalMerchants = signal(0);
  readonly totalDrivers = signal(0);
  readonly totalCustomers = signal(0);

  readonly pendingOffersCount = computed(() => this.pendingOffers().length);

  readonly openComplaintsCount = computed(
    () =>
      this.complaints().filter(
        (complaint) => complaint.status === ComplaintStatus.Open || complaint.status === ComplaintStatus.UnderReview
      ).length
  );

  readonly oldestPendingOffers = computed(() =>
    [...this.pendingOffers()]
      .sort((a, b) => new Date(a.submittedAt).getTime() - new Date(b.submittedAt).getTime())
      .slice(0, OLDEST_PENDING_OFFERS_COUNT)
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    forkJoin({
      pendingOffers: this.adminService.getPendingOffers(),
      complaints: this.adminService.getComplaints(),
      users: this.adminService.getUsers({ pageNumber: 1, pageSize: COUNT_ONLY_PAGE_SIZE }),
      merchants: this.adminService.getUsers({
        role: UserRole.Merchant,
        pageNumber: 1,
        pageSize: COUNT_ONLY_PAGE_SIZE
      }),
      drivers: this.adminService.getUsers({
        role: UserRole.Driver,
        pageNumber: 1,
        pageSize: COUNT_ONLY_PAGE_SIZE
      }),
      customers: this.adminService.getUsers({
        role: UserRole.Customer,
        pageNumber: 1,
        pageSize: COUNT_ONLY_PAGE_SIZE
      })
    }).subscribe({
      next: (result) => {
        this.pendingOffers.set(result.pendingOffers);
        this.complaints.set(result.complaints);
        this.totalUsers.set(result.users.totalCount);
        this.totalMerchants.set(result.merchants.totalCount);
        this.totalDrivers.set(result.drivers.totalCount);
        this.totalCustomers.set(result.customers.totalCount);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }
}
