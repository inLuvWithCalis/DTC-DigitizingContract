import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'models/auth_dto.dart';
import 'pages/catalog/services/service_list_page.dart';
import 'pages/catalog/service-types/service_type_list_page.dart';
import 'pages/dashboard_page.dart';
import 'services/api_client.dart';
import 'services/auth_api.dart';
import 'theme/app_theme.dart';
import 'utils/app_toast.dart';
import 'widgets/app_text_field.dart';

class AuthStore {
  static final AuthStore _instance = AuthStore._internal();
  factory AuthStore() => _instance;
  AuthStore._internal();

  bool isAuthenticated = false;
  UserProfileDto? user;

  void setUser(UserProfileDto userProfile) {
    user = userProfile;
    isAuthenticated = true;
  }

  void clear() {
    user = null;
    isAuthenticated = false;
  }
}

class LoginPage extends StatefulWidget {
  final String? errorParam; // Tương đương params.error trong Expo Router

  const LoginPage({super.key, this.errorParam});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  // State management
  bool _isLoading = true;
  bool _isSubmitting = false;
  bool _showPassword = false;

  final _tenantController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  final _tenantFocusNode = FocusNode();
  final _emailFocusNode = FocusNode();
  final _passwordFocusNode = FocusNode();

  String? _tenantError;
  String? _emailError;
  String? _passwordError;

  bool _tenantTouched = false;
  bool _emailTouched = false;
  bool _passwordTouched = false;

  // Giả lập Auth Store
  final AuthStore _authStore = AuthStore();

