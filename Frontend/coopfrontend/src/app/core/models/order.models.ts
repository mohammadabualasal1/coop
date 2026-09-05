import { OrderStatus, PaymentMethod } from '../enums';

export interface MerchantOrderItem {
  id: string;
  productName: string;
  quantity: number;
  discountedUnitPrice: number;
  lineTotal: number;
}

// GET /merchant-orders and the accept/reject responses — nothing more than this.
export interface MerchantOrderSummary {
  id: string;
  orderNumber: string;
  customerName: string;
  status: OrderStatus;
  totalAmount: number;
  placedAt: string;
}

// GET /merchant-orders/{id} — matches MerchantOrderDetailResponseDto exactly.
export interface MerchantOrderDetail {
  id: string;
  orderNumber: string;
  customerName: string;
  customerPhone: string;
  status: OrderStatus;
  paymentMethod: PaymentMethod;
  subtotal: number;
  totalDiscount: number;
  deliveryFee: number;
  totalAmount: number;
  customerNotes: string | null;
  placedAt: string;
  acceptedAt: string | null;
  readyAt: string | null;
  items: MerchantOrderItem[];
}

export interface PickupCodeResponse {
  code: string;
  expiresAt: string;
}

export interface MarkReadyResponse {
  order: MerchantOrderSummary;
  pickupCode: string | null;
  pickupCodeExpiresAt: string | null;
}

export interface PlaceOrderRequest {
  customerAddressId: string;
  paymentMethod: PaymentMethod;
  customerNotes: string | null;
}

export interface PlaceOrderResponse {
  id: string;
}
