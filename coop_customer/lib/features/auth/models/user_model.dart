import '../../../core/enums/app_enums.dart';

class UserModel {
  const UserModel({
    required this.id,
    required this.fullName,
    required this.email,
    required this.phoneNumber,
    required this.role,
    required this.status,
    this.profileImageUrl,
  });

  final String id;
  final String fullName;
  final String email;
  final String phoneNumber;
  final UserRole role;
  final UserStatus status;
  final String? profileImageUrl;

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id'] as String,
      fullName: json['fullName'] as String,
      email: json['email'] as String,
      phoneNumber: json['phoneNumber'] as String,
      role: UserRole.fromValue(json['role'] as int),
      status: UserStatus.fromValue(json['status'] as int),
      profileImageUrl: json['profileImageUrl'] as String?,
    );
  }

  UserModel copyWith({
    String? fullName,
    String? phoneNumber,
    String? profileImageUrl,
  }) {
    return UserModel(
      id: id,
      fullName: fullName ?? this.fullName,
      email: email,
      phoneNumber: phoneNumber ?? this.phoneNumber,
      role: role,
      status: status,
      profileImageUrl: profileImageUrl ?? this.profileImageUrl,
    );
  }
}
