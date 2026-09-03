import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { BadgeTone } from '../../../core/enums';

@Component({
  selector: 'coop-badge',
  template: `
    <span class="badge" [class]="'tone-' + tone()">
      <ng-content />
    </span>
  `,
  styles: `
    :host {
      display: inline-block;
    }

    .badge {
      display: inline-flex;
      align-items: center;
      padding-inline: var(--coop-space-sm);
      padding-block: 2px;
      border-radius: var(--coop-radius-full);
      font-size: 0.75rem;
      font-weight: 600;
      line-height: 1.6;
    }

    .tone-neutral {
      background: rgba(107, 107, 107, 0.14);
      color: var(--coop-text-muted);
    }

    .tone-info {
      background: rgba(139, 30, 63, 0.12);
      color: var(--coop-primary-dark);
    }

    .tone-success {
      background: rgba(30, 122, 70, 0.14);
      color: var(--coop-success);
    }

    .tone-warning {
      background: rgba(183, 121, 31, 0.14);
      color: var(--coop-warning);
    }

    .tone-danger {
      background: rgba(179, 38, 30, 0.14);
      color: var(--coop-danger);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiBadgeComponent {
  readonly tone = input<BadgeTone>('neutral');
}
