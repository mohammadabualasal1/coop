export enum VerificationCodePurpose {
  AccountVerification = 0,
  PasswordReset = 1
}

export const VerificationCodePurposeLabels: Record<VerificationCodePurpose, string> = {
  [VerificationCodePurpose.AccountVerification]: 'تفعيل الحساب',
  [VerificationCodePurpose.PasswordReset]: 'استعادة كلمة المرور'
};
