import 'package:shared_preferences/shared_preferences.dart';

/// Tracks whether the user has swiped through the 3-step onboarding once.
/// Not secret, so plain SharedPreferences rather than secure storage.
class OnboardingStorageService {
  static const _seenKey = 'has_seen_onboarding';

  Future<bool> hasSeenOnboarding() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getBool(_seenKey) ?? false;
  }

  Future<void> markOnboardingSeen() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool(_seenKey, true);
  }
}
