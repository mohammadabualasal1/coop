import { ComplaintStatus } from '../enums';

export interface Complaint {
  id: string;
  orderNumber: string | null;
  targetName: string | null;
  category: string;
  description: string;
  evidenceUrl: string | null;
  status: ComplaintStatus;
  adminResponse: string | null;
  createdAt: string;
  resolvedAt: string | null;
}

export interface CreateComplaintRequest {
  orderId?: string | null;
  merchantId?: string | null;
  driverProfileId?: string | null;
  offerId?: string | null;
  category: string;
  description: string;
  evidenceUrl?: string | null;
}
