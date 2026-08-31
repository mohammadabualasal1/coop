import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../providers/auth_provider.dart';

class _OnboardingSlide {
  const _OnboardingSlide({required this.icon, required this.title, required this.description});

  final IconData icon;
  final String title;
  final String description;
}

const _slides = [
  _OnboardingSlide(
    icon: Icons.sell_rounded,
    title: 'اكتشف أفضل الخصومات',
    description: 'ابحث عن المنتجات المخفضة من المتاجر القريبة منك واستمتع بتجربة تسوق مميزة بأسعار تنافسية.',
  ),
  _OnboardingSlide(
    icon: Icons.storefront_rounded,
    title: 'كل المتاجر في مكان واحد',
    description: 'تسوق من مختلف التصنيفات، من البقالة إلى الإلكترونيات، كل ما تحتاجه متاح في تطبيق واحد.',
  ),
  _OnboardingSlide(
    icon: Icons.local_shipping_rounded,
    title: 'اطلب وتابع توصيلك',
    description: 'أتمم طلبك بسهولة وتابع مسار السائق مباشرة حتى يصل طلبك إلى باب منزلك بكل أمان.',
  ),
];

class OnboardingScreen extends ConsumerStatefulWidget {
  const OnboardingScreen({super.key});

  @override
  ConsumerState<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends ConsumerState<OnboardingScreen> {
  final _pageController = PageController();
  int _index = 0;

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  bool get _isLastSlide => _index == _slides.length - 1;

  void _next() {
    if (_isLastSlide) {
      ref.read(authControllerProvider.notifier).markOnboardingSeen();
      return;
    }
    _pageController.nextPage(duration: const Duration(milliseconds: 300), curve: Curves.easeOut);
  }

  void _skip() {
    ref.read(authControllerProvider.notifier).markOnboardingSeen();
  }

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.marginMobile),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'COOP',
                    style: textTheme.headlineMedium?.copyWith(
                      color: AppColors.primary,
                      fontStyle: FontStyle.italic,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  TextButton(onPressed: _skip, child: const Text('تخطي')),
                ],
              ),
            ),
            Expanded(
              child: PageView.builder(
                controller: _pageController,
                itemCount: _slides.length,
                onPageChanged: (i) => setState(() => _index = i),
                itemBuilder: (context, i) => _SlideView(slide: _slides[i]),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.marginMobile,
                0,
                AppSpacing.marginMobile,
                AppSpacing.marginMobile,
              ),
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      for (var i = 0; i < _slides.length; i++)
                        AnimatedContainer(
                          duration: const Duration(milliseconds: 300),
                          margin: const EdgeInsets.symmetric(horizontal: 4),
                          width: i == _index ? 24 : 8,
                          height: 8,
                          decoration: BoxDecoration(
                            color: i == _index
                                ? AppColors.primary
                                : AppColors.secondaryContainer,
                            borderRadius: BorderRadius.circular(AppRadius.full),
                          ),
                        ),
                    ],
                  ),
                  const SizedBox(height: AppSpacing.stackMd),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      onPressed: _next,
                      child: Text(_isLastSlide ? 'ابدأ الآن' : 'التالي'),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SlideView extends StatelessWidget {
  const _SlideView({required this.slide});

  final _OnboardingSlide slide;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.marginMobile),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 220,
            height: 220,
            decoration: const BoxDecoration(
              color: AppColors.secondaryContainer,
              shape: BoxShape.circle,
            ),
            child: Icon(slide.icon, size: 96, color: AppColors.primary),
          ),
          const SizedBox(height: AppSpacing.stackLg),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(AppSpacing.stackMd),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(AppRadius.xl),
              boxShadow: const [
                BoxShadow(color: AppColors.cardShadow, blurRadius: 20, offset: Offset(0, 4)),
              ],
            ),
            child: Column(
              children: [
                Text(
                  slide.title,
                  style: textTheme.headlineMedium?.copyWith(color: AppColors.primary),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.stackSm),
                Text(
                  slide.description,
                  style: textTheme.bodyMedium?.copyWith(color: AppColors.onSurfaceVariant),
                  textAlign: TextAlign.center,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
