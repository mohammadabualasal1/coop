import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

type StarState = 'full' | 'half' | 'empty';

const STAR_PATH = 'M12 2l2.9 6.6 7.1.6-5.4 4.7 1.6 7-6.2-3.8-6.2 3.8 1.6-7L2 9.2l7.1-.6L12 2z';

let nextId = 0;

@Component({
  selector: 'coop-rating-stars',
  template: `
    <span class="stars" [class.interactive]="!readonly()" [style.height.px]="size()">
      @for (star of starStates(); track $index) {
        <svg
          [attr.width]="size()"
          [attr.height]="size()"
          viewBox="0 0 24 24"
          class="star"
          (click)="onStarClick($index)"
        >
          @if (star === 'full') {
            <path [attr.d]="starPath" fill="currentColor" stroke="currentColor" stroke-width="1" stroke-linejoin="round" />
          } @else if (star === 'half') {
            <defs>
              <linearGradient [attr.id]="uid + '-' + $index" x1="0" x2="1" y1="0" y2="0">
                <stop offset="50%" stop-color="currentColor" />
                <stop offset="50%" stop-color="transparent" />
              </linearGradient>
            </defs>
            <path
              [attr.d]="starPath"
              [attr.fill]="'url(#' + uid + '-' + $index + ')'"
              stroke="currentColor"
              stroke-width="1"
              stroke-linejoin="round"
            />
          } @else {
            <path [attr.d]="starPath" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linejoin="round" />
          }
        </svg>
      }
    </span>
  `,
  styles: `
    :host {
      display: inline-flex;
      color: var(--coop-accent);
    }

    .stars {
      display: inline-flex;
      align-items: center;
      gap: 2px;
    }

    .star {
      flex-shrink: 0;
    }

    .interactive .star {
      cursor: pointer;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiRatingStarsComponent {
  readonly value = input.required<number>();
  readonly size = input(16);
  readonly readonly = input(true);

  readonly starClicked = output<number>();

  readonly starPath = STAR_PATH;
  readonly uid = `coop-rating-stars-${nextId++}`;

  onStarClick(index: number): void {
    if (this.readonly()) {
      return;
    }

    this.starClicked.emit(index + 1);
  }

  readonly starStates = computed<StarState[]>(() => {
    const rounded = Math.round(Math.min(5, Math.max(0, this.value())) * 2) / 2;

    return Array.from({ length: 5 }, (_, i) => {
      const threshold = i + 1;

      if (rounded >= threshold) {
        return 'full';
      }

      if (rounded >= threshold - 0.5) {
        return 'half';
      }

      return 'empty';
    });
  });
}
