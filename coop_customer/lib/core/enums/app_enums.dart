/// Int-mapped enums matching the backend contract (API reference §3).
///
/// The API sends and receives integers, never strings. Every enum here
/// mirrors that: construct with [fromValue], serialize with [value].
/// Unknown values fall back to a safe default rather than throwing, since
/// the backend can add statuses the client hasn't shipped support for yet.
library;

enum UserRole {
  customer(0),
  merchant(1),
  driver(2),
  admin(3);

  const UserRole(this.value);
  final int value;

  static UserRole fromValue(int value) =>
      UserRole.values.firstWhere((e) => e.value == value, orElse: () => UserRole.customer);
}

enum UserStatus {
  active(0),
  suspended(1),
  deleted(2);

  const UserStatus(this.value);
  final int value;

  static UserStatus fromValue(int value) => UserStatus.values.firstWhere(
    (e) => e.value == value,
    orElse: () => UserStatus.active,
  );
}

/// Used by both Merchant and DriverProfile verification.
enum VerificationStatus {
  pending(0),
  approved(1),
  rejected(2),
  needsInformation(3);

  const VerificationStatus(this.value);
  final int value;

  static VerificationStatus fromValue(int value) => VerificationStatus.values
      .firstWhere((e) => e.value == value, orElse: () => VerificationStatus.pending);
}

enum VerificationCodePurpose {
  accountVerification(0),
  passwordReset(1);

  const VerificationCodePurpose(this.value);
  final int value;

  static VerificationCodePurpose fromValue(int value) => VerificationCodePurpose.values
      .firstWhere((e) => e.value == value, orElse: () => VerificationCodePurpose.accountVerification);
}

enum OfferStatus {
  draft(0),
  pendingApproval(1),
  approved(2),
  rejected(3),
  scheduled(4),
  active(5),
  paused(6),
  soldOut(7),
  expired(8),
  cancelled(9);

  const OfferStatus(this.value);
  final int value;

  static OfferStatus fromValue(int value) =>
      OfferStatus.values.firstWhere((e) => e.value == value, orElse: () => OfferStatus.draft);
}

enum OrderStatus {
  pendingPayment(0),
  pendingMerchantConfirmation(1),
  accepted(2),
  rejected(3),
  preparing(4),
  readyForPickup(5),
  driverAssigned(6),
  outForDelivery(7),
  delivered(8),
  completed(9),
  cancelled(10),
  deliveryFailed(11);

  const OrderStatus(this.value);
  final int value;

  static OrderStatus fromValue(int value) => OrderStatus.values.firstWhere(
    (e) => e.value == value,
    orElse: () => OrderStatus.pendingPayment,
  );

  /// Customer-initiated cancellation is only allowed through Accepted.
  bool get isCancellable =>
      this == OrderStatus.pendingPayment ||
      this == OrderStatus.pendingMerchantConfirmation ||
      this == OrderStatus.accepted;
}

enum DeliveryStatus {
  searchingDriver(0),
  offered(1),
  assigned(2),
  goingToMerchant(3),
  arrivedAtMerchant(4),
  pickedUp(5),
  goingToCustomer(6),
  arrivedAtCustomer(7),
  delivered(8),
  failed(9),
  cancelled(10);

  const DeliveryStatus(this.value);
  final int value;

  static DeliveryStatus fromValue(int value) => DeliveryStatus.values.firstWhere(
    (e) => e.value == value,
    orElse: () => DeliveryStatus.searchingDriver,
  );
}

enum PaymentMethod {
  cashOnDelivery(0),
  mockOnlinePayment(1);

  const PaymentMethod(this.value);
  final int value;

  static PaymentMethod fromValue(int value) => PaymentMethod.values.firstWhere(
    (e) => e.value == value,
    orElse: () => PaymentMethod.cashOnDelivery,
  );
}

enum PaymentStatus {
  pending(0),
  paid(1),
  failed(2),
  refunded(3);

  const PaymentStatus(this.value);
  final int value;

  static PaymentStatus fromValue(int value) => PaymentStatus.values.firstWhere(
    (e) => e.value == value,
    orElse: () => PaymentStatus.pending,
  );
}

enum StockReservationStatus {
  active(0),
  confirmed(1),
  released(2),
  expired(3);

  const StockReservationStatus(this.value);
  final int value;

  static StockReservationStatus fromValue(int value) => StockReservationStatus.values
      .firstWhere((e) => e.value == value, orElse: () => StockReservationStatus.active);
}

enum DriverTaskOfferStatus {
  pending(0),
  accepted(1),
  rejected(2),
  expired(3);

  const DriverTaskOfferStatus(this.value);
  final int value;

  static DriverTaskOfferStatus fromValue(int value) => DriverTaskOfferStatus.values
      .firstWhere((e) => e.value == value, orElse: () => DriverTaskOfferStatus.pending);
}

enum ComplaintStatus {
  open(0),
  underReview(1),
  resolved(2),
  rejected(3);

  const ComplaintStatus(this.value);
  final int value;

  static ComplaintStatus fromValue(int value) => ComplaintStatus.values.firstWhere(
    (e) => e.value == value,
    orElse: () => ComplaintStatus.open,
  );
}

enum ReviewStatus {
  visible(0),
  hidden(1);

  const ReviewStatus(this.value);
  final int value;

  static ReviewStatus fromValue(int value) => ReviewStatus.values.firstWhere(
    (e) => e.value == value,
    orElse: () => ReviewStatus.visible,
  );
}

enum ConfirmationTokenType {
  merchantPickup(0),
  customerDelivery(1);

  const ConfirmationTokenType(this.value);
  final int value;

  static ConfirmationTokenType fromValue(int value) => ConfirmationTokenType.values
      .firstWhere((e) => e.value == value, orElse: () => ConfirmationTokenType.merchantPickup);
}

enum DevicePlatform {
  android(0),
  ios(1),
  web(2);

  const DevicePlatform(this.value);
  final int value;

  static DevicePlatform fromValue(int value) => DevicePlatform.values.firstWhere(
    (e) => e.value == value,
    orElse: () => DevicePlatform.android,
  );
}
