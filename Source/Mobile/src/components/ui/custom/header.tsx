// import { ThemeToggle } from "@/components/theme-toggle";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Text } from "@/components/ui/text";
import { useAuthStore } from "@/hooks/use-auth-store";
import { authApi } from "@/services/auth-api";
import { useRouter } from "expo-router";
import { Bell, LogOut, Settings, User } from "lucide-react-native";
import { Pressable, View } from "react-native";

export function Header({ title }: { title?: string }) {
  const router = useRouter();
  const user = useAuthStore((state) => state.user);
  const { logout } = useAuthStore();

  const handleLogout = async () => {
    logout();
    // Dùng replace thay vì push khi đăng xuất để xóa lịch sử điều hướng (ngăn người dùng back lại)
    router.replace("/");
    try {
      await authApi.logout();
    } catch (error) {
      console.error("Lỗi khi gọi API logout:", error);
    }
  };

  return (
    <View className="bg-card border-b border-border px-5 h-16 flex-row items-center justify-between z-50">
      {/* Title */}
      <View className="flex-1 pr-4">
        {title ? (
          <Text
            className="text-lg font-semibold text-foreground tracking-tight"
            numberOfLines={1}
          >
            {title}
          </Text>
        ) : null}
      </View>

      {/* Actions */}
      <View className="flex-row items-center gap-4">
        {/* <ThemeToggle /> */}

        {/* Nút thông báo */}
        <Pressable className="relative p-2 rounded-full active:bg-accent/50">
          <Bell size={22} className="text-muted-foreground" />
          <View className="absolute top-1.5 right-1.5 w-2.5 h-2.5 bg-rose-500 rounded-full border-2 border-card" />
        </Pressable>

        {/* User Dropdown */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="default"
              size="icon"
              className="w-10 h-10 rounded-full bg-primary active:opacity-80"
            >
              <Text className="text-primary-foreground font-semibold">
                {user?.employeeFullName?.[0]?.toUpperCase() ?? "U"}
              </Text>
            </Button>
          </DropdownMenuTrigger>

          <DropdownMenuContent
            align="end" // Đổi thành end để menu không bị tràn ra ngoài màn hình trên mobile
            sideOffset={8}
            className="w-64 rounded-xl"
          >
            <DropdownMenuLabel className="px-3 py-3">
              <View className="flex-col space-y-1">
                <Text
                  className="text-sm font-semibold text-foreground"
                  numberOfLines={1}
                >
                  {user?.employeeFullName || "Người dùng"}
                </Text>
                <Text
                  className="text-xs text-muted-foreground"
                  numberOfLines={1}
                >
                  {user?.employeeEmail || "Chưa cập nhật email"}
                </Text>
              </View>
            </DropdownMenuLabel>

            <DropdownMenuSeparator />

            {/* Menu Items */}
            <DropdownMenuItem className="py-3 flex-row items-center">
              <User size={18} className="text-muted-foreground mr-3" />
              <Text className="font-medium text-foreground">Hồ sơ cá nhân</Text>
            </DropdownMenuItem>

            <DropdownMenuItem className="py-3 flex-row items-center">
              <Settings size={18} className="text-muted-foreground mr-3" />
              <Text className="font-medium text-foreground">Đổi mật khẩu</Text>
            </DropdownMenuItem>

            <DropdownMenuSeparator />

            <DropdownMenuItem
              className="py-3 flex-row items-center"
              onPress={handleLogout} // Native dùng onPress thay vì onClick
            >
              <LogOut size={18} className="text-destructive mr-3" />
              <Text className="font-medium text-destructive">Đăng xuất</Text>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </View>
    </View>
  );
}
