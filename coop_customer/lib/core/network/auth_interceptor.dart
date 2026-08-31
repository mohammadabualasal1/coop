import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import '../storage/secure_storage_service.dart';

/// Attaches the bearer token to every request and transparently refreshes
/// on 401, retrying the original request once with the new token.
///
/// Concurrent 401s share a single in-flight refresh call via
/// [_refreshCompleter] so a burst of requests doesn't trigger a burst of
/// refresh calls (each of which would rotate the refresh token and
/// invalidate the others).
class AuthInterceptor extends Interceptor {
  AuthInterceptor({
    required Dio dio,
    required Dio refreshDio,
    required SecureStorageService storage,
    required VoidCallback onSessionExpired,
  }) : _dio = dio,
       _refreshDio = refreshDio,
       _storage = storage,
       _onSessionExpired = onSessionExpired;

  final Dio _dio;
  final Dio _refreshDio;
  final SecureStorageService _storage;
  final VoidCallback _onSessionExpired;

  String? _accessToken;
  Completer<String?>? _refreshCompleter;

  /// Call after login/register/refresh so subsequent requests use the
  /// fresh token without a storage round-trip.
  void setAccessToken(String? token) => _accessToken = token;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final token = _accessToken ??= await _storage.readAccessToken();
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    final requestOptions = err.requestOptions;
    final isUnauthorized = err.response?.statusCode == 401;
    final alreadyRetried = requestOptions.extra['retried'] == true;
    final isAuthEndpoint = requestOptions.path.contains('/auth/refresh');

    if (!isUnauthorized || alreadyRetried || isAuthEndpoint) {
      handler.next(err);
      return;
    }

    final newToken = await _refreshAccessToken();
    if (newToken == null) {
      _onSessionExpired();
      handler.next(err);
      return;
    }

    requestOptions.extra['retried'] = true;
    requestOptions.headers['Authorization'] = 'Bearer $newToken';

    try {
      final response = await _dio.fetch(requestOptions);
      handler.resolve(response);
    } on DioException catch (retryError) {
      handler.next(retryError);
    }
  }

  Future<String?> _refreshAccessToken() {
    final existing = _refreshCompleter;
    if (existing != null) return existing.future;

    final completer = Completer<String?>();
    _refreshCompleter = completer;

    _performRefresh().then(completer.complete).whenComplete(() {
      _refreshCompleter = null;
    });

    return completer.future;
  }

  Future<String?> _performRefresh() async {
    try {
      final refreshToken = await _storage.readRefreshToken();
      if (refreshToken == null) return null;

      final response = await _refreshDio.post(
        '/auth/refresh',
        data: {'refreshToken': refreshToken},
      );

      final data = response.data as Map<String, dynamic>;
      final accessToken = data['accessToken'] as String;
      final newRefreshToken = data['refreshToken'] as String;

      await _storage.saveTokens(
        accessToken: accessToken,
        refreshToken: newRefreshToken,
      );
      _accessToken = accessToken;
      return accessToken;
    } catch (_) {
      await _storage.clearTokens();
      _accessToken = null;
      return null;
    }
  }
}
