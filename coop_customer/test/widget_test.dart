import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:coop_customer/app.dart';

void main() {
  testWidgets('App boots and renders the splash screen without crashing', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(const ProviderScope(child: CoopApp()));
    await tester.pump();

    // Full auth-bootstrap -> redirect flow depends on the secure-storage
    // and shared-preferences platform channels, which don't reply in a
    // bare `flutter test` host — that path is verified on-device instead.
    // This just guards against the app failing to boot at all.
    expect(find.text('COOP'), findsOneWidget);
  });
}
