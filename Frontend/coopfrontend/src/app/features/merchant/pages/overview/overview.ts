import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-merchant-overview',
  template: `
    <div class="placeholder-card">
      <h2>الرئيسية</h2>
      <p>قيد الإنشاء</p>
    </div>
  `,
  styles: `
    .placeholder-card {
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
      margin: 0;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OverviewComponent {}
