import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type AlertTone = 'info' | 'success' | 'warning' | 'danger';

@Component({
  selector: 'coop-alert',
  template: `
    <div class="alert" [class]="'tone-' + tone()" role="alert">
      <ng-content />
    </div>
  `,
  styles: `
    :host {
      display: block;
    }

    .alert {
      padding: var(--coop-space-sm) var(--coop-space-md);
      border-radius: var(--coop-radius-md);
      font-size: 0.875rem;
    }

    .tone-info {
      background: color-mix(in srgb, var(--coop-primary) 10%, var(--coop-surface));
      color: var(--coop-primary-dark);
    }

    .tone-success {
      background: color-mix(in srgb, var(--coop-success) 12%, var(--coop-surface));
      color: var(--coop-success);
    }

    .tone-warning {
      background: color-mix(in srgb, var(--coop-warning) 12%, var(--coop-surface));
      color: var(--coop-warning);
    }

    .tone-danger {
      background: color-mix(in srgb, var(--coop-danger) 12%, var(--coop-surface));
      color: var(--coop-danger);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiAlertComponent {
  readonly tone = input<AlertTone>('info');
}
