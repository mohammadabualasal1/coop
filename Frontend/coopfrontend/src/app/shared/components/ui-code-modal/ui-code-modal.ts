import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

import { CoopDatePipe } from '../../pipes/coop-date.pipe';
import { UiAlertComponent } from '../ui-alert/ui-alert';
import { UiButtonComponent } from '../ui-button/ui-button';
import { UiModalComponent } from '../ui-modal/ui-modal';

const COPY_LABEL_RESET_MS = 2000;

@Component({
  selector: 'coop-code-modal',
  imports: [UiModalComponent, UiAlertComponent, UiButtonComponent, CoopDatePipe],
  template: `
    <coop-modal [title]="title()" [open]="open()" size="sm" (closed)="closed.emit()">
      @if (warning(); as message) {
        <coop-alert tone="warning">{{ message }}</coop-alert>
      }

      <p class="code">{{ code() }}</p>

      <p class="instructions">{{ note() }}</p>

      @if (expiresAt(); as expiresAt) {
        <p class="expiry">ينتهي في {{ expiresAt | coopDate: 'datetime' }}</p>
      }

      <div modal-footer>
        <coop-button variant="secondary" size="sm" (click)="closed.emit()">إغلاق</coop-button>
        <coop-button size="sm" (click)="copy()">
          {{ copied() ? 'تم النسخ' : 'نسخ' }}
        </coop-button>
      </div>
    </coop-modal>
  `,
  styles: `
    .code {
      margin: var(--coop-space-md) 0 0;
      font-size: 2.5rem;
      font-weight: 700;
      letter-spacing: 0.5em;
      text-align: center;
      color: var(--coop-primary);
      user-select: all;
    }

    .instructions {
      margin: var(--coop-space-md) 0 var(--coop-space-xs);
      text-align: center;
      font-size: 0.875rem;
      color: var(--coop-text);
    }

    .expiry {
      margin: 0 0 var(--coop-space-md);
      text-align: center;
      font-size: 0.8125rem;
      color: var(--coop-text-muted);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiCodeModalComponent {
  readonly title = input('الرمز');
  readonly open = input(false);
  readonly code = input<string | null>(null);
  readonly expiresAt = input<string | null>(null);
  readonly note = input('');
  readonly warning = input<string | null>(null);

  readonly closed = output<void>();

  readonly copied = signal(false);
  private copyTimeout: ReturnType<typeof setTimeout> | null = null;

  copy(): void {
    const value = this.code();

    if (!value) {
      return;
    }

    navigator.clipboard
      .writeText(value)
      .then(() => {
        this.copied.set(true);

        if (this.copyTimeout) {
          clearTimeout(this.copyTimeout);
        }

        this.copyTimeout = setTimeout(() => this.copied.set(false), COPY_LABEL_RESET_MS);
      })
      .catch(() => {});
  }
}
