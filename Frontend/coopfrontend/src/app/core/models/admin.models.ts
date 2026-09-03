import { UserRole, UserStatus } from '../enums';

export interface PendingOffer {
  id: string;
  title: string;
  merchantName: string;
  discountPercentage: number;
  submittedAt: string;
}

export interface AdminUser {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: UserRole;
  status: UserStatus;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface GetUsersParams {
  role?: UserRole | null;
  status?: UserStatus | null;
  search?: string | null;
  pageNumber: number;
  pageSize: number;
}

export interface CreateMerchantUserRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
  merchantName: string;
  description: string | null;
  registrationNumber: string | null;
  contactEmail: string;
  contactPhone: string;
  logoUrl: string | null;
  coverImageUrl: string | null;
}

export interface CreateDriverUserRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
  vehicleType: string;
  vehiclePlateNumber: string;
  maximumCapacity: number;
}
