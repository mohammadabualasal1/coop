import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'coop-password-toggle',
  template: `
    <button
      type="button"
      class="toggle"
      [attr.aria-label]="visible() ? 'إخفاء كلمة المرور' : 'إظهار كلمة المرور'"
      (click)="toggled.emit()"
    >
      @if (visible()) {
        <svg
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <path
            d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-7 0-11-8-11-8a20.3 20.3 0 0 1 5.06-6.06M9.9 4.24A10.94 10.94 0 0 1 12 4c7 0 11 8 11 8a20.28 20.28 0 0 1-2.16 3.19"
          />
          <path d="M14.12 14.12a3 3 0 1 1-4.24-4.24" />
          <path d="M1 1l22 22" />
        </svg>
      } @else {
        <svg
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <path d="M1 12s4-7 11-7 11 7 11 7-4 7-11 7-11-7-11-7z" />
          <circle cx="12" cy="12" r="3" />
        </svg>
      }
    </button>
  `,
  styles: `
    :host {
      position: absolute;
      inset-inline-end: var(--coop-space-xs);
      display: flex;
    }

    .toggle {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      padding: 0;
      border: none;
      border-radius: var(--coop-radius-sm);
      background: none;
      color: var(--coop-text-muted);
      cursor: pointer;
    }

    .toggle:hover {
      color: var(--coop-text);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiPasswordToggleComponent {
  readonly visible = input(false);
  readonly toggled = output<void>();
}
