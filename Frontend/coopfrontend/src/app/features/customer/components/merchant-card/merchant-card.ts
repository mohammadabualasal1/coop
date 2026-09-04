import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { MerchantSummary } from '../../../../core/models/marketplace.models';
import { FollowService } from '../../../../core/services/follow.service';
import { UiRatingStarsComponent } from '../../../../shared/components';

@Component({
  selector: 'app-merchant-card',
  imports: [RouterLink, UiRatingStarsComponent],
  templateUrl: './merchant-card.html',
  styleUrl: './merchant-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MerchantCardComponent {
  private readonly followService = inject(FollowService);

  readonly merchant = input.required<MerchantSummary>();
  readonly showUnfollowButton = input(false);

  readonly unfollowSaving = signal(false);

  unfollow(event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    if (this.unfollowSaving()) {
      return;
    }

    this.unfollowSaving.set(true);

    this.followService.unfollow(this.merchant().id).subscribe({
      next: () => this.unfollowSaving.set(false),
      error: () => this.unfollowSaving.set(false)
    });
  }
}
