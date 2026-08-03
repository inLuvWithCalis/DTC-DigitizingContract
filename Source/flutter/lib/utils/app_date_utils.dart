class AppDateUtils {
  /// Chuyển đổi DateTime sang chuỗi định dạng yyyy-MM-dd
  static String? formatToYMD(DateTime? date) {
    if (date == null) return null;
    return "${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}";
  }
}
