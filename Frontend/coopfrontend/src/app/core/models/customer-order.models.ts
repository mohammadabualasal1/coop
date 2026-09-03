import { DeliveryStatus, OrderStatus, PaymentMethod, PaymentStatus } from '../enums';

export interface CustomerOrderSummary {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  merchantName: string;
  totalAmount: number;
  createdAt: string;
}

export interface CustomerOrderItem {
  id: string;
  offerId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  originalPrice: number;
  lineTotal: number;
}

export interface CustomerOrderDetail {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  merchantName: string;
  branchName: string;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  subtotal: number;
  totalDiscount: number;
  deliveryFee: number;
  totalAmount: number;
  customerNotes: string | null;
  createdAt: string;
  items: CustomerOrderItem[];
}

export interface OrderStatusHistoryEntry {
  status: OrderStatus;
  changedAt: string;
  note?: string | null;
}

export interface OrderTracking {
  orderId: string;
  orderNumber: string;
  status: OrderStatus;
  deliveryStatus?: DeliveryStatus | null;
  driverName?: string | null;
  driverPhone?: string | null;
  vehicleType?: string | null;
  vehiclePlateNumber?: string | null;
  driverLatitude?: number | null;
  driverLongitude?: number | null;
  history: OrderStatusHistoryEntry[];
}

export interface OrderStatusChangedEvent {
  orderId: string;
  status: OrderStatus;
  changedAt: string;
}

export interface DriverAssignedEvent {
  orderId: string;
  driverName: string;
  driverPhone: string;
  vehicleType: string;
  vehiclePlateNumber: string;
}

export interface DeliveryStatusChangedEvent {
  orderId: string;
  deliveryStatus: DeliveryStatus;
}

export interface DriverLocationUpdatedEvent {
  orderId: string;
  latitude: number;
  longitude: number;
}
