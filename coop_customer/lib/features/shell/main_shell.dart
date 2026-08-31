import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Bottom-nav scaffold shared by the five main tabs. Wraps a
/// [StatefulShellRoute] so each tab keeps its own navigation stack and
/// scroll position when switching away and back.
class MainShell extends StatelessWidget {
  const MainShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  static const _destinations = [
    (icon: Icons.home_rounded, label: 'الرئيسية'),
    (icon: Icons.grid_view_rounded, label: 'التصنيفات'),
    (icon: Icons.receipt_long_rounded, label: 'الطلبات'),
    (icon: Icons.favorite_rounded, label: 'المفضلة'),
    (icon: Icons.person_rounded, label: 'حسابي'),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: navigationShell,
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: navigationShell.currentIndex,
        onTap: (index) => navigationShell.goBranch(
          index,
          initialLocation: index == navigationShell.currentIndex,
        ),
        items: [
          for (final d in _destinations)
            BottomNavigationBarItem(icon: Icon(d.icon), label: d.label),
        ],
      ),
    );
  }
}
