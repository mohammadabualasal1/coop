import 'package:intl/intl.dart';

/// Formats amounts as Jordanian Dinar: two decimals, Western digits,
/// followed by the د.أ symbol — matching the design export exactly
/// (e.g. "92.00 د.أ"). Bidi layout (via the app-wide RTL Directionality)
/// places it correctly on screen; no manual reordering needed here.
abstract final class CurrencyFormatter {
  static final _numberFormat = NumberFormat('#,##0.00', 'en_US');

  static String format(num amount) => '${_numberFormat.format(amount)} د.أ';
}
