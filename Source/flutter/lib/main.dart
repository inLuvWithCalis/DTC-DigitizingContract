import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'pages/login_page.dart';
import 'pages/catalog/services/service_list_page.dart';
import 'pages/catalog/service-types/service_type_list_page.dart';
import 'pages/dashboard_page.dart';
import 'services/api_client.dart';
import 'theme/app_theme.dart';
import 'utils/auth_store.dart';
import 'utils/theme_store.dart';


final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  try {
    await dotenv.load(fileName: ".env");
  } catch (_) {}
  await ApiClient().init();
  await ThemeStore.initTheme(themeModeNotifier);

  bool isRedirecting = false;
  ApiClient.onUnauthorized = () {
    if (isRedirecting) return;
    if (navigatorKey.currentContext != null) {
      final currentRoute = ModalRoute.of(
        navigatorKey.currentContext!,
      )?.settings.name;
      if (currentRoute == '/') return; // đã ở LoginPage rồi, khỏi redirect nữa
    }

    isRedirecting = true;
    AuthStore().clear();
    navigatorKey.currentState?.pushNamedAndRemoveUntil(
      '/',
      (route) => false,
      arguments: 'session_expired',
    );
    WidgetsBinding.instance.addPostFrameCallback((_) {
      isRedirecting = false;
    });
  };

  runApp(const MyApp());
}

final ValueNotifier<ThemeMode> themeModeNotifier = ValueNotifier<ThemeMode>(
  ThemeMode.light,
);

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<ThemeMode>(
      valueListenable: themeModeNotifier,
      builder: (context, currentMode, child) {
        return MaterialApp(
          navigatorKey: navigatorKey,
          title: 'Digitizing Contract',
          debugShowCheckedModeBanner: false,
          theme: AppTheme.lightTheme,
          darkTheme: AppTheme.darkTheme,
          themeMode: currentMode,
          initialRoute: '/',
          onGenerateRoute: (settings) {
            if (settings.name == '/') {
              final errorParam = settings.arguments as String?;
              return MaterialPageRoute(
                builder: (context) => LoginPage(errorParam: errorParam),
              );
            }
            if (settings.name == '/dashboard') {
              return MaterialPageRoute(
                builder: (context) => const DashboardPage(),
              );
            }
            if (settings.name == '/catalog/service-types') {
              return MaterialPageRoute(
                builder: (context) => const ServiceTypeListPage(),
              );
            }
            if (settings.name == '/catalog/services') {
              return MaterialPageRoute(
                builder: (context) => const ServiceListPage(),
              );
            }
            return null;
          },
        );
      },
    );
  }
}
