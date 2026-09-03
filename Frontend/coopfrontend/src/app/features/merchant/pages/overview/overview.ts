import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { OfferStatus, OrderStatus, OrderStatusLabels, OrderStatusTones } from '../../../../core/enums';
import { MerchantBranch } from '../../../../core/models/branch.models';
import { Merchant } from '../../../../core/models/merchant.models';
import { Offer } from '../../../../core/models/offer.models';
import { MerchantOrder } from '../../../../core/models/order.models';
import { Product } from '../../../../core/models/product.models';
import { BranchService } from '../../../../core/services/branch.service';
import { MerchantOrderService } from '../../../../core/services/merchant-order.service';
import { MerchantService } from '../../../../core/services/merchant.service';
import { OfferService } from '../../../../core/services/offer.service';
import { ProductService } from '../../../../core/services/product.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiBadgeComponent,
  UiButtonComponent,
  UiCardComponent,
  UiSpinnerComponent,
  UiStatTileComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';
import { JodPipe } from '../../../../shared/pipes/jod.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';

interface AttentionItem {
  text: string;
  link: string;
}

const LATEST_ORDERS_COUNT = 5;

@Component({
  selector: 'app-merchant-overview',
  imports: [
    RouterLink,
    JodPipe,
    CoopDatePipe,
    UiCardComponent,
    UiButtonComponent,
    UiBadgeComponent,
    UiAlertComponent,
    UiSpinnerComponent,
    UiStatTileComponent
  ],
  templateUrl: './overview.html',
  styleUrl: './overview.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OverviewComponent implements OnInit {
  private readonly merchantService = inject(MerchantService);
  private readonly branchService = inject(BranchService);
  private readonly productService = inject(ProductService);
  private readonly offerService = inject(OfferService);
  private readonly orderService = inject(MerchantOrderService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);

  readonly merchant = signal<Merchant | null>(null);
  readonly branches = signal<MerchantBranch[]>([]);
  readonly products = signal<Product[]>([]);
  readonly offers = signal<Offer[]>([]);
  readonly orders = signal<MerchantOrder[]>([]);

  readonly newOrdersCount = computed(
    () => this.orders().filter((order) => order.status === OrderStatus.PendingMerchantConfirmation).length
  );

  readonly preparingCount = computed(
    () =>
      this.orders().filter(
        (order) => order.status === OrderStatus.Accepted || order.status === OrderStatus.Preparing
      ).length
  );

  readonly activeOffersCount = computed(
    () => this.offers().filter((offer) => offer.status === OfferStatus.Active).length
  );

  readonly productsCount = computed(() => this.products().length);

  readonly attentionItems = computed<AttentionItem[]>(() => {
    const items: AttentionItem[] = [];

    const newOrders = this.newOrdersCount();
    if (newOrders > 0) {
      items.push({ text: `لديك ${newOrders} طلب بانتظار القبول`, link: '/merchant/orders' });
    }

    const rejectedOffers = this.offers().filter((offer) => offer.status === OfferStatus.Rejected).length;
    if (rejectedOffers > 0) {
      items.push({ text: `${rejectedOffers} عرض مرفوض بحاجة لتعديل`, link: '/merchant/offers' });
    }

    const pendingApprovalOffers = this.offers().filter(
      (offer) => offer.status === OfferStatus.PendingApproval
    ).length;
    if (pendingApprovalOffers > 0) {
      items.push({ text: `${pendingApprovalOffers} عرض بانتظار موافقة الإدارة`, link: '/merchant/offers' });
    }

    if (this.branches().length === 0) {
      items.push({ text: 'لم تضف أي فرع بعد', link: '/merchant/branches' });
    }

    if (this.products().length === 0) {
      items.push({ text: 'لم تضف أي منتج بعد', link: '/merchant/products' });
    }

    return items;
  });

  readonly latestOrders = computed(() =>
    [...this.orders()]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, LATEST_ORDERS_COUNT)
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    forkJoin({
      merchant: this.merchantService.getMy(),
      branches: this.branchService.getAll(),
      products: this.productService.getMy(),
      offers: this.offerService.getMy(),
      orders: this.orderService.getAll()
    }).subscribe({
      next: (result) => {
        this.merchant.set(result.merchant);
        this.branches.set(result.branches);
        this.products.set(result.products);
        this.offers.set(result.offers);
        this.orders.set(result.orders);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  statusLabel(status: OrderStatus): string {
    return OrderStatusLabels[status];
  }

  statusTone(status: OrderStatus) {
    return OrderStatusTones[status];
  }
}
