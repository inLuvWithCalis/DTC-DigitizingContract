import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../services/api_client.dart';
import '../services/auth_api.dart';
import '../utils/app_toast.dart';
import '../utils/auth_store.dart';
import '../widgets/app_text_field.dart';

class LoginPage extends StatefulWidget {
  final String? errorParam;

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

  final AuthStore _authStore = AuthStore();

  @override
  void initState() {
    super.initState();

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

  Future<void> _handleEmailSignIn(StateSetter bottomSheetSetState) async {
    final tenantErr = _validateTenantCode(_tenantController.text);
    final emailErr = _validateEmail(_emailController.text);
    final passwordErr = _validatePassword(_passwordController.text);

    bottomSheetSetState(() {
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

    TextInput.finishAutofillContext();
    bottomSheetSetState(() => _isSubmitting = true);

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
        Navigator.pop(context); // Close bottom sheet
        _navigateToDashboard();
      }
    } catch (error) {
      if (mounted) {
        final msg = error.toString().replaceAll(RegExp(r'^Exception:\s*'), '');
        AppToast.error(context, msg);
      }
    } finally {
      if (mounted) {
        bottomSheetSetState(() => _isSubmitting = false);
      }
    }
  }

  void _navigateToDashboard() {
    Navigator.of(context).pushReplacementNamed('/dashboard');
  }

  void _showLoginBottomSheet() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setBottomSheetState) {
            final theme = Theme.of(context);
            final bottomInset = MediaQuery.of(context).viewInsets.bottom;

            return GestureDetector(
              onTap: () => FocusScope.of(context).unfocus(),
              child: Container(
                padding: EdgeInsets.only(
                  bottom: bottomInset > 0
                      ? bottomInset
                      : MediaQuery.of(context).padding.bottom,
                ),
                decoration: BoxDecoration(
                  color: theme.colorScheme.surface,
                  borderRadius: const BorderRadius.vertical(
                    top: Radius.circular(24),
                  ),
                ),
                child: SingleChildScrollView(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 24,
                      vertical: 24,
                    ),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        // Header
                        Center(
                          child: Container(
                            width: 40,
                            height: 4,
                            decoration: BoxDecoration(
                              color: theme.colorScheme.onSurfaceVariant
                                  .withValues(alpha: 0.4),
                              borderRadius: BorderRadius.circular(2),
                            ),
                          ),
                        ),
                        const SizedBox(height: 24),
                        Text(
                          'Đăng nhập',
                          style: theme.textTheme.headlineSmall?.copyWith(
                            fontWeight: FontWeight.bold,
                            color: theme.colorScheme.onSurface,
                          ),
                        ),
                        const SizedBox(height: 32),

                        // Form
                        AutofillGroup(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              // Tenant Code
                              _buildLabel('Mã công ty'),
                              const SizedBox(height: 8),
                              AppTextField(
                                controller: _tenantController,
                                focusNode: _tenantFocusNode,
                                maxLength: 50,
                                placeholder: 'Nhập mã công ty',
                                hasError:
                                    _tenantError != null && _tenantTouched,
                                autofillHints: const [
                                  AutofillHints.organizationName,
                                ],
                                onChanged: (val) {
                                  if (_tenantTouched) {
                                    setBottomSheetState(
                                      () => _tenantError = _validateTenantCode(
                                        val,
                                      ),
                                    );
                                  }
                                },
                              ),
                              if (_tenantError != null && _tenantTouched)
                                _buildErrorText(_tenantError!),

                              const SizedBox(height: 16),

                              // Email/Username
                              _buildLabel('Tên tài khoản'),
                              const SizedBox(height: 8),
                              AppTextField(
                                controller: _emailController,
                                focusNode: _emailFocusNode,
                                placeholder: 'Nhập tên đăng nhập',
                                hasError: _emailError != null && _emailTouched,
                                autocorrect: false,
                                enableSuggestions: false,
                                autofillHints: const [
                                  AutofillHints.username,
                                  AutofillHints.email,
                                ],
                                onChanged: (val) {
                                  if (_emailTouched) {
                                    setBottomSheetState(
                                      () => _emailError = _validateEmail(val),
                                    );
                                  }
                                },
                              ),
                              if (_emailError != null && _emailTouched)
                                _buildErrorText(_emailError!),

                              const SizedBox(height: 16),

                              // Password
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
                                          _passwordError != null &&
                                          _passwordTouched,
                                      textInputAction: TextInputAction.done,
                                      autofillHints: const [
                                        AutofillHints.password,
                                      ],
                                      onSubmitted: (_) => _handleEmailSignIn(
                                        setBottomSheetState,
                                      ),
                                      onChanged: (val) {
                                        if (_passwordTouched) {
                                          setBottomSheetState(
                                            () => _passwordError =
                                                _validatePassword(val),
                                          );
                                        }
                                      },
                                    ),
                                  ),
                                  const SizedBox(width: 12),
                                  InkWell(
                                    onTap: () => setBottomSheetState(
                                      () => _showPassword = !_showPassword,
                                    ),
                                    borderRadius: BorderRadius.circular(16),
                                    child: Container(
                                      height: 56,
                                      width: 56,
                                      decoration: BoxDecoration(
                                        color: theme
                                            .colorScheme
                                            .surfaceContainerHighest
                                            .withValues(alpha: 0.3),
                                        borderRadius: BorderRadius.circular(16),
                                      ),
                                      child: Icon(
                                        _showPassword
                                            ? Icons.visibility_off_outlined
                                            : Icons.visibility_outlined,
                                        color:
                                            theme.colorScheme.onSurfaceVariant,
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
                            ],
                          ),
                        ),

                        const SizedBox(height: 24),

                        // Submit Button
                        SizedBox(
                          width: double.infinity,
                          height: 56,
                          child: ElevatedButton(
                            onPressed: _isSubmitting
                                ? null
                                : () => _handleEmailSignIn(setBottomSheetState),
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

                        const SizedBox(height: 16),
                      ],
                    ),
                  ),
                ),
              ),
            );
          },
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      body: SafeArea(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Spacer(),
            // Logo Placeholder
            Center(
              child: Image.asset(
                'assets/images/logo.png',
                width: 120,
                height: 120,
                fit: BoxFit.contain,
              ),
            ),
            const SizedBox(height: 32),
            Text(
              'eContract',
              style: theme.textTheme.headlineMedium?.copyWith(
                fontWeight: FontWeight.w800,
                color: theme.colorScheme.onSurface,
                letterSpacing: -0.5,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'Số hóa hợp đồng thông minh',
              style: TextStyle(
                fontSize: 16,
                color: theme.colorScheme.onSurface.withValues(alpha: 0.6),
              ),
            ),
            const Spacer(),

            // Bottom Action Area
            Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: 32.0,
                vertical: 48.0,
              ),
              child: _isLoading
                  ? Column(
                      children: [
                        SizedBox(
                          width: 200,
                          child: LinearProgressIndicator(
                            color: theme.colorScheme.primary,
                            backgroundColor:
                                theme.colorScheme.surfaceContainerHighest,
                            borderRadius: BorderRadius.circular(8),
                            minHeight: 6,
                          ),
                        ),
                        const SizedBox(height: 16),
                        Text(
                          'Đang tải dữ liệu...',
                          style: TextStyle(
                            color: theme.colorScheme.onSurface.withValues(
                              alpha: 0.6,
                            ),
                            fontSize: 14,
                          ),
                        ),
                      ],
                    )
                  : SizedBox(
                      width: double.infinity,
                      height: 56,
                      child: ElevatedButton(
                        onPressed: _showLoginBottomSheet,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: theme.colorScheme.primary,
                          foregroundColor: theme.colorScheme.onPrimary,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(16),
                          ),
                          elevation: 2,
                        ),
                        child: const Text(
                          "Đăng nhập ngay",
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ),
            ),
          ],
        ),
      ),
    );
  }

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
