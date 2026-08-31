import 'package:flutter/material.dart';

/// Color tokens matching the Heritage Modern design export.
///
/// `primary` and `primaryContainer` intentionally diverge from the literal
/// hex values named in DESIGN.md — the pixel-matched screen exports use
/// #6C0029 for primary actions and #8B1E3F as a secondary/container tone.
abstract final class AppColors {
  static const primary = Color(0xFF6C0029);
  static const primaryContainer = Color(0xFF8B1E3F);
  static const onPrimary = Color(0xFFFFFFFF);
  static const onPrimaryContainer = Color(0xFFFF9DB0);

  static const secondary = Color(0xFF89502E);
  static const secondaryContainer = Color(0xFFFEB289); // Warm Peach accent
  static const onSecondary = Color(0xFFFFFFFF);
  static const onSecondaryContainer = Color(0xFF794222);

  static const tertiary = Color(0xFF62152C);
  static const tertiaryContainer = Color(0xFF802C42);
  static const onTertiary = Color(0xFFFFFFFF);
  static const onTertiaryContainer = Color(0xFFFF9DB1);

  static const background = Color(0xFFFFF9F5); // Warm Off-White
  static const onBackground = Color(0xFF1A1B1E);

  static const surface = Color(0xFFFFFFFF); // Pure White cards
  static const surfaceContainerLowest = Color(0xFFFFFFFF);
  static const surfaceContainerLow = Color(0xFFF4F3F7);
  static const surfaceContainer = Color(0xFFEFEDF1);
  static const surfaceContainerHigh = Color(0xFFE9E7EB);
  static const surfaceContainerHighest = Color(0xFFE3E2E6);
  static const onSurface = Color(0xFF1A1B1E);
  static const onSurfaceVariant = Color(0xFF564145);

  static const outline = Color(0xFF897174);
  static const outlineVariant = Color(0xFFE9E2DD); // input/card borders

  static const error = Color(0xFFBA1A1A);
  static const onError = Color(0xFFFFFFFF);
  static const errorContainer = Color(0xFFFFDAD6);
  static const onErrorContainer = Color(0xFF93000A);

  static const success = Color(0xFF2E7D32);
  static const onSuccess = Color(0xFFFFFFFF);

  /// Ambient shadow with a slight burgundy tint, per the design system.
  static const cardShadow = Color(0x0A8B1E3F); // rgba(139,30,63,0.04)
  static const cardShadowHover = Color(0x148B1E3F); // rgba(139,30,63,0.08)
}
