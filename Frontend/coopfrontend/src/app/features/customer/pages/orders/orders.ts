import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { OrderStatus, OrderStatusLabels, OrderStatusTones } from '../../../../core/enums';
import { CustomerOrderSummary } from '../../../../core/models/customer-order.models';
import { OrderService } from '../../../../core/services/order.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiBadgeComponent,
  UiButtonComponent,
  UiCardComponent,
  UiEmptyStateComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';
import { JodPipe } from '../../../../shared/pipes/jod.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';

@Component({
  selector: 'app-customer-orders',
  imports: [
    RouterLink,
    JodPipe,
    CoopDatePipe,
    UiCardComponent,
    UiButtonComponent,
    UiBadgeComponent,
    UiAlertComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent
  ],
  templateUrl: './orders.html',
  styleUrl: './orders.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrdersComponent implements OnInit {
  private readonly orderService = inject(OrderService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly orders = signal<CustomerOrderSummary[]>([]);
  readonly selectedStatus = signal<OrderStatus | 'all'>('all');

  readonly presentStatuses = computed(() => {
    const statuses = new Set(this.orders().map((order) => order.status));
    return Array.from(statuses).sort((a, b) => a - b);
  });

  readonly filteredOrders = computed(() => {
    const selected = this.selectedStatus();
    const sorted = [...this.orders()].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );

    return selected === 'all' ? sorted : sorted.filter((order) => order.status === selected);
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.orderService.getAll().subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  selectStatus(value: OrderStatus | 'all'): void {
    this.selectedStatus.set(value);
  }

  statusLabel(status: OrderStatus): string {
    return OrderStatusLabels[status];
  }

  statusTone(status: OrderStatus) {
    return OrderStatusTones[status];
  }
}
