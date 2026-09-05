import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  viewChild
} from '@angular/core';

let nextFieldId = 0;

@Component({
  selector: 'coop-field',
  template: `
    <div class="field">
      @if (label()) {
        <label class="field-label" [for]="fieldId">
          {{ label() }}
          @if (required()) {
            <span class="required" aria-hidden="true">*</span>
          }
        </label>
      }

      <div class="control" [class.has-error]="showError()" #controlHost>
        <ng-content />
      </div>

      @if (showError()) {
        <p class="field-error">{{ error() }}</p>
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
    }

    .field {
      display: flex;
      flex-direction: column;
      gap: 6px;
      margin-block-end: 20px;
    }

    .field-label {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--coop-text);
    }

    .required {
      color: var(--coop-danger);
      margin-inline-start: 2px;
    }

    :host ::ng-deep .control input,
    :host ::ng-deep .control select,
    :host ::ng-deep .control textarea {
      width: 100%;
      min-height: 44px;
      padding-inline: var(--coop-space-md);
      border: 1px solid var(--coop-border);
      border-radius: var(--coop-radius-md);
      font-family: inherit;
      font-size: 0.9375rem;
      color: var(--coop-text);
      background: var(--coop-surface);
    }

    :host ::ng-deep .control input:focus-visible,
    :host ::ng-deep .control select:focus-visible,
    :host ::ng-deep .control textarea:focus-visible {
      outline: 2px solid var(--coop-primary);
      outline-offset: 2px;
    }

    :host ::ng-deep .control.has-error input,
    :host ::ng-deep .control.has-error select,
    :host ::ng-deep .control.has-error textarea {
      border-color: var(--coop-danger);
    }

    .field-error {
      margin: 0;
      font-size: 0.8125rem;
      color: var(--coop-danger);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiFieldComponent implements AfterViewInit {
  readonly label = input<string | null>(null);
  readonly required = input(false);
  readonly error = input<string | null>(null);
  /** Whether the projected control has been touched (or the form submitted). The
   * error is rendered only when both `error` is set and this is true — a control
   * should never show its error style before the user has interacted with it. */
  readonly touched = input(false);

  readonly showError = computed(() => !!this.error() && this.touched());

  private readonly controlHost = viewChild.required<ElementRef<HTMLElement>>('controlHost');

  readonly fieldId = `coop-field-${nextFieldId++}`;

  ngAfterViewInit(): void {
    const control = this.controlHost().nativeElement.querySelector<HTMLElement>(
      'input, select, textarea'
    );

    if (control && !control.id) {
      control.id = this.fieldId;
    }
  }
}
