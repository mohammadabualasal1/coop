import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/loading_skeleton.dart';
import '../models/category_model.dart';
import '../providers/categories_provider.dart';

class CategoriesScreen extends ConsumerWidget {
  const CategoriesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categoriesAsync = ref.watch(categoriesProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('الأقسام')),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(categoriesProvider.future),
        child: categoriesAsync.when(
          loading: () => const _CategoriesGridSkeleton(),
          error: (error, _) => ErrorState(
            message: error.toString(),
            onRetry: () => ref.invalidate(categoriesProvider),
          ),
          data: (categories) {
            final topLevel = buildCategoryTree(categories);
            if (topLevel.isEmpty) {
              return const ErrorState(message: 'لا توجد أقسام متاحة حالياً');
            }
            return GridView.builder(
              padding: const EdgeInsets.all(AppSpacing.marginMobile),
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                mainAxisSpacing: AppSpacing.gutter,
                crossAxisSpacing: AppSpacing.gutter,
                childAspectRatio: 0.95,
              ),
              itemCount: topLevel.length,
              itemBuilder: (context, index) {
                final node = topLevel[index];
                return _CategoryCard(
                  category: node.category,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('قائمة عروض هذا القسم — قريباً')),
                    );
                  },
                );
              },
            );
          },
        ),
      ),
    );
  }
}

class _CategoryCard extends StatelessWidget {
  const _CategoryCard({required this.category, required this.onTap});

  final CategoryModel category;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(AppRadius.lg),
      child: Container(
        padding: const EdgeInsets.all(AppSpacing.stackMd),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(AppRadius.lg),
          boxShadow: const [
            BoxShadow(color: AppColors.cardShadow, blurRadius: 20, offset: Offset(0, 4)),
          ],
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              width: 64,
              height: 64,
              decoration: const BoxDecoration(
                color: AppColors.surfaceContainerLow,
                shape: BoxShape.circle,
              ),
              clipBehavior: Clip.antiAlias,
              child: category.imageUrl != null
                  ? Image.network(
                      category.imageUrl!,
                      fit: BoxFit.cover,
                      errorBuilder: (_, _, _) => const _CategoryIconFallback(),
                    )
                  : const _CategoryIconFallback(),
            ),
            const SizedBox(height: AppSpacing.stackSm),
            Text(
              category.nameAr,
              style: textTheme.headlineSmall,
              textAlign: TextAlign.center,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
            if (category.description != null) ...[
              const SizedBox(height: 2),
              Text(
                category.description!,
                style: textTheme.bodySmall,
                textAlign: TextAlign.center,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _CategoryIconFallback extends StatelessWidget {
  const _CategoryIconFallback();

  @override
  Widget build(BuildContext context) {
    return const Icon(Icons.category_rounded, size: 32, color: AppColors.primary);
  }
}

class _CategoriesGridSkeleton extends StatelessWidget {
  const _CategoriesGridSkeleton();

  @override
  Widget build(BuildContext context) {
    return GridView.builder(
      padding: const EdgeInsets.all(AppSpacing.marginMobile),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        mainAxisSpacing: AppSpacing.gutter,
        crossAxisSpacing: AppSpacing.gutter,
        childAspectRatio: 0.95,
      ),
      itemCount: 6,
      itemBuilder: (context, index) => const LoadingSkeleton.card(),
    );
  }
}
