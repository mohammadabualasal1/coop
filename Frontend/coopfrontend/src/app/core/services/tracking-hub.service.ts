import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  DeliveryStatusChangedEvent,
  DriverAssignedEvent,
  DriverLocationUpdatedEvent,
  OrderStatusChangedEvent
} from '../models/customer-order.models';
import { TokenStorageService } from './token-storage.service';

@Injectable({ providedIn: 'root' })
export class TrackingHubService {
  private readonly tokenStorage = inject(TokenStorageService);

  private connection: signalR.HubConnection | null = null;
  private connectPromise: Promise<void> | null = null;

  private readonly _orderStatusChanged = new Subject<OrderStatusChangedEvent>();
  private readonly _driverAssigned = new Subject<DriverAssignedEvent>();
  private readonly _deliveryStatusChanged = new Subject<DeliveryStatusChangedEvent>();
  private readonly _driverLocationUpdated = new Subject<DriverLocationUpdatedEvent>();

  readonly orderStatusChanged$: Observable<OrderStatusChangedEvent> =
    this._orderStatusChanged.asObservable();
  readonly driverAssigned$: Observable<DriverAssignedEvent> = this._driverAssigned.asObservable();
  readonly deliveryStatusChanged$: Observable<DeliveryStatusChangedEvent> =
    this._deliveryStatusChanged.asObservable();
  readonly driverLocationUpdated$: Observable<DriverLocationUpdatedEvent> =
    this._driverLocationUpdated.asObservable();

  connect(): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return Promise.resolve();
    }

    if (this.connectPromise) {
      return this.connectPromise;
    }

    const token = this.tokenStorage.accessToken ?? '';

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}?access_token=${token}`)
      .withAutomaticReconnect()
      .build();

    connection.on('order.status.changed', (payload: OrderStatusChangedEvent) =>
      this._orderStatusChanged.next(payload)
    );
    connection.on('delivery.driver.assigned', (payload: DriverAssignedEvent) =>
      this._driverAssigned.next(payload)
    );
    connection.on('delivery.status.changed', (payload: DeliveryStatusChangedEvent) =>
      this._deliveryStatusChanged.next(payload)
    );
    connection.on('delivery.location.updated', (payload: DriverLocationUpdatedEvent) =>
      this._driverLocationUpdated.next(payload)
    );

    this.connection = connection;

    this.connectPromise = connection
      .start()
      .catch((error: unknown) => {
        console.error('Tracking hub connection failed', error);
      })
      .finally(() => {
        this.connectPromise = null;
      });

    return this.connectPromise;
  }

  joinOrder(orderId: string): void {
    this.connection?.invoke('JoinOrder', orderId).catch((error: unknown) => {
      console.error('Failed to join order tracking group', error);
    });
  }

  leaveOrder(orderId: string): void {
    this.connection?.invoke('LeaveOrder', orderId).catch((error: unknown) => {
      console.error('Failed to leave order tracking group', error);
    });
  }
}
