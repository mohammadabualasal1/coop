export interface CartItem {
  id: string;
  offerId: string;
  title: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Cart {
  id: string; // Guid.Empty when empty
  merchantBranchId: string;
  items: CartItem[];
  subtotal: number;
  totalDiscount: number;
  estimatedTotal: number;
}

export interface CartValidation {
  isValid: boolean;
  issues: string[]; // Arabic, display as-is
  cart: Cart;
}