  @override
  void initState() {
    super.initState();

    // Lắng nghe sự kiện Blur (khi user chuyển focus khỏi input)
    _tenantFocusNode.addListener(() {
      if (!_tenantFocusNode.hasFocus) {
        setState(() {
          _tenantTouched = true;
          _tenantError = _validateTenantCode(_tenantController.text);
        });
      }
    });

    _emailFocusNode.addListener(() {
      if (!_emailFocusNode.hasFocus) {
        setState(() {
          _emailTouched = true;
          _emailError = _validateEmail(_emailController.text);
        });
      }
    });

    _passwordFocusNode.addListener(() {
      if (!_passwordFocusNode.hasFocus) {
        setState(() {
          _passwordTouched = true;
          _passwordError = _validatePassword(_passwordController.text);
        });
      }
    });

    // Check Toast error param & Check đã đăng nhập chưa
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _checkSessionError();
      _checkAlreadyLoggedIn();
    });
  }

  @override
  void dispose() {
    _tenantController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _tenantFocusNode.dispose();
    _emailFocusNode.dispose();
    _passwordFocusNode.dispose();
    super.dispose();
  }

  // --- VALIDATORS ---
  String? _validateTenantCode(String value) {
    if (value.trim().isEmpty) return "Vui lòng nhập Mã công ty";
    return null;
  }

  String? _validateEmail(String value) {
    if (value.trim().isEmpty) return "Vui lòng nhập tên tài khoản";
    return null;
  }

  String? _validatePassword(String value) {
    if (value.isEmpty) return "Vui lòng nhập mật khẩu";
    return null;
  }

  // --- EFFECTS & LOGIC ---
  void _checkSessionError() {
    if (widget.errorParam == "session_expired") {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) {
          AppToast.error(
            context,
            "Phiên đăng nhập đã hết hạn hoặc không tồn tại, vui lòng đăng nhập lại.",
          );
        }
      });
    }
  }

  Future<void> _checkAlreadyLoggedIn() async {
    // Nếu vừa bị đá ra do session hết hạn, đừng thử gọi lại getMe()
    if (widget.errorParam == "session_expired") {
      setState(() => _isLoading = false);
      return;
    }

    if (_authStore.isAuthenticated && _authStore.user != null) {
      _navigateToDashboard();
      return;
    }

    try {
      final userData = await AuthApi.getMe();
      _authStore.setUser(userData);
      if (mounted) {
        _navigateToDashboard();
      }
    } catch (e) {
      _authStore.clear();
      if (mounted) {
        setState(() => _isLoading = false);
      }
      ApiClient().clearCookies();
    }
  }

  Future<void> _handleEmailSignIn() async {
    final tenantErr = _validateTenantCode(_tenantController.text);
    final emailErr = _validateEmail(_emailController.text);
    final passwordErr = _validatePassword(_passwordController.text);

    setState(() {
      _tenantTouched = true;
      _emailTouched = true;
      _passwordTouched = true;
      _tenantError = tenantErr;
      _emailError = emailErr;
      _passwordError = passwordErr;
    });

    if (tenantErr != null || emailErr != null || passwordErr != null) {
      AppToast.error(context, tenantErr ?? emailErr ?? passwordErr!);
      return;
    }

    setState(() => _isSubmitting = true);

    try {
      final loginResult = await AuthApi.login(
        _tenantController.text.trim(),
        accountName: _emailController.text.trim(),
        password: _passwordController.text,
      );

      final userData = await AuthApi.getMe();
      _authStore.setUser(userData);

      if (mounted) {
        AppToast.show(
          context,
          loginResult.message.isNotEmpty
              ? loginResult.message
              : "Đăng nhập thành công!",
        );
        _navigateToDashboard();
      }
    } catch (error) {
      if (mounted) {
        final msg = error.toString().replaceAll(RegExp(r'^Exception:\s*'), '');
        AppToast.error(context, msg);
      }
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  void _navigateToDashboard() {
    Navigator.of(context).pushReplacementNamed('/dashboard');
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    // Render Loading State ban đầu
    if (_isLoading) {
      return Scaffold(
        backgroundColor: theme.colorScheme.surface,
        body: Center(
          child: SizedBox(
            width: 32,
            height: 32,
            child: CircularProgressIndicator(
              strokeWidth: 3,
              color: theme.colorScheme.primary,
            ),
          ),
        ),
      );
    }

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      body: GestureDetector(
        // TouchableWithoutFeedback + Keyboard.dismiss
        onTap: () => FocusScope.of(context).unfocus(),
        child: SafeArea(
          child: CustomScrollView(
            // ScrollView contentContainerStyle={{ flexGrow: 1 }}
            slivers: [
              SliverFillRemaining(
                hasScrollBody: false,
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 24,
                    vertical: 16,
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // --- HEADER SECTION ---
                      const SizedBox(height: 16),
                      Container(
                        width: 56,
                        height: 56,
                        decoration: BoxDecoration(
                          color: theme.colorScheme.primary,
                          borderRadius: BorderRadius.circular(16),
                          boxShadow: [
                            BoxShadow(
                              color: theme.colorScheme.primary.withValues(
                                alpha: 0.3,
                              ),
                              blurRadius: 12,
                              offset: const Offset(0, 4),
                            ),
                          ],
                        ),
                        child: const Icon(
                          Icons.description_outlined, // FileText Icon
                          color: Colors.white,
                          size: 28,
                        ),
                      ),
                      const SizedBox(height: 24),
                      Text(
                        'Chào mừng trở lại',
                        style: theme.textTheme.headlineMedium?.copyWith(
                          fontWeight: FontWeight.w800,
                          color: theme.colorScheme.onSurface,
                          letterSpacing: -0.5,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Đăng nhập vào eContract để tiếp tục công việc của bạn.',
                        style: TextStyle(
                          fontSize: 16,
                          color: theme.colorScheme.onSurface.withValues(
                            alpha: 0.6,
                          ),
                        ),
                      ),
                      const SizedBox(height: 40),

                      // --- FORM SECTION ---
                      // Input 1: Mã công ty
                      _buildLabel('Mã công ty'),
                      const SizedBox(height: 8),
                      AppTextField(
                        controller: _tenantController,
                        focusNode: _tenantFocusNode,
                        maxLength: 50,
                        placeholder: 'Nhập mã công ty',
                        hasError: _tenantError != null && _tenantTouched,
                        onChanged: (val) {
                          if (_tenantTouched) {
                            setState(
                              () => _tenantError = _validateTenantCode(val),
                            );
                          }
                        },
                      ),
                      if (_tenantError != null && _tenantTouched)
                        _buildErrorText(_tenantError!),

                      const SizedBox(height: 16),

                      _buildLabel('Tên tài khoản'),
                      const SizedBox(height: 8),
                      AppTextField(
                        controller: _emailController,
                        focusNode: _emailFocusNode,
                        placeholder: 'Nhập tên đăng nhập',
                        hasError: _emailError != null && _emailTouched,
                        autocorrect: false,
                        enableSuggestions: false,
                        onChanged: (val) {
                          if (_emailTouched) {
                            setState(() => _emailError = _validateEmail(val));
                          }
                        },
                      ),
                      if (_emailError != null && _emailTouched)
                        _buildErrorText(_emailError!),

                      const SizedBox(height: 16),

                      // Input 3: Mật khẩu + Toggle Visibility
                      _buildLabel('Mật khẩu'),
                      const SizedBox(height: 8),
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            child: AppTextField(
                              controller: _passwordController,
                              focusNode: _passwordFocusNode,
                              obscureText: !_showPassword,
                              maxLength: 64,
                              placeholder: '••••••••',
                              hasError:
                                  _passwordError != null && _passwordTouched,
                              onChanged: (val) {
                                if (_passwordTouched) {
                                  setState(
                                    () =>
                                        _passwordError = _validatePassword(val),
                                  );
                                }
                              },
                            ),
                          ),
                          const SizedBox(width: 12),
                          InkWell(
                            onTap: () =>
                                setState(() => _showPassword = !_showPassword),
                            borderRadius: BorderRadius.circular(16),
                            child: Container(
                              height: 56,
                              width: 56,
                              decoration: BoxDecoration(
                                color: theme.colorScheme.surfaceContainerHighest
                                    .withValues(alpha: 0.3),
                                borderRadius: BorderRadius.circular(16),
                              ),
                              child: Icon(
                                _showPassword
                                    ? Icons.visibility_off_outlined
                                    : Icons.visibility_outlined,
                                color: theme.colorScheme.onSurfaceVariant,
                                size: 22,
                              ),
                            ),
                          ),
                        ],
                      ),
                      if (_passwordError != null && _passwordTouched)
                        _buildErrorText(_passwordError!)
                      else
                        Align(
                          alignment: Alignment.centerRight,
                          child: TextButton(
                            onPressed: () {},
                            child: Text(
                              'Quên mật khẩu?',
                              style: TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.bold,
                                color: theme.colorScheme.primary,
                              ),
                            ),
                          ),
                        ),

                      const SizedBox(height: 32),

                      // --- BUTTON SECTION ---
                      SizedBox(
                        width: double.infinity,
                        height: 56,
                        child: ElevatedButton(
                          onPressed: _isSubmitting ? null : _handleEmailSignIn,
                          style: ElevatedButton.styleFrom(
                            backgroundColor: theme.colorScheme.primary,
                            foregroundColor: theme.colorScheme.onPrimary,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(16),
                            ),
                            elevation: 2,
                          ),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              if (_isSubmitting) ...[
                                SizedBox(
                                  width: 18,
                                  height: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: theme.colorScheme.onPrimary,
                                  ),
                                ),
                                const SizedBox(width: 12),
                              ],
                              Text(
                                _isSubmitting ? "Đang xử lý..." : "Đăng nhập",
                                style: const TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),

                      const Spacer(),

                      // --- FOOTER SECTION ---
                      Padding(
                        padding: const EdgeInsets.only(top: 24, bottom: 8),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(
                              Icons.verified_user_outlined,
                              size: 18,
                              color: Colors.green.shade600,
                            ),
                            const SizedBox(width: 8),
                            Text(
                              'Bảo mật cấp doanh nghiệp',
                              style: TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.w500,
                                color: theme.colorScheme.onSurface.withValues(
                                  alpha: 0.6,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  // --- HELPER WIDGETS & STYLES ---
  Widget _buildLabel(String label) {
    return Text(
      label,
      style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
    );
  }

  Widget _buildErrorText(String text) {
    return Padding(
      padding: const EdgeInsets.only(top: 6, left: 4),
      child: Text(
        text,
        style: TextStyle(
          color: Theme.of(context).colorScheme.error,
          fontSize: 14,
          fontWeight: FontWeight.w500,
        ),
      ),
    );
  }
}

final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  try {
    await dotenv.load(fileName: ".env");
  } catch (_) {}
  await ApiClient().init();

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

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      navigatorKey: navigatorKey,
      title: 'Digitizing Contract',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      darkTheme: AppTheme.darkTheme,
      themeMode: ThemeMode.system,
      initialRoute: '/',
      onGenerateRoute: (settings) {
        if (settings.name == '/') {
          final errorParam = settings.arguments as String?;
          return MaterialPageRoute(
            builder: (context) => LoginPage(errorParam: errorParam),
          );
        }
        if (settings.name == '/dashboard') {
          return MaterialPageRoute(builder: (context) => const DashboardPage());
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
  }
}
