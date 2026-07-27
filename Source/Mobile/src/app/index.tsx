import { useState, useEffect } from "react";
import {
  View,
  ScrollView,
  Pressable,
  KeyboardAvoidingView,
  TouchableWithoutFeedback,
  Platform,
  Keyboard,
} from "react-native";
import { useRouter, useLocalSearchParams } from "expo-router";
import {
  Loader2,
  FileText,
  ShieldCheck,
  Eye,
  EyeOff,
} from "lucide-react-native";
import Toast from "react-native-toast-message";

import { authApi } from "@/services/auth-api";
import { useAuthStore } from "@/hooks/use-auth-store";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Text } from "@/components/ui/text";

export default function LoginPage() {
  const router = useRouter();
  const params = useLocalSearchParams<{ error?: string }>();

  const [isLoading, setIsLoading] = useState(true);
  const [tenantCode, setTenantCode] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const { user, isAuthenticated, setUser } = useAuthStore();

  const [errors, setErrors] = useState<{
    tenantCode?: string;
    email?: string;
    password?: string;
  }>({});
  const [touched, setTouched] = useState<{
    tenantCode?: boolean;
    email?: boolean;
    password?: boolean;
  }>({});

  const validateTenantCode = (value: string): string | undefined => {
    if (!value.trim()) return "Vui lòng nhập Mã công ty";
    return undefined;
  };

  const validateEmail = (value: string): string | undefined => {
    if (!value.trim()) return "Vui lòng nhập tên tài khoản";
    return undefined;
  };

  const validatePassword = (value: string): string | undefined => {
    if (!value) return "Vui lòng nhập mật khẩu";
    return undefined;
  };

  useEffect(() => {
    if (params.error === "session_expired") {
      const timer = setTimeout(() => {
        Toast.show({
          type: "error",
          text1: "Lỗi",
          text2:
            "Phiên đăng nhập đã hết hạn hoặc không tồn tại, vui lòng đăng nhập lại.",
        });
        router.setParams({ error: "" });
      }, 100);
      return () => clearTimeout(timer);
    }
  }, [params.error]);

  useEffect(() => {
    const checkAlreadyLoggedIn = async () => {
      if (isAuthenticated && user) {
        router.replace("/dashboard");
        return;
      }

      try {
        const userData = await authApi.getMe();
        setUser(userData);
        router.replace("/dashboard");
      } catch (error) {
        setIsLoading(false);
      }
    };

    checkAlreadyLoggedIn();
  }, [isAuthenticated, user, router, setUser]);

  const handleEmailChange = (value: string) => {
    setEmail(value);
    if (touched.email) {
      setErrors((prev) => ({ ...prev, email: validateEmail(value) }));
    }
  };

  const handlePasswordChange = (value: string) => {
    setPassword(value);
    if (touched.password) {
      setErrors((prev) => ({ ...prev, password: validatePassword(value) }));
    }
  };

  const handleTenantCodeChange = (value: string) => {
    setTenantCode(value);
    if (touched.tenantCode) {
      setErrors((prev) => ({ ...prev, tenantCode: validateTenantCode(value) }));
    }
  };

  const handleBlur = (field: "tenantCode" | "email" | "password") => {
    setTouched((prev) => ({ ...prev, [field]: true }));
    let value = "";
    let validator;

    if (field === "tenantCode") {
      value = tenantCode;
      validator = validateTenantCode;
    } else if (field === "email") {
      value = email;
      validator = validateEmail;
    } else {
      value = password;
      validator = validatePassword;
    }

    setErrors((prev) => ({ ...prev, [field]: validator(value) }));
  };

  const handleEmailSignIn = async () => {
    const tenantError = validateTenantCode(tenantCode);
    const emailError = validateEmail(email);
    const passwordError = validatePassword(password);

    setTouched({ tenantCode: true, email: true, password: true });
    setErrors({
      tenantCode: tenantError,
      email: emailError,
      password: passwordError,
    });

    if (tenantError || emailError || passwordError) {
      Toast.show({
        type: "error",
        text1: "Lỗi",
        text2: tenantError || emailError || passwordError,
      });
      return;
    }

    setIsLoading(true);

    try {
      const data = await authApi.login(
        {
          accountName: email,
          password,
        },
        tenantCode.trim(),
      );

      // Fetch user profile immediately after login to populate store
      const userData = await authApi.getMe();
      setUser(userData);

      Toast.show({
        type: "success",
        text1: "Thành công",
        text2: data.message || "Đăng nhập thành công!",
      });

      router.replace("/dashboard");
    } catch (error: any) {
      const message =
        error?.response?.data?.message ||
        error?.message ||
        "Đăng nhập thất bại, vui lòng thử lại.";
      Toast.show({
        type: "error",
        text1: "Lỗi",
        text2: message,
      });
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <View className="flex-1 bg-background items-center justify-center">
        <Loader2 className="text-primary animate-spin" size={32} />
      </View>
    );
  }

  return (
    <KeyboardAvoidingView
      behavior={Platform.OS === "ios" ? "padding" : "height"}
      className="flex-1 bg-background"
    >
      <TouchableWithoutFeedback onPress={Keyboard.dismiss}>
        <ScrollView
          contentContainerStyle={{ flexGrow: 1 }}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          <View className="flex-1 px-6 pt-16 pb-8">
            {/* --- HEADER SECTION --- */}
            <View className="mb-10 mt-4">
              <View className="w-14 h-14 bg-primary rounded-2xl items-center justify-center shadow-lg shadow-primary/30 mb-6">
                <FileText color="white" size={28} />
              </View>
              <Text className="text-3xl font-extrabold text-foreground tracking-tight mb-2">
                Chào mừng trở lại
              </Text>
              <Text className="text-base text-muted-foreground">
                Đăng nhập vào eContract để tiếp tục công việc của bạn.
              </Text>
            </View>

            {/* --- FORM SECTION --- */}
            <View className="space-y-6">
              <View className="space-y-2">
                <Label className="text-foreground/90 font-semibold ml-1 pt-2">
                  Mã công ty
                </Label>
                <Input
                  value={tenantCode}
                  maxLength={50}
                  onChangeText={handleTenantCodeChange}
                  onBlur={() => handleBlur("tenantCode")}
                  placeholder="Ví dụ: VNG_CORP"
                  className={`h-14 rounded-2xl px-5 text-base bg-muted/30 border-transparent focus:border-primary focus:bg-background ${
                    errors.tenantCode && touched.tenantCode
                      ? "border-destructive bg-destructive/5"
                      : ""
                  }`}
                />
                {errors.tenantCode && touched.tenantCode && (
                  <Text className="text-destructive text-sm mt-1 ml-1 font-medium">
                    {errors.tenantCode}
                  </Text>
                )}
              </View>

              <View className="space-y-2">
                <Label className="text-foreground/90 font-semibold ml-1 pt-2">
                  Tên tài khoản
                </Label>
                <Input
                  value={email}
                  maxLength={255}
                  onChangeText={handleEmailChange}
                  onBlur={() => handleBlur("email")}
                  placeholder="Nhập tên đăng nhập"
                  autoCapitalize="none"
                  className={`h-14 rounded-2xl px-5 text-base bg-muted/30 border-transparent focus:border-primary focus:bg-background ${
                    errors.email && touched.email
                      ? "border-destructive bg-destructive/5"
                      : ""
                  }`}
                />
                {errors.email && touched.email && (
                  <Text className="text-destructive text-sm mt-1 ml-1 font-medium">
                    {errors.email}
                  </Text>
                )}
              </View>

              <View className="space-y-2">
                <View className="flex-row items-center justify-between ml-1">
                  <Label className="text-foreground/90 font-semibold pt-2">
                    Mật khẩu
                  </Label>
                </View>

                <View className="flex-row items-center gap-3">
                  <View className="flex-1">
                    <Input
                      value={password}
                      secureTextEntry={!showPassword}
                      maxLength={64}
                      onChangeText={handlePasswordChange}
                      onBlur={() => handleBlur("password")}
                      placeholder="••••••••"
                      className={`h-14 rounded-2xl px-5 text-base bg-muted/30 border-transparent focus:border-primary focus:bg-background ${
                        errors.password && touched.password
                          ? "border-destructive bg-destructive/5"
                          : ""
                      }`}
                    />
                  </View>

                  <Pressable
                    onPress={() => setShowPassword(!showPassword)}
                    className="h-14 w-14 items-center justify-center rounded-2xl bg-muted/30 active:bg-muted/50 border border-transparent"
                  >
                    {showPassword ? (
                      <EyeOff className="text-muted-foreground" size={22} />
                    ) : (
                      <Eye className="text-muted-foreground" size={22} />
                    )}
                  </Pressable>
                </View>

                {errors.password && touched.password ? (
                  <Text className="text-destructive text-sm mt-1 ml-1 font-medium">
                    {errors.password}
                  </Text>
                ) : (
                  <Pressable className="mt-1 ml-1 self-end py-2">
                    <Text className="text-sm font-semibold text-primary">
                      Quên mật khẩu?
                    </Text>
                  </Pressable>
                )}
              </View>
            </View>

            {/* --- BUTTON SECTION --- */}
            <View className="mt-8">
              <Button
                onPress={handleEmailSignIn}
                disabled={isLoading}
                className="w-full h-14 rounded-2xl font-semibold flex-row items-center justify-center shadow-md shadow-primary/20 active:scale-[0.98] transition-transform"
              >
                {isLoading && (
                  <Loader2
                    className="mr-2 text-primary-foreground animate-spin"
                    size={18}
                  />
                )}
                <Text className="font-bold text-lg text-primary-foreground">
                  {isLoading ? "Đang xử lý..." : "Đăng nhập"}
                </Text>
              </Button>
            </View>

            {/* --- FOOTER SECTION --- */}
            <View className="mt-auto pt-10 items-center justify-center flex-row gap-2 opacity-70">
              <ShieldCheck size={18} className="text-emerald-500" />
              <Text className="text-sm font-medium text-muted-foreground">
                Bảo mật cấp doanh nghiệp
              </Text>
            </View>
          </View>
        </ScrollView>
      </TouchableWithoutFeedback>
      <Toast />
    </KeyboardAvoidingView>
  );
}
