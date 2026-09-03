import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

import { UserRole, UserRoleLabels, UserRoleTones, UserStatus, UserStatusLabels, UserStatusTones } from '../../../../core/enums';
import {
  AdminUser,
  CreateDriverUserRequest,
  CreateMerchantUserRequest
} from '../../../../core/models/admin.models';
import { AdminService } from '../../../../core/services/admin.service';
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
  UiPasswordToggleComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';

const PAGE_SIZE = 20;
const PHONE_PATTERN = /^07[789][0-9]{7}$/;
const SEARCH_DEBOUNCE_MS = 400;

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

function parseEnumParam<T extends number>(raw: string | null, enumObj: Record<string, unknown>): T | null {
  if (raw === null) {
    return null;
  }

  const value = Number(raw);
  return !Number.isNaN(value) && Object.values(enumObj).includes(value) ? (value as T) : null;
}

@Component({
  selector: 'app-admin-users',
  imports: [
    ReactiveFormsModule,
    CoopDatePipe,
    UiCardComponent,
    UiButtonComponent,
    UiBadgeComponent,
    UiAlertComponent,
    UiFieldComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    UiModalComponent,
    UiConfirmModalComponent,
    UiPasswordToggleComponent
  ],
  templateUrl: './users.html',
  styleUrl: './users.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UsersComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly UserRole = UserRole;
  readonly UserStatus = UserStatus;

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly users = signal<AdminUser[]>([]);
  readonly totalCount = signal(0);
  readonly pageNumber = signal(1);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)));

  readonly searchInputValue = signal('');
  readonly roleFilter = signal<UserRole | 'all'>('all');
  readonly statusFilter = signal<UserStatus | 'all'>('all');

  readonly successMessage = signal<string | null>(null);

  private readonly searchInput$ = new Subject<string>();

  readonly confirmSuspendOpen = signal(false);
  readonly userToSuspend = signal<AdminUser | null>(null);
  readonly suspendSaving = signal(false);
  readonly suspendErrorMessage = signal<string | null>(null);
  readonly suspendMessage = computed(() => {
    const user = this.userToSuspend();
    return user ? `تعليق حساب "${user.fullName}"؟` : '';
  });

  readonly suspendForm = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.minLength(5)]]
  });

  readonly confirmActivateOpen = signal(false);
  readonly userToActivate = signal<AdminUser | null>(null);
  readonly activateSaving = signal(false);
  readonly activateErrorMessage = signal<string | null>(null);
  readonly activateMessage = computed(() => {
    const user = this.userToActivate();
    return user ? `إعادة تفعيل حساب "${user.fullName}"؟` : '';
  });

  readonly createMerchantModalOpen = signal(false);
  readonly createMerchantSaving = signal(false);
  readonly createMerchantErrorMessage = signal<string | null>(null);
  readonly showMerchantPassword = signal(false);

  readonly createMerchantForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    merchantName: ['', [Validators.required, Validators.minLength(2)]],
    contactEmail: ['', [Validators.required, Validators.email]],
    contactPhone: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    description: [''],
    registrationNumber: [''],
    logoUrl: ['', [optionalUrlValidator]],
    coverImageUrl: ['', [optionalUrlValidator]]
  });

  readonly createDriverModalOpen = signal(false);
  readonly createDriverSaving = signal(false);
  readonly createDriverErrorMessage = signal<string | null>(null);
  readonly showDriverPassword = signal(false);

  readonly createDriverForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    vehicleType: ['', [Validators.required]],
    vehiclePlateNumber: ['', [Validators.required]],
    maximumCapacity: [1, [Validators.required, Validators.min(1)]]
  });

  constructor() {
    this.searchInput$
      .pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        this.pageNumber.set(1);
        this.load();
      });
  }

  ngOnInit(): void {
    this.seedFiltersFromQueryParams();
    this.load();
  }

  private seedFiltersFromQueryParams(): void {
    const params = this.route.snapshot.queryParamMap;
    const role = parseEnumParam<UserRole>(params.get('role'), UserRole);
    const status = parseEnumParam<UserStatus>(params.get('status'), UserStatus);

    if (role !== null) {
      this.roleFilter.set(role);
    }

    if (status !== null) {
      this.statusFilter.set(status);
    }
  }

  private syncFiltersToUrl(): void {
    const role = this.roleFilter();
    const status = this.statusFilter();

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        role: role === 'all' ? null : role,
        status: status === 'all' ? null : status
      },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    const role = this.roleFilter();
    const status = this.statusFilter();

    this.adminService
      .getUsers({
        role: role === 'all' ? null : role,
        status: status === 'all' ? null : status,
        search: this.searchInputValue().trim() || null,
        pageNumber: this.pageNumber(),
        pageSize: PAGE_SIZE
      })
      .subscribe({
        next: (result) => {
          this.users.set(result.items);
          this.totalCount.set(result.totalCount);
          this.status.set('loaded');
        },
        error: (err: unknown) => {
          this.loadErrorMessage.set(extractErrorMessage(err));
          this.status.set('error');
        }
      });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInputValue.set(value);
    this.searchInput$.next(value.trim());
  }

  roleFilterOptionValue(): string {
    const role = this.roleFilter();
    return role === 'all' ? '' : String(role);
  }

  statusFilterOptionValue(): string {
    const status = this.statusFilter();
    return status === 'all' ? '' : String(status);
  }

  onRoleFilterChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.roleFilter.set(value === '' ? 'all' : (Number(value) as UserRole));
    this.pageNumber.set(1);
    this.syncFiltersToUrl();
    this.load();
  }

  onStatusFilterChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.statusFilter.set(value === '' ? 'all' : (Number(value) as UserStatus));
    this.pageNumber.set(1);
    this.syncFiltersToUrl();
    this.load();
  }

  roleLabel(role: UserRole): string {
    return UserRoleLabels[role];
  }

  roleTone(role: UserRole) {
    return UserRoleTones[role];
  }

  statusLabel(status: UserStatus): string {
    return UserStatusLabels[status];
  }

  statusTone(status: UserStatus) {
    return UserStatusTones[status];
  }

  canSuspend(user: AdminUser): boolean {
    return user.role !== UserRole.Admin && user.status === UserStatus.Active;
  }

  canActivate(user: AdminUser): boolean {
    return user.role !== UserRole.Admin && user.status === UserStatus.Suspended;
  }

  goToPreviousPage(): void {
    if (this.pageNumber() <= 1) {
      return;
    }

    this.pageNumber.update((value) => value - 1);
    this.load();
  }

  goToNextPage(): void {
    if (this.pageNumber() >= this.totalPages()) {
      return;
    }

    this.pageNumber.update((value) => value + 1);
    this.load();
  }

  private patchUserStatus(id: string, newStatus: UserStatus): void {
    this.users.update((list) =>
      list.map((user) => (user.id === id ? { ...user, status: newStatus } : user))
    );
  }

  private resetFiltersAfterCreate(): void {
    this.roleFilter.set('all');
    this.statusFilter.set('all');
    this.searchInputValue.set('');
    this.pageNumber.set(1);
  }

  openSuspendConfirm(user: AdminUser): void {
    this.userToSuspend.set(user);
    this.suspendErrorMessage.set(null);
    this.suspendForm.reset({ reason: '' });
    this.confirmSuspendOpen.set(true);
  }

  closeSuspendConfirm(): void {
    if (this.suspendSaving()) {
      return;
    }

    this.confirmSuspendOpen.set(false);
    this.userToSuspend.set(null);
  }

  suspendReasonErrorMessage(): string | null {
    const control = this.suspendForm.controls.reason;
    return control.invalid && control.touched ? 'سبب التعليق مطلوب ولا يقل عن 5 أحرف' : null;
  }

  confirmSuspend(): void {
    const user = this.userToSuspend();

    if (!user || this.suspendForm.invalid || this.suspendSaving()) {
      this.suspendForm.markAllAsTouched();
      return;
    }

    const raw = this.suspendForm.getRawValue();

    this.suspendSaving.set(true);
    this.suspendErrorMessage.set(null);

    this.adminService.suspendUser(user.id, raw.reason).subscribe({
      next: () => {
        this.suspendSaving.set(false);
        this.confirmSuspendOpen.set(false);
        this.userToSuspend.set(null);
        this.patchUserStatus(user.id, UserStatus.Suspended);
      },
      error: (err: unknown) => {
        this.suspendSaving.set(false);
        this.suspendErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openActivateConfirm(user: AdminUser): void {
    this.userToActivate.set(user);
    this.activateErrorMessage.set(null);
    this.confirmActivateOpen.set(true);
  }

  closeActivateConfirm(): void {
    if (this.activateSaving()) {
      return;
    }

    this.confirmActivateOpen.set(false);
    this.userToActivate.set(null);
  }

  confirmActivate(): void {
    const user = this.userToActivate();

    if (!user || this.activateSaving()) {
      return;
    }

    this.activateSaving.set(true);
    this.activateErrorMessage.set(null);

    this.adminService.activateUser(user.id).subscribe({
      next: () => {
        this.activateSaving.set(false);
        this.confirmActivateOpen.set(false);
        this.userToActivate.set(null);
        this.patchUserStatus(user.id, UserStatus.Active);
      },
      error: (err: unknown) => {
        this.activateSaving.set(false);
        this.activateErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openCreateMerchantModal(): void {
    this.successMessage.set(null);
    this.createMerchantErrorMessage.set(null);
    this.showMerchantPassword.set(false);
    this.createMerchantForm.reset({
      fullName: '',
      email: '',
      phoneNumber: '',
      password: '',
      merchantName: '',
      contactEmail: '',
      contactPhone: '',
      description: '',
      registrationNumber: '',
      logoUrl: '',
      coverImageUrl: ''
    });
    this.createMerchantModalOpen.set(true);
  }

  closeCreateMerchantModal(): void {
    if (this.createMerchantSaving()) {
      return;
    }

    this.createMerchantModalOpen.set(false);
  }

  toggleMerchantPasswordVisibility(): void {
    this.showMerchantPassword.update((value) => !value);
  }

  merchantFullNameErrorMessage(): string | null {
    const control = this.createMerchantForm.controls.fullName;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'الاسم مطلوب';
    }

    return 'الاسم قصير جداً';
  }

  merchantEmailErrorMessage(): string | null {
    const control = this.createMerchantForm.controls.email;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'البريد الإلكتروني مطلوب';
    }

    return 'صيغة البريد الإلكتروني غير صحيحة';
  }

  merchantPhoneNumberErrorMessage(): string | null {
    const control = this.createMerchantForm.controls.phoneNumber;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'رقم الهاتف مطلوب';
    }

    return 'رقم الهاتف يجب أن يبدأ بـ 077 أو 078 أو 079 ويتكون من 10 أرقام';
  }

  merchantPasswordErrorMessage(): string | null {
    const control = this.createMerchantForm.controls.password;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'كلمة المرور مطلوبة';
    }

    return 'كلمة المرور يجب أن تكون 6 أحرف على الأقل';
  }

  merchantNameErrorMessage(): string | null {
    const control = this.createMerchantForm.controls.merchantName;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'اسم المتجر مطلوب';
    }

    return 'اسم المتجر قصير جداً';
  }

  merchantContactEmailErrorMessage(): string | null {
    const control = this.createMerchantForm.controls.contactEmail;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'البريد الإلكتروني للتواصل مطلوب';
    }

    return 'صيغة البريد الإلكتروني غير صحيحة';
  }

  merchantContactPhoneErrorMessage(): string | null {
    const control = this.createMerchantForm.controls.contactPhone;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'رقم هاتف التواصل مطلوب';
    }

    return 'رقم الهاتف يجب أن يبدأ بـ 077 أو 078 أو 079 ويتكون من 10 أرقام';
  }

  merchantLogoUrlErrorMessage(): string | null {
    return this.createMerchantForm.controls.logoUrl.hasError('url') ? 'الرابط غير صالح' : null;
  }

  merchantCoverImageUrlErrorMessage(): string | null {
    return this.createMerchantForm.controls.coverImageUrl.hasError('url') ? 'الرابط غير صالح' : null;
  }

  submitCreateMerchant(): void {
    if (this.createMerchantForm.invalid || this.createMerchantSaving()) {
      this.createMerchantForm.markAllAsTouched();
      return;
    }

    const raw = this.createMerchantForm.getRawValue();

    const body: CreateMerchantUserRequest = {
      fullName: raw.fullName,
      email: raw.email,
      phoneNumber: raw.phoneNumber,
      password: raw.password,
      merchantName: raw.merchantName,
      description: raw.description.trim() ? raw.description : null,
      registrationNumber: raw.registrationNumber.trim() ? raw.registrationNumber : null,
      contactEmail: raw.contactEmail,
      contactPhone: raw.contactPhone,
      logoUrl: raw.logoUrl.trim() ? raw.logoUrl : null,
      coverImageUrl: raw.coverImageUrl.trim() ? raw.coverImageUrl : null
    };

    this.createMerchantSaving.set(true);
    this.createMerchantErrorMessage.set(null);

    this.adminService.createMerchantUser(body).subscribe({
      next: () => {
        this.createMerchantSaving.set(false);
        this.createMerchantModalOpen.set(false);
        this.afterCreateSuccess(raw.email);
      },
      error: (err: unknown) => {
        this.createMerchantSaving.set(false);
        this.createMerchantErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openCreateDriverModal(): void {
    this.successMessage.set(null);
    this.createDriverErrorMessage.set(null);
    this.showDriverPassword.set(false);
    this.createDriverForm.reset({
      fullName: '',
      email: '',
      phoneNumber: '',
      password: '',
      vehicleType: '',
      vehiclePlateNumber: '',
      maximumCapacity: 1
    });
    this.createDriverModalOpen.set(true);
  }

  closeCreateDriverModal(): void {
    if (this.createDriverSaving()) {
      return;
    }

    this.createDriverModalOpen.set(false);
  }

  toggleDriverPasswordVisibility(): void {
    this.showDriverPassword.update((value) => !value);
  }

  driverFullNameErrorMessage(): string | null {
    const control = this.createDriverForm.controls.fullName;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'الاسم مطلوب';
    }

    return 'الاسم قصير جداً';
  }

  driverEmailErrorMessage(): string | null {
    const control = this.createDriverForm.controls.email;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'البريد الإلكتروني مطلوب';
    }

    return 'صيغة البريد الإلكتروني غير صحيحة';
  }

  driverPhoneNumberErrorMessage(): string | null {
    const control = this.createDriverForm.controls.phoneNumber;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'رقم الهاتف مطلوب';
    }

    return 'رقم الهاتف يجب أن يبدأ بـ 077 أو 078 أو 079 ويتكون من 10 أرقام';
  }

  driverPasswordErrorMessage(): string | null {
    const control = this.createDriverForm.controls.password;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'كلمة المرور مطلوبة';
    }

    return 'كلمة المرور يجب أن تكون 6 أحرف على الأقل';
  }

  driverVehicleTypeErrorMessage(): string | null {
    const control = this.createDriverForm.controls.vehicleType;
    return control.invalid && control.touched ? 'نوع المركبة مطلوب' : null;
  }

  driverVehiclePlateNumberErrorMessage(): string | null {
    const control = this.createDriverForm.controls.vehiclePlateNumber;
    return control.invalid && control.touched ? 'رقم اللوحة مطلوب' : null;
  }

  driverMaximumCapacityErrorMessage(): string | null {
    const control = this.createDriverForm.controls.maximumCapacity;

    if (!control.invalid || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'السعة القصوى مطلوبة';
    }

    return 'السعة القصوى يجب أن تكون 1 على الأقل';
  }

  submitCreateDriver(): void {
    if (this.createDriverForm.invalid || this.createDriverSaving()) {
      this.createDriverForm.markAllAsTouched();
      return;
    }

    const raw = this.createDriverForm.getRawValue();

    const body: CreateDriverUserRequest = {
      fullName: raw.fullName,
      email: raw.email,
      phoneNumber: raw.phoneNumber,
      password: raw.password,
      vehicleType: raw.vehicleType,
      vehiclePlateNumber: raw.vehiclePlateNumber,
      maximumCapacity: raw.maximumCapacity
    };

    this.createDriverSaving.set(true);
    this.createDriverErrorMessage.set(null);

    this.adminService.createDriverUser(body).subscribe({
      next: () => {
        this.createDriverSaving.set(false);
        this.createDriverModalOpen.set(false);
        this.afterCreateSuccess(raw.email);
      },
      error: (err: unknown) => {
        this.createDriverSaving.set(false);
        this.createDriverErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  private afterCreateSuccess(email: string): void {
    this.resetFiltersAfterCreate();
    this.syncFiltersToUrl();
    this.successMessage.set(`تم إنشاء الحساب: ${email}`);
    this.load();
  }
}
