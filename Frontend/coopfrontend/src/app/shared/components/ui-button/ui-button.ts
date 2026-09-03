import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { UiSpinnerComponent } from '../ui-spinner/ui-spinner';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md';
export type ButtonType = 'button' | 'submit';

@Component({
  selector: 'coop-button',
  imports: [UiSpinnerComponent],
  template: `
    <button
      [type]="type()"
      class="btn"
      [class]="'btn-' + variant() + ' btn-' + size()"
      [disabled]="disabled() || loading()"
    >
      @if (loading()) {
        <coop-spinner [size]="16" />
      }
      <span class="btn-label"><ng-content /></span>
    </button>
  `,
  styles: `
    :host {
      display: block;
    }

    .btn {
      width: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--coop-space-sm);
      border: none;
      border-radius: var(--coop-radius-md);
      font-family: inherit;
      font-weight: 600;
      cursor: pointer;
      transition:
        background 0.15s ease,
        color 0.15s ease,
        border-color 0.15s ease;
    }

    .btn-md {
      min-height: 44px;
      padding-inline: var(--coop-space-lg);
      font-size: 0.9375rem;
    }

    .btn-sm {
      min-height: 36px;
      padding-inline: var(--coop-space-md);
      font-size: 0.8125rem;
    }

    .btn-primary {
      background: var(--coop-primary);
      color: var(--coop-surface);
    }

    .btn-primary:hover:not(:disabled) {
      background: var(--coop-primary-dark);
    }

    .btn-secondary {
      background: var(--coop-surface);
      color: var(--coop-primary);
      border: 1px solid var(--coop-border);
    }

    .btn-secondary:hover:not(:disabled) {
      background: var(--coop-bg);
    }

    .btn-ghost {
      background: none;
      color: var(--coop-primary);
    }

    .btn-ghost:hover:not(:disabled) {
      background: color-mix(in srgb, var(--coop-primary) 8%, transparent);
    }

    .btn-danger {
      background: var(--coop-danger);
      color: var(--coop-surface);
    }

    .btn-danger:hover:not(:disabled) {
      background: color-mix(in srgb, var(--coop-danger) 85%, black);
    }

    .btn:disabled {
      opacity: 0.7;
      cursor: not-allowed;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiButtonComponent {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('md');
  readonly type = input<ButtonType>('button');
  readonly disabled = input(false);
  readonly loading = input(false);
}
