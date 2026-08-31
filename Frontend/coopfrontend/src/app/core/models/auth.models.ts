import { UserRole, UserStatus, VerificationCodePurpose } from '../enums';

export interface AuthUser {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: UserRole;
  status: UserStatus;
  profileImageUrl: string | null;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string; // ISO 8601 UTC
  user: AuthUser;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
  role: UserRole;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface LogoutRequest {
  refreshToken: string;
}

export interface UpdateProfileRequest {
  fullName: string;
  phoneNumber: string;
  profileImageUrl: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface SendVerificationCodeRequest {
  destination: string;
  purpose: VerificationCodePurpose;
}

export interface VerifyCodeRequest {
  destination: string;
  code: string;
  purpose: VerificationCodePurpose;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  code: string;
  newPassword: string;
}

export interface SimulatedCodeResponse {
  message: string;
  // Development-only (§29): must never be shown in a production build.
  simulatedCode?: string;
}
