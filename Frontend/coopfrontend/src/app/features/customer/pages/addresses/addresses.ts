import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';

import { Address } from '../../../../core/models/address.models';
import { AddressService } from '../../../../core/services/address.service';
import { extractErrorMessage } from '../../../../core/utils/http-error';
import {
  UiAlertComponent,
  UiBadgeComponent,
  UiButtonComponent,
  UiCardComponent,
  UiConfirmModalComponent,
  UiEmptyStateComponent,
  UiSpinnerComponent
} from '../../../../shared/components';
import { AddressFormModalComponent } from '../../components/address-form-modal/address-form-modal';

type PageStatus = 'loading' | 'error' | 'loaded';

@Component({
  selector: 'app-customer-addresses',
  imports: [
    UiCardComponent,
    UiButtonComponent,
    UiBadgeComponent,
    UiAlertComponent,
    UiSpinnerComponent,
    UiEmptyStateComponent,
    UiConfirmModalComponent,
    AddressFormModalComponent
  ],
  templateUrl: './addresses.html',
  styleUrl: './addresses.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AddressesComponent implements OnInit {
  private readonly addressService = inject(AddressService);

  readonly status = signal<PageStatus>('loading');
  readonly loadErrorMessage = signal<string | null>(null);
  readonly addresses = signal<Address[]>([]);

  readonly actionErrorMessage = signal<string | null>(null);
  readonly settingDefaultId = signal<string | null>(null);

  readonly formModalOpen = signal(false);
  readonly editingAddress = signal<Address | null>(null);

  readonly confirmDeleteOpen = signal(false);
  readonly addressToDelete = signal<Address | null>(null);
  readonly deleteSaving = signal(false);
  readonly deleteErrorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.status.set('loading');
    this.loadErrorMessage.set(null);

    this.addressService.getAll().subscribe({
      next: (addresses) => {
        this.addresses.set(addresses);
        this.status.set('loaded');
      },
      error: (err: unknown) => {
        this.loadErrorMessage.set(extractErrorMessage(err));
        this.status.set('error');
      }
    });
  }

  fullAddressLine(address: Address): string {
    let line = `${address.city} — ${address.area} — ${address.street}`;
    const extras: string[] = [];

    if (address.building) {
      extras.push(`مبنى ${address.building}`);
    }

    if (address.floor) {
      extras.push(`طابق ${address.floor}`);
    }

    if (address.additionalDirections) {
      extras.push(address.additionalDirections);
    }

    if (extras.length > 0) {
      line += `، ${extras.join('، ')}`;
    }

    return line;
  }

  openCreateModal(): void {
    this.editingAddress.set(null);
    this.formModalOpen.set(true);
  }

  openEditModal(address: Address): void {
    this.editingAddress.set(address);
    this.formModalOpen.set(true);
  }

  onFormClosed(): void {
    this.formModalOpen.set(false);
  }

  onFormSaved(): void {
    this.formModalOpen.set(false);
    this.load();
  }

  setDefault(address: Address): void {
    if (this.settingDefaultId()) {
      return;
    }

    this.actionErrorMessage.set(null);
    this.settingDefaultId.set(address.id);

    this.addressService.setDefault(address.id).subscribe({
      next: () => {
        this.settingDefaultId.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.settingDefaultId.set(null);
        this.actionErrorMessage.set(extractErrorMessage(err));
      }
    });
  }

  openDeleteConfirm(address: Address): void {
    this.addressToDelete.set(address);
    this.deleteErrorMessage.set(null);
    this.confirmDeleteOpen.set(true);
  }

  closeDeleteConfirm(): void {
    if (this.deleteSaving()) {
      return;
    }

    this.confirmDeleteOpen.set(false);
    this.addressToDelete.set(null);
  }

  confirmDelete(): void {
    const address = this.addressToDelete();

    if (!address || this.deleteSaving()) {
      return;
    }

    this.deleteSaving.set(true);
    this.deleteErrorMessage.set(null);

    this.addressService.remove(address.id).subscribe({
      next: () => {
        this.deleteSaving.set(false);
        this.confirmDeleteOpen.set(false);
        this.addressToDelete.set(null);
        this.load();
      },
      error: (err: unknown) => {
        this.deleteSaving.set(false);
        this.deleteErrorMessage.set(extractErrorMessage(err));
      }
    });
  }
}
