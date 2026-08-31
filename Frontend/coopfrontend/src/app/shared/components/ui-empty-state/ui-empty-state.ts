import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'coop-empty-state',
  template: `
    <div class="empty-state">
      @if (title()) {
        <p class="empty-title">{{ title() }}</p>
      }
      @if (message()) {
        <p class="empty-message">{{ message() }}</p>
      }
      <ng-content select="[coopEmptyStateAction]" />
    </div>
  `,
  styles: `
    :host {
      display: block;
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--coop-space-sm);
      padding: var(--coop-space-2xl) var(--coop-space-lg);
      text-align: center;
      color: var(--coop-text-muted);
    }

    .empty-title {
      margin: 0;
      font-size: 1rem;
      font-weight: 600;
      color: var(--coop-text);
    }

    .empty-message {
      margin: 0;
      font-size: 0.875rem;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiEmptyStateComponent {
  readonly title = input<string | null>(null);
  readonly message = input<string | null>(null);
}
