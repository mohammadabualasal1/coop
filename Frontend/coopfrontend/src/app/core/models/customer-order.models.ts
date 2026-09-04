import { DeliveryStatus, OrderStatus, PaymentMethod } from '../enums';

export interface CustomerOrderSummary {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  merchantName: string;
  totalAmount: number;
  placedAt: string;
}

export interface CustomerOrderItem {
  id: string;
  productNameSnapshot: string;
  originalUnitPrice: number;
  discountedUnitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface CustomerOrderDetail {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  merchantName: string;
  branchName: string;
  paymentMethod: PaymentMethod;
  subtotal: number;
  totalDiscount: number;
  deliveryFee: number;
  totalAmount: number;
  customerNotes: string | null;
  placedAt: string;
  acceptedAt: string | null;
  readyAt: string | null;
  deliveredAt: string | null;
  completedAt: string | null;
  items: CustomerOrderItem[];
}

export interface OrderStatusHistoryEntry {
  oldStatus: OrderStatus;
  newStatus: OrderStatus;
  note: string | null;
  createdAt: string;
}

export interface OrderDeliveryInfo {
  status: DeliveryStatus;
  driverName: string | null;
  driverLatitude: number | null;
  driverLongitude: number | null;
  lastUpdatedAt: string | null;
}

export interface OrderTracking {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  placedAt: string;
  acceptedAt: string | null;
  readyAt: string | null;
  deliveredAt: string | null;
  completedAt: string | null;
  delivery: OrderDeliveryInfo | null;
  driverPhone: string | null;
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
