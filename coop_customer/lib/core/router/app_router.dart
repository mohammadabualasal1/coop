import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/providers/auth_provider.dart';
import '../../features/auth/screens/forgot_password_screen.dart';
import '../../features/auth/screens/login_screen.dart';
import '../../features/auth/screens/onboarding_screen.dart';
import '../../features/auth/screens/register_screen.dart';
import '../../features/auth/screens/reset_password_screen.dart';
import '../../features/auth/screens/splash_screen.dart';
import '../../features/marketplace/screens/categories_screen.dart';
import '../../features/marketplace/screens/search_screen.dart';
import '../../features/shell/main_shell.dart';
import '../../features/shell/placeholder_tab_screen.dart';
import 'route_paths.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  // Bumping this notifies GoRouter to re-run `redirect` whenever the auth
  // state changes (login, logout, session restore resolving, onboarding
  // marked seen) — see https://pub.dev/packages/go_router #`refreshListenable`.
  final refreshNotifier = ValueNotifier(0);
  ref.listen(authControllerProvider, (_, _) => refreshNotifier.value++);
  ref.onDispose(refreshNotifier.dispose);

  return GoRouter(
    initialLocation: RoutePaths.splash,
    refreshListenable: refreshNotifier,
    redirect: (context, state) {
      final authState = ref.read(authControllerProvider);
      final location = state.matchedLocation;
      final isSplash = location == RoutePaths.splash;

      if (authState.isLoading) {
        return isSplash ? null : RoutePaths.splash;
      }

      final data = authState.valueOrNull;
      // A restore-session failure that isn't a clean "no token" case still
      // means we don't know who the user is — treat it the same as
      // unauthenticated rather than stranding them on the splash screen.
      final isAuthenticated = data?.status == AuthStatus.authenticated;
      final hasSeenOnboarding = data?.hasSeenOnboarding ?? false;
      final isOnAuthRoute = RoutePaths.authRoutes.contains(location);

      if (isAuthenticated) {
        final shouldLeave = isSplash || location == RoutePaths.onboarding || isOnAuthRoute;
        return shouldLeave ? RoutePaths.home : null;
      }

      if (!hasSeenOnboarding) {
        return location == RoutePaths.onboarding ? null : RoutePaths.onboarding;
      }

      if (isSplash || location == RoutePaths.onboarding) return RoutePaths.login;
      return null;
    },
    routes: [
      GoRoute(path: RoutePaths.splash, builder: (context, state) => const SplashScreen()),
      GoRoute(
        path: RoutePaths.onboarding,
        builder: (context, state) => const OnboardingScreen(),
      ),
      GoRoute(path: RoutePaths.login, builder: (context, state) => const LoginScreen()),
      GoRoute(path: RoutePaths.register, builder: (context, state) => const RegisterScreen()),
      GoRoute(
        path: RoutePaths.forgotPassword,
        builder: (context, state) => const ForgotPasswordScreen(),
      ),
      GoRoute(
        path: RoutePaths.resetPassword,
        builder: (context, state) {
          final extra = state.extra as Map<String, dynamic>?;
          return ResetPasswordScreen(
            email: extra?['email'] as String? ?? '',
            simulatedCode: extra?['simulatedCode'] as String?,
          );
        },
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) => MainShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: RoutePaths.home,
                builder: (context, state) => PlaceholderTabScreen(
                  title: 'الرئيسية',
                  icon: Icons.home_rounded,
                  // Temporary — home feed is blocked on the marketplace
                  // offer JSON shape (see task #37). This gets the search
                  // screen reachable in the meantime.
                  action: OutlinedButton.icon(
                    onPressed: () => context.push(RoutePaths.homeSearch),
                    icon: const Icon(Icons.search_rounded, size: 18),
                    label: const Text('بحث'),
                  ),
                ),
                routes: [
                  GoRoute(
                    path: 'search',
                    builder: (context, state) => const SearchScreen(),
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: RoutePaths.categories,
                builder: (context, state) => const CategoriesScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: RoutePaths.orders,
                builder: (context, state) =>
                    const PlaceholderTabScreen(title: 'الطلبات', icon: Icons.receipt_long_rounded),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: RoutePaths.favorites,
                builder: (context, state) =>
                    const PlaceholderTabScreen(title: 'المفضلة', icon: Icons.favorite_rounded),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: RoutePaths.account,
                builder: (context, state) => const PlaceholderTabScreen(
                  title: 'حسابي',
                  icon: Icons.person_rounded,
                  // Temporary — Phase 6 replaces this whole screen with the
                  // real account/profile UI, which will have its own
                  // logout action. Kept here only so the full auth loop
                  // (login -> home -> logout -> login) is testable now.
                  action: _LogoutButton(),
                ),
              ),
            ],
          ),
        ],
      ),
    ],
  );
});

class _LogoutButton extends ConsumerWidget {
  const _LogoutButton();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return OutlinedButton.icon(
      onPressed: () => ref.read(authControllerProvider.notifier).logout(),
      icon: const Icon(Icons.logout_rounded, size: 18),
      label: const Text('تسجيل الخروج'),
    );
  }
}
