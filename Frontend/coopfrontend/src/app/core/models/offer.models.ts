import { OfferStatus } from '../enums';

export interface BranchOffer {
  id: string;
  offerId: string;
  merchantBranchId: string;
  branchName?: string;
  totalStock: number;
  reservedStock: number;
  soldStock: number;
  isAvailable: boolean;
}

export interface Offer {
  id: string;
  merchantId: string;
  productId: string;
  productName?: string;
  title: string;
  description: string | null;
  originalPrice: number;
  discountedPrice: number;
  discountPercentage: number; // server-computed, never sent
  startAt: string;
  endAt: string;
  maximumQuantityPerCustomer: number | null;
  status: OfferStatus;
  adminReviewNote: string | null;
  approvedAt: string | null;
  createdAt: string;
  updatedAt: string;
  branches?: BranchOffer[]; // only on GET /offers/{id}
}

export interface OfferRequest {
  productId: string;
  title: string;
  description: string | null;
  originalPrice: number;
  discountedPrice: number;
  startAt: string;
  endAt: string;
  maximumQuantityPerCustomer: number | null;
}
