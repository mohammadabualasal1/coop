import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network_providers.dart';
import '../../../core/storage/onboarding_storage_service.dart';
import '../data/auth_api.dart';
import '../data/auth_repository.dart';
import '../models/user_model.dart';

final onboardingStorageServiceProvider = Provider<OnboardingStorageService>((ref) {
  return OnboardingStorageService();
});

final authApiProvider = Provider<AuthApi>((ref) {
  return AuthApi(ref.watch(dioProvider));
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(
    api: ref.watch(authApiProvider),
    storage: ref.watch(secureStorageServiceProvider),
    authInterceptor: ref.watch(dioClientBundleProvider).authInterceptor,
  );
});

enum AuthStatus { unknown, authenticated, unauthenticated }

class AuthState {
  const AuthState({required this.status, required this.hasSeenOnboarding, this.user});

  final AuthStatus status;
  final bool hasSeenOnboarding;
  final UserModel? user;

  AuthState copyWith({AuthStatus? status, bool? hasSeenOnboarding, UserModel? user}) {
    return AuthState(
      status: status ?? this.status,
      hasSeenOnboarding: hasSeenOnboarding ?? this.hasSeenOnboarding,
      user: user ?? this.user,
    );
  }
}

/// Drives both the router redirect (via [AuthStatus]) and the auth screens'
/// loading/error UI (via the surrounding [AsyncValue]) from one source of
/// truth, so a failed login shows inline without a separate error channel.
class AuthController extends AsyncNotifier<AuthState> {
  @override
  Future<AuthState> build() async {
    final (user, hasSeenOnboarding) = await (
      ref.read(authRepositoryProvider).restoreSession(),
      ref.read(onboardingStorageServiceProvider).hasSeenOnboarding(),
    ).wait;

    return AuthState(
      status: user != null ? AuthStatus.authenticated : AuthStatus.unauthenticated,
      hasSeenOnboarding: hasSeenOnboarding,
      user: user,
    );
  }

  Future<void> login({required String email, required String password}) {
    return _runAuthAction(
      () => ref.read(authRepositoryProvider).login(email: email, password: password),
    );
  }

  Future<void> register({
    required String fullName,
    required String email,
    required String phoneNumber,
    required String password,
  }) {
    return _runAuthAction(
      () => ref.read(authRepositoryProvider).register(
        fullName: fullName,
        email: email,
        phoneNumber: phoneNumber,
        password: password,
      ),
    );
  }

  Future<void> logout() async {
    await ref.read(authRepositoryProvider).logout();
    final hasSeenOnboarding = state.valueOrNull?.hasSeenOnboarding ?? true;
    state = AsyncData(
      AuthState(status: AuthStatus.unauthenticated, hasSeenOnboarding: hasSeenOnboarding),
    );
  }

  Future<void> markOnboardingSeen() async {
    await ref.read(onboardingStorageServiceProvider).markOnboardingSeen();
    final current = state.valueOrNull;
    if (current != null) {
      state = AsyncData(current.copyWith(hasSeenOnboarding: true));
    }
  }

  /// Clears a failed login/register attempt's error so the screen can be
  /// revisited without showing a stale message.
  void clearError() {
    final previous = state.valueOrNull;
    if (previous != null) state = AsyncData(previous);
  }

  Future<void> _runAuthAction(Future<UserModel> Function() action) async {
    final previousState = state;
    state = const AsyncValue<AuthState>.loading().copyWithPrevious(previousState);
    final result = await AsyncValue.guard(() async {
      final user = await action();
      final current =
          previousState.valueOrNull ??
          const AuthState(status: AuthStatus.unknown, hasSeenOnboarding: true);
      return current.copyWith(status: AuthStatus.authenticated, user: user);
    });
    state = result.hasError ? result.copyWithPrevious(previousState) : result;
  }
}

final authControllerProvider = AsyncNotifierProvider<AuthController, AuthState>(
  AuthController.new,
);
