import { OrderStatus, PaymentMethod, PaymentStatus } from '../enums';

export interface MerchantOrderItem {
  id: string;
  offerId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  originalPrice: number;
  lineTotal: number;
}

export interface MerchantOrder {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  customerName: string;
  customerPhone: string;
  branchName: string;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  subtotal: number;
  totalDiscount: number;
  deliveryFee: number;
  totalAmount: number;
  customerNotes: string | null;
  createdAt: string;
  acceptedAt: string | null;
  items?: MerchantOrderItem[]; // only on GET /{id}
}

export interface PickupCodeResponse {
  code: string;
  expiresAt: string;
}

export interface MarkReadyResponse {
  order: MerchantOrder;
  pickupCode: string;
  expiresAt: string;
}
