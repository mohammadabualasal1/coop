import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/storage/recent_search_storage_service.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/error_state.dart';
import '../providers/categories_provider.dart';

/// Results grid is intentionally not wired yet — `GET /api/marketplace/offers`
/// response shape hasn't been confirmed. Submitting a search saves it to
/// local history and shows a placeholder until that model exists.
class SearchScreen extends ConsumerStatefulWidget {
  const SearchScreen({super.key});

  @override
  ConsumerState<SearchScreen> createState() => _SearchScreenState();
}

class _SearchScreenState extends ConsumerState<SearchScreen> {
  final _storage = RecentSearchStorageService();
  final _controller = TextEditingController();
  List<String> _recentSearches = [];
  String? _submittedQuery;

  @override
  void initState() {
    super.initState();
    _loadRecentSearches();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _loadRecentSearches() async {
    final searches = await _storage.getRecentSearches();
    if (mounted) setState(() => _recentSearches = searches);
  }

  Future<void> _submit(String query) async {
    if (query.trim().isEmpty) return;
    await _storage.addSearch(query);
    await _loadRecentSearches();
    setState(() => _submittedQuery = query.trim());
  }

  Future<void> _clearHistory() async {
    await _storage.clear();
    await _loadRecentSearches();
  }

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;

    return Scaffold(
      appBar: AppBar(
        title: TextField(
          controller: _controller,
          textInputAction: TextInputAction.search,
          autofocus: _submittedQuery == null,
          decoration: const InputDecoration(
            hintText: 'ابحث عن منتجات أو متاجر',
            prefixIcon: Icon(Icons.search_rounded),
            isDense: true,
          ),
          onSubmitted: _submit,
        ),
      ),
      body: _submittedQuery != null
          ? _SearchPlaceholderResult(
              query: _submittedQuery!,
              onBack: () => setState(() => _submittedQuery = null),
            )
          : ListView(
              padding: const EdgeInsets.all(AppSpacing.marginMobile),
              children: [
                if (_recentSearches.isNotEmpty) ...[
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('عمليات البحث الأخيرة', style: textTheme.headlineSmall),
                      TextButton(onPressed: _clearHistory, child: const Text('مسح')),
                    ],
                  ),
                  const SizedBox(height: AppSpacing.stackSm),
                  Wrap(
                    spacing: AppSpacing.stackSm,
                    runSpacing: AppSpacing.stackSm,
                    children: [
                      for (final term in _recentSearches)
                        ActionChip(
                          avatar: const Icon(Icons.history_rounded, size: 18),
                          label: Text(term),
                          onPressed: () {
                            _controller.text = term;
                            _submit(term);
                          },
                        ),
                    ],
                  ),
                  const SizedBox(height: AppSpacing.stackLg),
                ],
                Text('اكتشف الفئات', style: textTheme.headlineSmall),
                const SizedBox(height: AppSpacing.stackMd),
                const _CategoryShortcuts(),
              ],
            ),
    );
  }
}

class _CategoryShortcuts extends ConsumerWidget {
  const _CategoryShortcuts();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categoriesAsync = ref.watch(categoriesProvider);

    return categoriesAsync.when(
      loading: () => const SizedBox(
        height: 96,
        child: Center(child: CircularProgressIndicator()),
      ),
      error: (error, _) => ErrorState(message: error.toString()),
      data: (categories) {
        if (categories.isEmpty) return const SizedBox.shrink();
        return GridView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: 2,
            mainAxisSpacing: AppSpacing.stackMd,
            crossAxisSpacing: AppSpacing.stackMd,
            childAspectRatio: 1.8,
          ),
          itemCount: categories.length,
          itemBuilder: (context, index) {
            final category = categories[index];
            return Container(
              padding: const EdgeInsets.all(AppSpacing.stackMd),
              decoration: BoxDecoration(
                color: AppColors.surface,
                borderRadius: BorderRadius.circular(AppRadius.lg),
                boxShadow: const [
                  BoxShadow(color: AppColors.cardShadow, blurRadius: 20, offset: Offset(0, 4)),
                ],
              ),
              child: Row(
                children: [
                  Container(
                    width: 40,
                    height: 40,
                    decoration: const BoxDecoration(
                      color: AppColors.surfaceContainerLow,
                      shape: BoxShape.circle,
                    ),
                    clipBehavior: Clip.antiAlias,
                    child: category.imageUrl != null
                        ? Image.network(
                            category.imageUrl!,
                            fit: BoxFit.cover,
                            errorBuilder: (_, _, _) => const Icon(
                              Icons.category_rounded,
                              color: AppColors.primary,
                            ),
                          )
                        : const Icon(Icons.category_rounded, color: AppColors.primary),
                  ),
                  const SizedBox(width: AppSpacing.stackSm),
                  Expanded(
                    child: Text(
                      category.nameAr,
                      style: Theme.of(context).textTheme.bodyMedium,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }
}

class _SearchPlaceholderResult extends StatelessWidget {
  const _SearchPlaceholderResult({required this.query, required this.onBack});

  final String query;
  final VoidCallback onBack;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.gutter),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.search_rounded, size: 48, color: AppColors.outlineVariant),
            const SizedBox(height: AppSpacing.stackMd),
            Text(
              'نتائج البحث عن "$query" قريباً',
              style: textTheme.headlineSmall,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.stackSm),
            Text(
              'قائمة نتائج البحث قيد الإنشاء',
              style: textTheme.bodyMedium?.copyWith(color: AppColors.onSurfaceVariant),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.stackLg),
            OutlinedButton(onPressed: onBack, child: const Text('رجوع')),
          ],
        ),
      ),
    );
  }
}
