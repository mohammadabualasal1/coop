import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';

import { OfferSummary, PagedResponse } from '../../../../core/models/marketplace.models';
import { MarketplaceService } from '../../../../core/services/marketplace.service';
import { OfferCardComponent } from '../../components/offer-card/offer-card';

const LATEST_PAGE_SIZE = 12;

function emptyPage(): PagedResponse<OfferSummary> {
  return { items: [], totalCount: 0, pageNumber: 1, pageSize: LATEST_PAGE_SIZE };
}

@Component({
  selector: 'app-customer-home',
  imports: [RouterLink, OfferCardComponent],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent implements OnInit {
  private readonly marketplace = inject(MarketplaceService);

  readonly endingSoon = signal<OfferSummary[]>([]);
  readonly topDiscounts = signal<OfferSummary[]>([]);
  readonly latest = signal<OfferSummary[]>([]);

  ngOnInit(): void {
    forkJoin({
      endingSoon: this.marketplace.endingSoon().pipe(catchError(() => of<OfferSummary[]>([]))),
      topDiscounts: this.marketplace.topDiscounts().pipe(catchError(() => of<OfferSummary[]>([]))),
      latest: this.marketplace
        .searchOffers({ pageNumber: 1, pageSize: LATEST_PAGE_SIZE })
        .pipe(catchError(() => of(emptyPage())))
    }).subscribe((result) => {
      this.endingSoon.set(result.endingSoon);
      this.topDiscounts.set(result.topDiscounts);
      this.latest.set(result.latest.items);
    });
  }
}
