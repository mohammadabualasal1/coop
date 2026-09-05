import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';

import { Category, CategoryRequest, UpdateCategoryRequest } from '../../../../core/models/category.models';
import { CategoryService } from '../../../../core/services/category.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiBadgeComponent,
  UiButtonComponent,
  UiCardComponent,
  UiConfirmModalComponent,
  UiEmptyStateComponent,
  UiFieldComponent,
  UiModalComponent,
  UiSpinnerComponent
} from '../../../../shared/components';

type PageStatus = 'loading' | 'error' | 'loaded';

interface CategoryTreeNode {
  root: Category;
  children: Category[];
}

function isValidUrl(value: string): boolean {
  if (!value) {
    return false;
  }

  try {
    new URL(value);
    return true;
  } catch {
    return false;
  }
}

function optionalUrlValidator(control: AbstractControl): ValidationErrors | null {
  const raw: unknown = control.value;
  const value = typeof raw === 'string' ? raw.trim() : '';
  return !value || isValidUrl(value) ? null : { url: true };
}

@Component({
  selector: 'app-admin-categories',
  imports: [
    ReactiveFormsModule,
    UiCardComponent,
    UiButtonComponent,
    UiBadgeComponent,
    UiAlertComponent,
    UiFieldComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    UiModalComponent,
    UiConfirmModalComponent
  ],
  templateUrl: './categories.html',
  styleUrl: './categories.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CategoriesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly categoryService = inject(CategoryService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly categories = signal<Category[]>([]);

  readonly tree = computed<CategoryTreeNode[]>(() => {
    const roots = [...this.categories()].sort((a, b) => a.displayOrder - b.displayOrder);

    return roots.map((root) => ({
      root,
      children: [...(root.children ?? [])].sort((a, b) => a.displayOrder - b.displayOrder)
    }));
  });

  readonly formModalOpen = signal(false);
  readonly editingCategory = signal<Category | null>(null);
  readonly parentForNewSub = signal<Category | null>(null);
  readonly formParentCategoryId = signal<string | null>(null);
  readonly formSaving = signal(false);
  readonly formErrorMessage = signal<string | null>(null);
  readonly imagePreviewFailed = signal(false);

  readonly modalTitle = computed(() => {
    if (this.editingCategory()) {
      return 'تعديل التصنيف';
    }

    const parent = this.parentForNewSub();
    return parent ? `إضافة تصنيف فرعي إلى ${parent.nameAr}` : 'إضافة تصنيف رئيسي';
  });

  readonly confirmDeactivateOpen = signal(false);
  readonly categoryToDeactivate = signal<Category | null>(null);
  readonly deactivateSaving = signal(false);
  readonly deactivateErrorMessage = signal<string | null>(null);
  readonly deactivateMessage = computed(() => {
    const category = this.categoryToDeactivate();
    return category ? `هل أنت متأكد من إلغاء تفعيل تصنيف «${category.nameAr}»؟` : '';
  });

  readonly reactivatingId = signal<string | null>(null);
  readonly reactivateErrorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    nameAr: ['', [Validators.required, Validators.minLength(2)]],
    nameEn: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
    imageUrl: ['', [optionalUrlValidator]],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    isActive: [true]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.categoryService.getAll(true).subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  firstLetter(name: string): string {
    return name.trim().charAt(0) || '؟';
  }

  imageUrlPreview(): string | null {
    const value = (this.form.controls.imageUrl.value ?? '').trim();
    return isValidUrl(value) ? value : null;
  }

  openCreateRootModal(): void {
    this.editingCategory.set(null);
    this.parentForNewSub.set(null);
    this.formParentCategoryId.set(null);
    this.formErrorMessage.set(null);
    this.imagePreviewFailed.set(false);
    this.form.reset({
      nameAr: '',
      nameEn: '',
      description: '',
      imageUrl: '',
      displayOrder: 0,
      isActive: true
    });
    this.formModalOpen.set(true);
  }

  openCreateSubModal(parent: Category): void {
    this.editingCategory.set(null);
    this.parentForNewSub.set(parent);
    this.formParentCategoryId.set(parent.id);
    this.formErrorMessage.set(null);
    this.imagePreviewFailed.set(false);
    this.form.reset({
      nameAr: '',
      nameEn: '',
      description: '',
      imageUrl: '',
      displayOrder: 0,
      isActive: true
    });
    this.formModalOpen.set(true);
  }

  openEditModal(category: Category): void {
    this.editingCategory.set(category);
    this.parentForNewSub.set(null);
    this.formParentCategoryId.set(category.parentCategoryId);
    this.formErrorMessage.set(null);
    this.imagePreviewFailed.set(false);
    this.form.reset({
      nameAr: category.nameAr,
      nameEn: category.nameEn,
      description: category.description ?? '',
      imageUrl: category.imageUrl ?? '',
      displayOrder: category.displayOrder,
      isActive: category.isActive !== false
    });
    this.formModalOpen.set(true);
  }

  closeFormModal(): void {
    if (this.formSaving()) {
      return;
    }

    this.formModalOpen.set(false);
  }

  nameArErrorMessage(): string | null {
    const control = this.form.controls.nameAr;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'الاسم بالعربية مطلوب';
    }

    return 'الاسم بالعربية قصير جداً';
  }

  nameEnErrorMessage(): string | null {
    const control = this.form.controls.nameEn;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'الاسم بالإنجليزية مطلوب';
    }

    return 'الاسم بالإنجليزية قصير جداً';
  }

  imageUrlErrorMessage(): string | null {
    return this.form.controls.imageUrl.hasError('url') ? 'الرابط غير صالح' : null;
  }

  displayOrderErrorMessage(): string | null {
    const control = this.form.controls.displayOrder;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'ترتيب العرض مطلوب';
    }

    return 'ترتيب العرض يجب أن يكون 0 أو أكثر';
  }

  submitForm(): void {
    if (this.form.invalid || this.formSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const editing = this.editingCategory();

    const body: CategoryRequest = {
      parentCategoryId: this.formParentCategoryId(),
      nameAr: raw.nameAr,
      nameEn: raw.nameEn,
      description: raw.description.trim() ? raw.description : null,
      imageUrl: raw.imageUrl.trim() ? raw.imageUrl : null,
      displayOrder: raw.displayOrder
    };

    this.formSaving.set(true);
    this.formErrorMessage.set(null);

    const request$ = editing
      ? this.categoryService.update(editing.id, { ...body, isActive: raw.isActive } as UpdateCategoryRequest)
      : this.categoryService.create(body);

    request$.subscribe({
      next: () => {
        this.formSaving.set(false);
        this.formModalOpen.set(false);
        this.load();
      },
      error: (err: unknown) => {
        this.formSaving.set(false);
        this.formErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openDeactivateConfirm(category: Category): void {
    this.categoryToDeactivate.set(category);
    this.deactivateErrorMessage.set(null);
    this.confirmDeactivateOpen.set(true);
  }

  closeDeactivateConfirm(): void {
    if (this.deactivateSaving()) {
      return;
    }

    this.confirmDeactivateOpen.set(false);
    this.categoryToDeactivate.set(null);
  }

  confirmDeactivate(): void {
    const category = this.categoryToDeactivate();

    if (!category || this.deactivateSaving()) {
      return;
    }

    this.deactivateSaving.set(true);
    this.deactivateErrorMessage.set(null);

    this.categoryService.deactivate(category.id).subscribe({
      next: () => {
        this.deactivateSaving.set(false);
        this.confirmDeactivateOpen.set(false);
        this.categoryToDeactivate.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.deactivateSaving.set(false);
        this.deactivateErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  reactivate(category: Category): void {
    if (this.reactivatingId()) {
      return;
    }

    this.reactivatingId.set(category.id);
    this.reactivateErrorMessage.set(null);

    const body: UpdateCategoryRequest = {
      parentCategoryId: category.parentCategoryId,
      nameAr: category.nameAr,
      nameEn: category.nameEn,
      description: category.description,
      imageUrl: category.imageUrl,
      displayOrder: category.displayOrder,
      isActive: true
    };

    this.categoryService.update(category.id, body).subscribe({
      next: () => {
        this.reactivatingId.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.reactivatingId.set(null);
        this.reactivateErrorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
