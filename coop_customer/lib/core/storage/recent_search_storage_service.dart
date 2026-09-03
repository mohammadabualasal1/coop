import 'package:shared_preferences/shared_preferences.dart';

/// Local-only search history — there's no backend endpoint for this, and
/// there's no reason there should be; it's purely a per-device UX nicety.
class RecentSearchStorageService {
  static const _key = 'recent_search_terms';
  static const _maxEntries = 8;

  Future<List<String>> getRecentSearches() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getStringList(_key) ?? const [];
  }

  Future<void> addSearch(String term) async {
    final trimmed = term.trim();
    if (trimmed.isEmpty) return;

    final prefs = await SharedPreferences.getInstance();
    final current = prefs.getStringList(_key) ?? const [];
    final updated = [trimmed, ...current.where((e) => e != trimmed)].take(_maxEntries).toList();
    await prefs.setStringList(_key, updated);
  }

  Future<void> clear() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_key);
  }
}
