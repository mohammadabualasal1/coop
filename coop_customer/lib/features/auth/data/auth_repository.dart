import 'package:dio/dio.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/network/auth_interceptor.dart';
import '../../../core/storage/secure_storage_service.dart';
import '../models/auth_response_model.dart';
import '../models/user_model.dart';
import 'auth_api.dart';

class AuthRepository {
  AuthRepository({
    required AuthApi api,
    required SecureStorageService storage,
    required AuthInterceptor authInterceptor,
  }) : _api = api,
       _storage = storage,
       _authInterceptor = authInterceptor;

  final AuthApi _api;
  final SecureStorageService _storage;
  final AuthInterceptor _authInterceptor;

  Future<UserModel> register({
    required String fullName,
    required String email,
    required String phoneNumber,
    required String password,
  }) async {
    try {
      final response = await _api.register(
        fullName: fullName,
        email: email,
        phoneNumber: phoneNumber,
        password: password,
      );
      await _persistSession(response);
      return response.user;
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<UserModel> login({required String email, required String password}) async {
    try {
      final response = await _api.login(email: email, password: password);
      await _persistSession(response);
      return response.user;
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  /// Called once at app boot. Returns the current user if a refresh token
  /// is stored and still valid (the auth interceptor transparently
  /// refreshes an expired access token), or null otherwise — any failure
  /// here just means "not logged in," not an error to surface.
  Future<UserModel?> restoreSession() async {
    final refreshToken = await _storage.readRefreshToken();
    if (refreshToken == null) return null;
    try {
      return await _api.me();
    } catch (_) {
      return null;
    }
  }

  Future<void> logout() async {
    final refreshToken = await _storage.readRefreshToken();
    if (refreshToken != null) {
      try {
        await _api.logout(refreshToken);
      } catch (_) {
        // Best-effort server-side revocation; local session is cleared
        // regardless so the user is signed out either way.
      }
    }
    await _storage.clearTokens();
    _authInterceptor.setAccessToken(null);
  }

  /// Returns the dev-only simulated code, or null in a real deployment.
  Future<String?> forgotPassword(String email) async {
    try {
      return await _api.forgotPassword(email);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<void> resetPassword({
    required String email,
    required String code,
    required String newPassword,
  }) async {
    try {
      await _api.resetPassword(email: email, code: code, newPassword: newPassword);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }

  Future<void> _persistSession(AuthResponseModel response) async {
    await _storage.saveTokens(
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
    );
    _authInterceptor.setAccessToken(response.accessToken);
  }
}
