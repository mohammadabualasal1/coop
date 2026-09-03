import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'coop-card',
  template: `
    <div class="card">
      @if (title()) {
        <div class="card-header">
          <h2 class="card-title">{{ title() }}</h2>
        </div>
      }
      <div class="card-body">
        <ng-content />
      </div>
    </div>
  `,
  styles: `
    :host {
      display: block;
    }

    .card {
      background: var(--coop-surface);
      border-radius: var(--coop-radius-lg);
      box-shadow: var(--coop-shadow-lg);
      padding: var(--coop-space-xl);
    }

    .card-header {
      padding-block-end: var(--coop-space-md);
      margin-block-end: var(--coop-space-md);
      border-block-end: 1px solid var(--coop-border);
    }

    .card-title {
      margin: 0;
      font-size: 1.125rem;
      font-weight: 600;
      color: var(--coop-text);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiCardComponent {
  readonly title = input<string | null>(null);
}
