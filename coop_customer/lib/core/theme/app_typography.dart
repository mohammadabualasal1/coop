import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import 'app_colors.dart';

/// Text styles from the Heritage Modern type scale.
/// Montserrat for headlines/labels/buttons, Inter for body copy.
abstract final class AppTypography {
  static TextTheme textTheme(Color onSurface, Color onSurfaceVariant) {
    return TextTheme(
      displayLarge: GoogleFonts.montserrat(
        fontSize: 32,
        height: 40 / 32,
        fontWeight: FontWeight.w700,
        letterSpacing: -0.01 * 32,
        color: onSurface,
      ),
      headlineMedium: GoogleFonts.montserrat(
        fontSize: 24,
        height: 32 / 24,
        fontWeight: FontWeight.w600,
        color: onSurface,
      ),
      headlineSmall: GoogleFonts.montserrat(
        fontSize: 20,
        height: 28 / 20,
        fontWeight: FontWeight.w600,
        color: onSurface,
      ),
      labelLarge: GoogleFonts.montserrat(
        fontSize: 12,
        height: 16 / 12,
        fontWeight: FontWeight.w700,
        letterSpacing: 0.05 * 12,
        color: onSurfaceVariant,
      ),
      titleMedium: GoogleFonts.montserrat(
        fontSize: 16,
        height: 20 / 16,
        fontWeight: FontWeight.w600,
        color: onSurface,
      ),
      bodyLarge: GoogleFonts.inter(
        fontSize: 18,
        height: 28 / 18,
        fontWeight: FontWeight.w400,
        color: onSurface,
      ),
      bodyMedium: GoogleFonts.inter(
        fontSize: 16,
        height: 24 / 16,
        fontWeight: FontWeight.w400,
        color: onSurface,
      ),
      bodySmall: GoogleFonts.inter(
        fontSize: 14,
        height: 20 / 14,
        fontWeight: FontWeight.w400,
        color: onSurfaceVariant,
      ),
    );
  }

  static TextStyle button = GoogleFonts.montserrat(
    fontSize: 16,
    height: 20 / 16,
    fontWeight: FontWeight.w600,
  );

  static TextStyle labelCaps = GoogleFonts.montserrat(
    fontSize: 12,
    height: 16 / 12,
    fontWeight: FontWeight.w700,
    letterSpacing: 0.05 * 12,
    color: AppColors.onSurfaceVariant,
  );
}
