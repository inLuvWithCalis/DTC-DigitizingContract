// This is a basic Flutter widget test.
//
// To perform an interaction with a widget in your test, use the WidgetTester
// utility in the flutter_test package. For example, you can send tap and scroll
// gestures. You can also use WidgetTester to find child widgets in the widget
// tree, read text, and verify that the values of widget properties are correct.

import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:dtc_digitizingcontract/main.dart';

void main() {
  setUp(() async {
    await dotenv.load(fileName: '.env', isOptional: true);
  });

  testWidgets('LoginPage smoke test', (WidgetTester tester) async {
    // Build our app and trigger a frame.
    await tester.pumpWidget(const MyApp());
    await tester.pumpAndSettle();

    // Verify that the login page renders
    expect(find.text('Đăng nhập'), findsWidgets);
  });
}
