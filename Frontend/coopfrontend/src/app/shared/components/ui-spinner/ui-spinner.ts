import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'coop-spinner',
  template: `
    <span
      class="spinner"
      [style.width.px]="size()"
      [style.height.px]="size()"
      aria-hidden="true"
    ></span>
  `,
  styles: `
    :host {
      display: inline-flex;
    }

    .spinner {
      display: inline-block;
      border: 2px solid color-mix(in srgb, currentColor 30%, transparent);
      border-top-color: currentColor;
      border-radius: var(--coop-radius-full);
      animation: coop-spin 0.6s linear infinite;
    }

    @keyframes coop-spin {
      to {
        transform: rotate(360deg);
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiSpinnerComponent {
  readonly size = input(20);
}
