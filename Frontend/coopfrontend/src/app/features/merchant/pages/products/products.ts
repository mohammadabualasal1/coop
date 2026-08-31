import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { forkJoin } from 'rxjs';

import { Product, ProductImage, ProductRequest, UpdateProductRequest } from '../../../../core/models/product.models';
import { CategoryService } from '../../../../core/services/category.service';
import { ProductService } from '../../../../core/services/product.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiButtonComponent,
  UiCardComponent,
  UiEmptyStateComponent,
  UiFieldComponent,
  UiModalComponent,
  UiSpinnerComponent
} from '../../../../shared/components';

type PageStatus = 'loading' | 'error' | 'loaded';
type ImagesModalStatus = 'loading' | 'error' | 'loaded';

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
  selector: 'app-merchant-products',
  imports: [
    ReactiveFormsModule,
    UiCardComponent,
    UiButtonComponent,
    UiAlertComponent,
    UiFieldComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    UiModalComponent
  ],
  templateUrl: './products.html',
  styleUrl: './products.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(CategoryService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly products = signal<Product[]>([]);

  readonly searchTerm = signal('');

  readonly filteredProducts = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();

    if (!term) {
      return this.products();
    }

    return this.products().filter((product) => {
      const haystacks = [product.name, product.sku ?? '', product.brand ?? ''];
      return haystacks.some((value) => value.toLowerCase().includes(term));
    });
  });

  readonly formModalOpen = signal(false);
  readonly editingProduct = signal<Product | null>(null);
  readonly formSaving = signal(false);
  readonly formErrorMessage = signal<string | null>(null);
  readonly modalTitle = computed(() => (this.editingProduct() ? 'تعديل المنتج' : 'إضافة منتج'));
  readonly mainImagePreviewFailed = signal(false);

  readonly confirmDeactivateOpen = signal(false);
  readonly productToDeactivate = signal<Product | null>(null);
  readonly deactivateSaving = signal(false);
  readonly deactivateErrorMessage = signal<string | null>(null);

  readonly imagesModalOpen = signal(false);
  readonly imagesModalProduct = signal<Product | null>(null);
  readonly imagesModalStatus = signal<ImagesModalStatus>('loading');
  readonly imagesModalErrorMessage = signal<string | null>(null);
  readonly productImages = signal<ProductImage[]>([]);
  readonly addImageSaving = signal(false);
  readonly addImageErrorMessage = signal<string | null>(null);
  readonly removingImageId = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    categoryId: ['', [Validators.required]],
    name: ['', [Validators.required, Validators.minLength(2)]],
    description: ['', [Validators.required]],
    sku: [''],
    brand: [''],
    mainImageUrl: ['', [optionalUrlValidator]],
    isActive: [true]
  });

  readonly addImageForm = this.fb.nonNullable.group({
    fileUrl: ['', [Validators.required, optionalUrlValidator]],
    displayOrder: [0, [Validators.required, Validators.min(0)]]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    forkJoin({
      products: this.productService.getMy(),
      categories: this.categoryService.getAll()
    }).subscribe({
      next: ({ products }) => {
        this.products.set(products);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  onSearchInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchTerm.set(target.value);
  }

  categoryOptions() {
    return this.categoryService.options();
  }

  categoryName(categoryId: string): string {
    return this.categoryService.nameAr(categoryId);
  }

  firstLetter(name: string): string {
    return name.trim().charAt(0) || '؟';
  }

  openCreateModal(): void {
    this.editingProduct.set(null);
    this.formErrorMessage.set(null);
    this.mainImagePreviewFailed.set(false);
    this.form.reset({
      categoryId: '',
      name: '',
      description: '',
      sku: '',
      brand: '',
      mainImageUrl: '',
      isActive: true
    });
    this.formModalOpen.set(true);
  }

  openEditModal(product: Product): void {
    this.editingProduct.set(product);
    this.formErrorMessage.set(null);
    this.mainImagePreviewFailed.set(false);
    this.form.reset({
      categoryId: product.categoryId,
      name: product.name,
      description: product.description,
      sku: product.sku ?? '',
      brand: product.brand ?? '',
      mainImageUrl: product.mainImageUrl ?? '',
      isActive: product.isActive
    });
    this.formModalOpen.set(true);
  }

  closeFormModal(): void {
    if (this.formSaving()) {
      return;
    }

    this.formModalOpen.set(false);
  }

  categoryIdErrorMessage(): string | null {
    return this.form.controls.categoryId.invalid ? 'التصنيف مطلوب' : null;
  }

  nameErrorMessage(): string | null {
    const control = this.form.controls.name;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'اسم المنتج مطلوب';
    }

    if (control.hasError('minlength')) {
      return 'اسم المنتج قصير جداً';
    }

    return null;
  }

  descriptionErrorMessage(): string | null {
    return this.form.controls.description.invalid ? 'الوصف مطلوب' : null;
  }

  mainImageUrlErrorMessage(): string | null {
    return this.form.controls.mainImageUrl.hasError('url') ? 'الرابط غير صالح' : null;
  }

  mainImageUrlPreview(): string | null {
    const value = (this.form.controls.mainImageUrl.value ?? '').trim();
    return isValidUrl(value) ? value : null;
  }

  submitForm(): void {
    if (this.form.invalid || this.formSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();

    const body: ProductRequest = {
      categoryId: raw.categoryId,
      name: raw.name,
      description: raw.description,
      sku: raw.sku.trim() ? raw.sku : null,
      brand: raw.brand.trim() ? raw.brand : null,
      mainImageUrl: raw.mainImageUrl.trim() ? raw.mainImageUrl : null
    };

    this.formSaving.set(true);
    this.formErrorMessage.set(null);

    const editing = this.editingProduct();
    const request$ = editing
      ? this.productService.update(editing.id, { ...body, isActive: raw.isActive } as UpdateProductRequest)
      : this.productService.create(body);

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

  openDeactivateConfirm(product: Product): void {
    this.productToDeactivate.set(product);
    this.deactivateErrorMessage.set(null);
    this.confirmDeactivateOpen.set(true);
  }

  closeDeactivateConfirm(): void {
    if (this.deactivateSaving()) {
      return;
    }

    this.confirmDeactivateOpen.set(false);
    this.productToDeactivate.set(null);
  }

  confirmDeactivate(): void {
    const product = this.productToDeactivate();

    if (!product || this.deactivateSaving()) {
      return;
    }

    this.deactivateSaving.set(true);
    this.deactivateErrorMessage.set(null);

    this.productService.deactivate(product.id).subscribe({
      next: () => {
        this.deactivateSaving.set(false);
        this.confirmDeactivateOpen.set(false);
        this.productToDeactivate.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.deactivateSaving.set(false);
        this.deactivateErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openImagesModal(product: Product): void {
    this.imagesModalProduct.set(product);
    this.imagesModalOpen.set(true);
    this.addImageForm.reset({ fileUrl: '', displayOrder: 0 });
    this.addImageErrorMessage.set(null);
    this.loadImages(product.id);
  }

  closeImagesModal(): void {
    if (this.addImageSaving() || this.removingImageId()) {
      return;
    }

    this.imagesModalOpen.set(false);
    this.imagesModalProduct.set(null);
    this.productImages.set([]);
  }

  private loadImages(productId: string): void {
    this.imagesModalStatus.set('loading');
    this.imagesModalErrorMessage.set(null);

    this.productService.getById(productId).subscribe({
      next: (product) => {
        this.productImages.set(product.images ?? []);
        this.imagesModalStatus.set('loaded');
      },
      error: (err: unknown) => {
        this.imagesModalErrorMessage.set(extractErrorMessage(err));
        this.imagesModalStatus.set('error');
      }
    });
  }

  fileUrlErrorMessage(): string | null {
    const control = this.addImageForm.controls.fileUrl;

    if (!control.invalid) {
      return null;
    }

    if (control.hasError('required')) {
      return 'رابط الصورة مطلوب';
    }

    return 'الرابط غير صالح';
  }

  displayOrderErrorMessage(): string | null {
    return this.addImageForm.controls.displayOrder.invalid ? 'ترتيب العرض مطلوب' : null;
  }

  submitAddImage(): void {
    const product = this.imagesModalProduct();

    if (!product || this.addImageForm.invalid || this.addImageSaving()) {
      this.addImageForm.markAllAsTouched();
      return;
    }

    const raw = this.addImageForm.getRawValue();

    this.addImageSaving.set(true);
    this.addImageErrorMessage.set(null);

    this.productService
      .addImage(product.id, { fileUrl: raw.fileUrl, displayOrder: raw.displayOrder })
      .subscribe({
        next: () => {
          this.addImageSaving.set(false);
          this.addImageForm.reset({ fileUrl: '', displayOrder: 0 });
          this.loadImages(product.id);
        },
        error: (err: unknown) => {
          this.addImageSaving.set(false);
          this.addImageErrorMessage.set(extractErrorMessage(err));
        }
      });
  }

  removeImage(image: ProductImage): void {
    const product = this.imagesModalProduct();

    if (!product || this.removingImageId()) {
      return;
    }

    this.removingImageId.set(image.id);
    this.addImageErrorMessage.set(null);

    this.productService.removeImage(product.id, image.id).subscribe({
      next: () => {
        this.removingImageId.set(null);
        this.loadImages(product.id);
      },
      error: (err: unknown) => {
        this.removingImageId.set(null);
        this.addImageErrorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
