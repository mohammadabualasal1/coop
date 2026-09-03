import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-offer-detail',
  template: `
    <div class="placeholder-card">
      <h2>تفاصيل العرض</h2>
      <p>معرّف العرض: {{ offerId }}</p>
      <p>قيد الإنشاء</p>
    </div>
  `,
  styles: `
    .placeholder-card {
      max-width: 1280px;
      margin-inline: auto;
      background: var(--coop-surface);
      border: 1px solid var(--coop-border);
      border-radius: var(--coop-radius-lg);
      padding: var(--coop-space-xl);
      text-align: center;
      color: var(--coop-text-muted);
    }

    h2 {
      margin: 0 0 var(--coop-space-sm);
      color: var(--coop-text);
      font-size: 1.125rem;
      font-weight: 600;
    }

    p {
      margin: 0 0 var(--coop-space-xs);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OfferDetailComponent {
  private readonly route = inject(ActivatedRoute);

  readonly offerId = this.route.snapshot.paramMap.get('id');
}
