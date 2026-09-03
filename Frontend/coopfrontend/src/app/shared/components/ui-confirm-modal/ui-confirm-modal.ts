import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { UiAlertComponent } from '../ui-alert/ui-alert';
import { UiButtonComponent } from '../ui-button/ui-button';
import { UiModalComponent } from '../ui-modal/ui-modal';

export type ConfirmTone = 'primary' | 'danger';

@Component({
  selector: 'coop-confirm-modal',
  imports: [UiModalComponent, UiButtonComponent, UiAlertComponent],
  template: `
    <coop-modal [title]="title()" [open]="open()" size="sm" (closed)="cancelled.emit()">
      @if (errorMessage(); as message) {
        <coop-alert tone="danger">{{ message }}</coop-alert>
      }

      <p>{{ message() }}</p>

      @if (note(); as noteText) {
        <p class="note">{{ noteText }}</p>
      }

      <ng-content />

      <div modal-footer>
        <coop-button variant="secondary" size="sm" (click)="cancelled.emit()">
          {{ cancelLabel() }}
        </coop-button>
        <coop-button
          [variant]="tone()"
          size="sm"
          [loading]="saving()"
          [disabled]="saving() || confirmDisabled()"
          (click)="confirmed.emit()"
        >
          {{ confirmLabel() }}
        </coop-button>
      </div>
    </coop-modal>
  `,
  styles: `
    .note {
      margin: var(--coop-space-sm) 0 0;
      font-size: 0.8125rem;
      color: var(--coop-text-muted);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiConfirmModalComponent {
  readonly title = input('');
  readonly message = input('');
  readonly note = input<string | null>(null);
  readonly confirmLabel = input('تأكيد');
  readonly cancelLabel = input('تراجع');
  readonly tone = input<ConfirmTone>('danger');
  readonly open = input(false);
  readonly saving = input(false);
  readonly confirmDisabled = input(false);
  readonly errorMessage = input<string | null>(null);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}
