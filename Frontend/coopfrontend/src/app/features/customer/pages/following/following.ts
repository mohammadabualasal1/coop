import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';

import { MerchantSummary } from '../../../../core/models/marketplace.models';
import { FollowService } from '../../../../core/services/follow.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiEmptyStateComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { MerchantCardComponent } from '../../components/merchant-card/merchant-card';

type PageStatus = 'loading' | 'error' | 'loaded';

@Component({
  selector: 'app-customer-following',
  imports: [UiAlertComponent, UiButtonComponent, UiEmptyStateComponent, UiSpinnerComponent, MerchantCardComponent],
  templateUrl: './following.html',
  styleUrl: './following.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FollowingComponent implements OnInit {
  private readonly followService = inject(FollowService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  private readonly loadedMerchants = signal<MerchantSummary[]>([]);

  readonly merchants = computed(() =>
    this.loadedMerchants().filter((merchant) => this.followService.isFollowing(merchant.id))
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.followService.getAll().subscribe({
      next: (merchants) => {
        this.loadedMerchants.set(merchants);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }
}
