import '../models/auth_dto.dart';

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
