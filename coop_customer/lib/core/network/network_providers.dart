import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../storage/secure_storage_service.dart';
import 'dio_client.dart';

final secureStorageServiceProvider = Provider<SecureStorageService>((ref) {
  return SecureStorageService();
});

final dioClientBundleProvider = Provider<DioClientBundle>((ref) {
  final storage = ref.watch(secureStorageServiceProvider);
  return buildDioClient(
    storage: storage,
    onSessionExpired: () {
      // Phase 2 (auth) wires this to clear session state and redirect to
      // the login screen once the auth provider exists.
    },
  );
});

final dioProvider = Provider<Dio>((ref) {
  return ref.watch(dioClientBundleProvider).dio;
});
