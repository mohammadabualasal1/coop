import 'package:dio/dio.dart';

import '../../../core/enums/app_enums.dart';
import '../models/auth_response_model.dart';
import '../models/user_model.dart';

/// Raw calls to `api/auth` — no error wrapping, that's [AuthRepository]'s job.
class AuthApi {
  AuthApi(this._dio);

  final Dio _dio;

  Future<AuthResponseModel> register({
    required String fullName,
    required String email,
    required String phoneNumber,
    required String password,
  }) async {
    final response = await _dio.post(
      '/auth/register',
      data: {
        'fullName': fullName,
        'email': email,
        'phoneNumber': phoneNumber,
        'password': password,
        'role': UserRole.customer.value,
      },
    );
    return AuthResponseModel.fromJson(response.data as Map<String, dynamic>);
  }

  Future<AuthResponseModel> login({required String email, required String password}) async {
    final response = await _dio.post(
      '/auth/login',
      data: {'email': email, 'password': password},
    );
    return AuthResponseModel.fromJson(response.data as Map<String, dynamic>);
  }

  Future<void> logout(String refreshToken) {
    return _dio.post('/auth/logout', data: {'refreshToken': refreshToken});
  }

  Future<UserModel> me() async {
    final response = await _dio.get('/auth/me');
    return UserModel.fromJson(response.data as Map<String, dynamic>);
  }

  /// Returns the dev-only `simulatedCode` when present (no real email
  /// provider is wired up server-side yet). Must not be relied on once
  /// real delivery ships — see API reference "Known gaps".
  Future<String?> forgotPassword(String email) async {
    final response = await _dio.post('/auth/forgot-password', data: {'email': email});
    final data = response.data as Map<String, dynamic>;
    return data['simulatedCode'] as String?;
  }

  Future<void> resetPassword({
    required String email,
    required String code,
    required String newPassword,
  }) {
    return _dio.post(
      '/auth/reset-password',
      data: {'email': email, 'code': code, 'newPassword': newPassword},
    );
  }
}
