import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

export type StatTileTone = 'plain' | 'accent';

@Component({
  selector: 'coop-stat-tile',
  imports: [RouterLink],
  template: `
    @if (link(); as href) {
      <a class="tile" [class]="'tone-' + tone()" [routerLink]="href">
        <span class="tile-icon"><ng-content /></span>
        <span class="tile-value">{{ value() }}</span>
        @if (hint(); as hintText) {
          <span class="tile-hint">{{ hintText }}</span>
        }
        <span class="tile-label">{{ label() }}</span>
      </a>
    } @else {
      <div class="tile" [class]="'tone-' + tone()">
        <span class="tile-icon"><ng-content /></span>
        <span class="tile-value">{{ value() }}</span>
        @if (hint(); as hintText) {
          <span class="tile-hint">{{ hintText }}</span>
        }
        <span class="tile-label">{{ label() }}</span>
      </div>
    }
  `,
  styles: `
    :host {
      display: block;
    }

    .tile {
      display: flex;
      flex-direction: column;
      gap: var(--coop-space-xs);
      padding: var(--coop-space-lg);
      border-radius: var(--coop-radius-lg);
      background: var(--coop-surface);
      box-shadow: var(--coop-shadow-md);
      color: inherit;
      text-decoration: none;
    }

    a.tile {
      cursor: pointer;
      transition: box-shadow 0.15s ease;
    }

    a.tile:hover {
      box-shadow: var(--coop-shadow-lg);
    }

    .tone-accent {
      background: color-mix(in srgb, var(--coop-accent) 30%, var(--coop-surface));
    }

    .tile-icon {
      display: inline-flex;
      color: var(--coop-primary);
    }

    .tile-value {
      font-size: 1.75rem;
      font-weight: 700;
      color: var(--coop-text);
      line-height: 1.2;
    }

    .tile-label {
      font-size: 0.8125rem;
      color: var(--coop-text-muted);
    }

    .tile-hint {
      font-size: 0.75rem;
      color: var(--coop-text-muted);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiStatTileComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly tone = input<StatTileTone>('plain');
  readonly link = input<string | null>(null);
  readonly hint = input<string | null>(null);
}
