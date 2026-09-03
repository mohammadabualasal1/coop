import { ChangeDetectionStrategy, Component, effect, input, output } from '@angular/core';

export type ModalSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'coop-modal',
  template: `
    @if (open()) {
      <div class="overlay" (click)="closed.emit()">
        <div
          class="modal"
          [class]="'size-' + size()"
          role="dialog"
          aria-modal="true"
          (click)="$event.stopPropagation()"
        >
          <div class="modal-header">
            @if (title()) {
              <h2 class="modal-title">{{ title() }}</h2>
            }
            <button
              type="button"
              class="close-button"
              (click)="closed.emit()"
              aria-label="إغلاق"
            >
              &times;
            </button>
          </div>

          <div class="modal-body">
            <ng-content />
          </div>

          <div class="modal-footer">
            <ng-content select="[modal-footer]" />
          </div>
        </div>
      </div>
    }
  `,
  styles: `
    :host {
      display: contents;
    }

    .overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.45);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: var(--coop-space-lg);
      z-index: 1000;
    }

    .modal {
      width: 100%;
      max-height: 90vh;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      background: var(--coop-surface);
      border-radius: var(--coop-radius-lg);
      box-shadow: var(--coop-shadow-lg);
    }

    .size-sm {
      max-width: 420px;
    }

    .size-md {
      max-width: 640px;
    }

    .size-lg {
      max-width: 880px;
    }

    .modal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--coop-space-md);
      padding: var(--coop-space-lg);
      border-block-end: 1px solid var(--coop-border);
    }

    .modal-title {
      margin: 0;
      font-size: 1.125rem;
      font-weight: 600;
      color: var(--coop-text);
    }

    .close-button {
      flex-shrink: 0;
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      border: none;
      background: none;
      border-radius: var(--coop-radius-md);
      font-size: 1.5rem;
      line-height: 1;
      color: var(--coop-text-muted);
      cursor: pointer;
    }

    .close-button:hover {
      background: var(--coop-bg);
      color: var(--coop-text);
    }

    .modal-body {
      padding: var(--coop-space-lg);
      overflow-y: auto;
      flex: 1;
      min-block-size: 0;
    }

    .modal-footer {
      padding: var(--coop-space-lg);
      border-block-start: 1px solid var(--coop-border);
      display: flex;
      justify-content: flex-end;
      gap: 12px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiModalComponent {
  readonly title = input<string | null>(null);
  readonly open = input(false);
  readonly size = input<ModalSize>('md');

  readonly closed = output<void>();

  constructor() {
    effect((onCleanup) => {
      if (!this.open()) {
        return;
      }

      document.body.style.overflow = 'hidden';

      const handleKeydown = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
          this.closed.emit();
        }
      };

      window.addEventListener('keydown', handleKeydown);

      onCleanup(() => {
        document.body.style.overflow = '';
        window.removeEventListener('keydown', handleKeydown);
      });
    });
  }
}
