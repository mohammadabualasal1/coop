/// Route path constants.
abstract final class RoutePaths {
  static const splash = '/splash';
  static const onboarding = '/onboarding';
  static const login = '/login';
  static const register = '/register';
  static const forgotPassword = '/forgot-password';
  static const resetPassword = '/reset-password';

  static const home = '/home';
  static const homeSearch = '/home/search';
  static const categories = '/categories';
  static const orders = '/orders';
  static const favorites = '/favorites';
  static const account = '/account';

  static const authRoutes = [login, register, forgotPassword, resetPassword];
}
