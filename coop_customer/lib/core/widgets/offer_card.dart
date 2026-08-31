import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../utils/currency_formatter.dart';

/// The product/offer card repeated across the home feed, search results,
/// category browsing and merchant storefront screens.
///
/// Takes primitive fields rather than an Offer model so it stays usable
/// before the marketplace models exist (Phase 3) and reusable across any
/// screen that lists offers.
class OfferCard extends StatelessWidget {
  const OfferCard({
    super.key,
    required this.title,
    required this.discountedPrice,
    this.imageUrl,
    this.originalPrice,
    this.discountPercentage,
    this.badgeLabel,
    this.isFavorite = false,
    this.onTap,
    this.onFavoriteToggle,
  });

  final String title;
  final double discountedPrice;
  final String? imageUrl;
  final double? originalPrice;
  final int? discountPercentage;
  final String? badgeLabel;
  final bool isFavorite;
  final VoidCallback? onTap;
  final VoidCallback? onFavoriteToggle;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    final badge = badgeLabel ?? (discountPercentage != null ? '-$discountPercentage%' : null);

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(AppRadius.lg),
      child: Container(
        padding: const EdgeInsets.all(AppSpacing.stackMd),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(AppRadius.lg),
          boxShadow: const [BoxShadow(color: AppColors.cardShadow, blurRadius: 20, offset: Offset(0, 4))],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            AspectRatio(
              aspectRatio: 1,
              child: Stack(
                children: [
                  Positioned.fill(
                    child: Container(
                      decoration: BoxDecoration(
                        color: AppColors.background,
                        borderRadius: BorderRadius.circular(AppRadius.md),
                        border: Border.all(color: AppColors.outlineVariant),
                      ),
                      clipBehavior: Clip.antiAlias,
                      child: imageUrl != null
                          ? Image.network(
                              imageUrl!,
                              fit: BoxFit.cover,
                              errorBuilder: (_, _, _) => const _ImagePlaceholder(),
                            )
                          : const _ImagePlaceholder(),
                    ),
                  ),
                  if (badge != null)
                    Positioned(
                      top: 8,
                      right: 8,
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(
                          color: AppColors.secondaryContainer,
                          borderRadius: BorderRadius.circular(AppRadius.full),
                        ),
                        child: Text(
                          badge,
                          style: textTheme.labelLarge?.copyWith(
                            fontSize: 10,
                            color: AppColors.onSecondaryContainer,
                          ),
                        ),
                      ),
                    ),
                  if (onFavoriteToggle != null)
                    Positioned(
                      top: 4,
                      left: 4,
                      child: IconButton(
                        onPressed: onFavoriteToggle,
                        icon: Icon(
                          isFavorite ? Icons.favorite_rounded : Icons.favorite_border_rounded,
                          color: isFavorite ? AppColors.primary : AppColors.onSurfaceVariant,
                        ),
                        style: IconButton.styleFrom(
                          backgroundColor: AppColors.surface.withValues(alpha: 0.85),
                        ),
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.stackSm),
            Text(
              title,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: textTheme.bodySmall?.copyWith(
                color: AppColors.onSurface,
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: AppSpacing.stackSm),
            Row(
              crossAxisAlignment: CrossAxisAlignment.baseline,
              textBaseline: TextBaseline.alphabetic,
              children: [
                Text(
                  CurrencyFormatter.format(discountedPrice),
                  style: textTheme.headlineSmall?.copyWith(color: AppColors.primary),
                ),
                if (originalPrice != null) ...[
                  const SizedBox(width: 8),
                  Text(
                    CurrencyFormatter.format(originalPrice!),
                    style: textTheme.bodySmall?.copyWith(
                      decoration: TextDecoration.lineThrough,
                    ),
                  ),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _ImagePlaceholder extends StatelessWidget {
  const _ImagePlaceholder();

  @override
  Widget build(BuildContext context) {
    return const ColoredBox(
      color: AppColors.background,
      child: Center(
        child: Icon(Icons.image_outlined, color: AppColors.outlineVariant, size: 32),
      ),
    );
  }
}
