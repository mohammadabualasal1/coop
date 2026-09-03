import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { OfferSummary } from '../../../../core/models/marketplace.models';
import { DiscountPercentPipe } from '../../../../shared/pipes/discount-percent.pipe';
import { JodPipe } from '../../../../shared/pipes/jod.pipe';

const ENDING_SOON_HOURS = 24;
const MS_PER_HOUR = 3_600_000;

@Component({
  selector: 'app-offer-card',
  imports: [RouterLink, JodPipe, DiscountPercentPipe],
  templateUrl: './offer-card.html',
  styleUrl: './offer-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OfferCardComponent {
  readonly offer = input.required<OfferSummary>();

  readonly hoursRemaining = computed<number | null>(() => {
    const endAt = new Date(this.offer().endAt).getTime();

    if (Number.isNaN(endAt)) {
      return null;
    }

    const hours = Math.ceil((endAt - Date.now()) / MS_PER_HOUR);
    return hours > 0 && hours <= ENDING_SOON_HOURS ? hours : null;
  });
}
