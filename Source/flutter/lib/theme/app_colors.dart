import 'package:flutter/material.dart';

/// Bảng màu Design Tokens tương xứng 1:1 với globals.css của Web Frontend
abstract class AppColors {
  // --- LIGHT THEME (khai báo tương ứng với :root trong globals.css) ---
  static const Color lightBackground = Color(0xFFF8F8F8);
  static const Color lightForeground = Color(0xFF161616);
  static const Color lightCard = Color(0xFFFFFFFF);
  static const Color lightCardForeground = Color(0xFF161616);

  static const Color lightPrimary = Color(
    0xFF005398,
  ); // Deep Blue (từ --primary)
  static const Color lightPrimaryForeground = Color(0xFFF8F8F8);

  static const Color lightSecondary = Color(0xFFEBF3F6);
  static const Color lightSecondaryForeground = Color(0xFF161616);

  static const Color lightMuted = Color(0xFFD1D9DC);
  static const Color lightMutedForeground = Color(0xFF636363);

  static const Color lightAccent = Color(0xFFEBEBEB);
  static const Color lightAccentForeground = Color(0xFF222222);

  static const Color lightDestructive = Color(0xFFE7000B);
  static const Color lightDestructiveForeground = Color(0xFFF8F8F8);

  static const Color lightBorder = Color(0xFFDEE6E9);
  static const Color lightInput = Color(0xFFF5F5F5);
  static const Color lightRing = Color(0xFF005398);

  // Chart colors (theo --chart-1..5 trong globals.css)
  static const Color chart1 = Color(0xFFF54900);
  static const Color chart2 = Color(0xFF009689);
  static const Color chart3 = Color(0xFF104E64);
  static const Color chart4 = Color(0xFFFFB900);
  static const Color chart5 = Color(0xFFFE9A00);

  // Status colors (giữ nguyên, không có trong globals.css — dùng chung Web/Flutter)
  static const Color statusSuccess = Color(0xFF059669); // Emerald
  static const Color statusWarning = Color(0xFFD97706); // Amber
  static const Color statusError = Color(0xFFE11D48); // Rose
  static const Color statusInfo = Color(0xFF0284C7); // Sky

  // --- DARK THEME (khai báo tương ứng với .dark trong globals.css) ---
  static const Color darkBackground = Color(0xFF070B0E);
  static const Color darkForeground = Color(0xFFE5E8EB);
  static const Color darkCard = Color(0xFF0D1216);
  static const Color darkCardForeground = Color(0xFFE5E8EB);

  static const Color darkPrimary = Color(0xFF009AEE);
  static const Color darkPrimaryForeground = Color(0xFFF8F8F8);

  static const Color darkSecondary = Color(0xFF141C21);
  static const Color darkSecondaryForeground = Color(0xFFDBDEE1);

  static const Color darkMuted = Color(0xFF1B2328);
  static const Color darkMutedForeground = Color(0xFF8A9095);

  static const Color darkAccent = Color(0xFF19232A);
  static const Color darkAccentForeground = Color(0xFFE9F0F4);

  static const Color darkDestructive = Color(0xFFCC2827);
  static const Color darkDestructiveForeground = Color(0xFFF8F8F8);

  static const Color darkBorder = Color(0xFF222A2F);
  static const Color darkInput = Color(0xFF151C20);
  static const Color darkRing = Color(0xFF009AEE);

  // Dark chart colors (theo .dark --chart-1..5)
  static const Color darkChart1 = Color(0xFF009AEE);
  static const Color darkChart2 = Color(0xFF00BC7D);
  static const Color darkChart3 = Color(0xFFFE9A00);
  static const Color darkChart4 = Color(0xFFAD46FF);
  static const Color darkChart5 = Color(0xFFFF2056);
}
