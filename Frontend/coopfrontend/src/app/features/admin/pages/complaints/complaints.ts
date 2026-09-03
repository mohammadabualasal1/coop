import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ComplaintStatus, ComplaintStatusLabels, ComplaintStatusTones } from '../../../../core/enums';
import { AdminComplaint, ResolveComplaintResponse } from '../../../../core/models/admin.models';
import { AdminService } from '../../../../core/services/admin.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiBadgeComponent,
  UiButtonComponent,
  UiCardComponent,
  UiEmptyStateComponent,
  UiFieldComponent,
  UiModalComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { CoopDatePipe } from '../../../../shared/pipes/coop-date.pipe';

type PageStatus = 'loading' | 'error' | 'loaded';

@Component({
  selector: 'app-admin-complaints',
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
    UiModalComponent
  ],
  templateUrl: './complaints.html',
  styleUrl: './complaints.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ComplaintsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);

  readonly ComplaintStatus = ComplaintStatus;
  readonly allStatuses = [
    ComplaintStatus.Open,
    ComplaintStatus.UnderReview,
    ComplaintStatus.Resolved,
    ComplaintStatus.Rejected
  ];

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly complaints = signal<AdminComplaint[]>([]);
  readonly selectedStatus = signal<ComplaintStatus | 'all'>('all');

  readonly resolveModalOpen = signal(false);
  readonly complaintToResolve = signal<AdminComplaint | null>(null);
  readonly resolveSaving = signal(false);
  readonly resolveErrorMessage = signal<string | null>(null);

  readonly resolveForm = this.fb.nonNullable.group({
    status: [ComplaintStatus.UnderReview, [Validators.required]],
    adminResponse: ['', [Validators.required, Validators.minLength(5)]]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    const selected = this.selectedStatus();

    this.adminService.getComplaints(selected === 'all' ? null : selected).subscribe({
      next: (complaints) => {
        this.complaints.set(complaints);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  selectStatus(value: ComplaintStatus | 'all'): void {
    this.selectedStatus.set(value);
    this.load();
  }

  statusLabel(status: ComplaintStatus): string {
    return ComplaintStatusLabels[status];
  }

  statusTone(status: ComplaintStatus) {
    return ComplaintStatusTones[status];
  }

  canResolve(complaint: AdminComplaint): boolean {
    return complaint.status === ComplaintStatus.Open || complaint.status === ComplaintStatus.UnderReview;
  }

  openResolveModal(complaint: AdminComplaint): void {
    this.complaintToResolve.set(complaint);
    this.resolveErrorMessage.set(null);
    this.resolveForm.reset({ status: ComplaintStatus.UnderReview, adminResponse: '' });
    this.resolveModalOpen.set(true);
  }

  closeResolveModal(): void {
    if (this.resolveSaving()) {
      return;
    }

    this.resolveModalOpen.set(false);
    this.complaintToResolve.set(null);
  }

  statusErrorMessage(): string | null {
    const control = this.resolveForm.controls.status;
    return control.invalid && control.touched ? 'يجب اختيار حالة' : null;
  }

  adminResponseErrorMessage(): string | null {
    const control = this.resolveForm.controls.adminResponse;
    return control.invalid && control.touched ? 'الرد مطلوب ولا يقل عن 5 أحرف' : null;
  }

  submitResolve(): void {
    const complaint = this.complaintToResolve();

    if (!complaint || this.resolveForm.invalid || this.resolveSaving()) {
      this.resolveForm.markAllAsTouched();
      return;
    }

    const raw = this.resolveForm.getRawValue();

    this.resolveSaving.set(true);
    this.resolveErrorMessage.set(null);

    this.adminService
      .resolveComplaint(complaint.id, { status: raw.status, adminResponse: raw.adminResponse })
      .subscribe({
        next: (updated) => {
          this.resolveSaving.set(false);
          this.resolveModalOpen.set(false);
          this.complaintToResolve.set(null);
          this.patchComplaint(updated);
        },
        error: (err: unknown) => {
          this.resolveSaving.set(false);
          this.resolveErrorMessage.set(extractErrorMessage(err));
        }
      });
  }

  private patchComplaint(updated: ResolveComplaintResponse): void {
    this.complaints.update((list) =>
      list.map((complaint) =>
        complaint.id === updated.id
          ? {
              ...complaint,
              status: updated.status,
              adminResponse: updated.adminResponse,
              resolvedAt: updated.resolvedAt
            }
          : complaint
      )
    );
  }
}
