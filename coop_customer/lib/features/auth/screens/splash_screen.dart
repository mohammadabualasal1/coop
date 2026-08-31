import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';

/// Purely presentational — the router redirect (driven by
/// [authControllerProvider]) sends the user to home/onboarding/login as
/// soon as the session check resolves, so this screen has no navigation
/// logic of its own.
///
/// No brand logo asset exists yet, so this reuses the italic Montserrat
/// "COOP" wordmark treatment the design export itself falls back to on
/// the onboarding and OTP screens (in place of the raster logo used only
/// on splash/login in the mockups).
class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.marginMobile),
          child: Column(
            children: [
              const Spacer(),
              Text(
                'COOP',
                style: textTheme.displayLarge?.copyWith(
                  color: AppColors.primary,
                  fontStyle: FontStyle.italic,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: AppSpacing.stackLg),
              const _LoadingBar(),
              const Spacer(),
              Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.stackLg),
                child: Text(
                  'عروض أكثر، في مكان واحد',
                  style: textTheme.headlineMedium?.copyWith(
                    color: AppColors.onSurfaceVariant,
                  ),
                  textAlign: TextAlign.center,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _LoadingBar extends StatefulWidget {
  const _LoadingBar();

  @override
  State<_LoadingBar> createState() => _LoadingBarState();
}

class _LoadingBarState extends State<_LoadingBar> with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1200),
  )..repeat();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 120,
      height: 4,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(AppRadius.full),
        child: ColoredBox(
          color: AppColors.outlineVariant,
          child: AnimatedBuilder(
            animation: _controller,
            builder: (context, child) {
              final t = _controller.value;
              return Align(
                alignment: Alignment(t * 4 - 1.8, 0),
                child: FractionallySizedBox(widthFactor: 0.4, child: child),
              );
            },
            child: const ColoredBox(color: AppColors.primary),
          ),
        ),
      ),
    );
  }
}
