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
      background: color-mix(in srgb, var(--coop-text-muted) 15%, var(--coop-surface));
      color: var(--coop-text-muted);
    }

    .tone-info {
      background: color-mix(in srgb, var(--coop-primary) 12%, var(--coop-surface));
      color: var(--coop-primary-dark);
    }

    .tone-success {
      background: color-mix(in srgb, var(--coop-success) 14%, var(--coop-surface));
      color: var(--coop-success);
    }

    .tone-warning {
      background: color-mix(in srgb, var(--coop-warning) 14%, var(--coop-surface));
      color: var(--coop-warning);
    }

    .tone-danger {
      background: color-mix(in srgb, var(--coop-danger) 14%, var(--coop-surface));
      color: var(--coop-danger);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiBadgeComponent {
  readonly tone = input<BadgeTone>('neutral');
}
