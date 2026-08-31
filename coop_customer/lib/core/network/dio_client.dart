import 'dart:io';

import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:flutter/foundation.dart';

import '../config/api_config.dart';
import '../storage/secure_storage_service.dart';
import 'auth_interceptor.dart';

class DioClientBundle {
  DioClientBundle({required this.dio, required this.authInterceptor});

  final Dio dio;
  final AuthInterceptor authInterceptor;
}

/// Builds the app's Dio instance wired with the auth interceptor.
///
/// [onSessionExpired] is invoked when a 401 survives a refresh attempt
/// (refresh token missing, expired, or revoked) — the caller should route
/// to the login screen.
DioClientBundle buildDioClient({
  required SecureStorageService storage,
  required VoidCallback onSessionExpired,
}) {
  final baseOptions = BaseOptions(
    baseUrl: ApiConfig.apiBaseUrl,
    connectTimeout: const Duration(seconds: 15),
    receiveTimeout: const Duration(seconds: 15),
    contentType: 'application/json',
  );

  final dio = Dio(baseOptions);
  // Separate, interceptor-free client for the refresh call itself so a
  // failed refresh can't recursively trigger another refresh attempt.
  final refreshDio = Dio(baseOptions);

  _applyDebugCertificateOverride(dio);
  _applyDebugCertificateOverride(refreshDio);

  final authInterceptor = AuthInterceptor(
    dio: dio,
    refreshDio: refreshDio,
    storage: storage,
    onSessionExpired: onSessionExpired,
  );

  dio.interceptors.add(authInterceptor);
  if (kDebugMode) {
    dio.interceptors.add(LogInterceptor(requestBody: true, responseBody: true));
  }

  return DioClientBundle(dio: dio, authInterceptor: authInterceptor);
}

/// Accepts the backend's self-signed development TLS certificate.
///
/// DEBUG-ONLY — guarded by [kDebugMode], which is stripped out of
/// release/profile builds by the compiler. This must never run against a
/// real deployment: it disables certificate validation entirely for this
/// HTTP client, which would allow trivial MITM interception in production.
void _applyDebugCertificateOverride(Dio dio) {
  if (!kDebugMode) return;
  final adapter = IOHttpClientAdapter();
  adapter.createHttpClient = () {
    final client = HttpClient();
    client.badCertificateCallback = (cert, host, port) => true;
    return client;
  };
  dio.httpClientAdapter = adapter;
}
