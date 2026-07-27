import 'package:flutter/material.dart';

/// Reusable AppTextField component giúp tự động thiết lập `maxLength` mặc định (255 ký tự)
/// và ẩn bộ đếm ký tự (counterText) thừa mà không cần viết lại `maxLength` mỗi lần dùng TextField.
class AppTextField extends StatelessWidget {
  final TextEditingController? controller;
  final FocusNode? focusNode;
  final String? placeholder;
  final bool obscureText;
  final int? maxLength;
  final ValueChanged<String>? onChanged;
  final VoidCallback? onTap;
  final bool hasError;
  final bool autocorrect;
  final bool enableSuggestions;
  final Widget? suffixIcon;

  const AppTextField({
    super.key,
    this.controller,
    this.focusNode,
    this.placeholder,
    this.obscureText = false,
    this.maxLength = 255, // Default maxLength toàn hệ thống
    this.onChanged,
    this.onTap,
    this.hasError = false,
    this.autocorrect = true,
    this.enableSuggestions = true,
    this.suffixIcon,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return TextField(
      controller: controller,
      focusNode: focusNode,
      obscureText: obscureText,
      maxLength: maxLength,
      onChanged: onChanged,
      onTap: onTap,
      autocorrect: autocorrect,
      enableSuggestions: enableSuggestions,
      decoration: InputDecoration(
        hintText: placeholder,
        counterText: "", // Ẩn chữ đếm ký tự rườm rà mặc định
        filled: true,
        fillColor: hasError
            ? theme.colorScheme.error.withValues(alpha: 0.05)
            : theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
        contentPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
        suffixIcon: suffixIcon,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(16),
          borderSide: BorderSide.none,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(16),
          borderSide: hasError
              ? BorderSide(color: theme.colorScheme.error)
              : BorderSide.none,
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(16),
          borderSide: BorderSide(
            color: hasError ? theme.colorScheme.error : theme.colorScheme.primary,
            width: 1.5,
          ),
        ),
      ),
    );
  }
}
