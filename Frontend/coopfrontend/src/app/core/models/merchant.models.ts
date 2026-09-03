import { VerificationStatus } from '../enums';

export interface Merchant {
  id: string;
  ownerUserId: string;
  name: string;
  description: string | null;
  registrationNumber: string | null;
  contactEmail: string;
  contactPhone: string;
  logoUrl: string | null;
  coverImageUrl: string | null;
  verificationStatus: VerificationStatus;
  rejectionReason: string | null;
  isActive: boolean;
  averageRating: number | null;
  createdAt: string;
  verifiedAt: string | null;
  verifiedByUserId: string | null;
}

// registrationNumber and verificationStatus are not editable through this endpoint.
export interface UpdateMerchantRequest {
  name: string;
  description: string | null;
  contactEmail: string;
  contactPhone: string;
  logoUrl: string | null;
  coverImageUrl: string | null;
}
