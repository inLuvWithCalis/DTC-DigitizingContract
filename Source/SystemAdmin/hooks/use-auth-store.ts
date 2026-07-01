import { create } from "zustand";
import { UserProfileDto } from "@/services/auth-api";

interface AuthState {
  user: UserProfileDto | null;
  isAuthenticated: boolean;
  setUser: (userData: UserProfileDto | null) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,

  setUser: (userData) =>
    set({
      user: userData,
      isAuthenticated: !!userData,
    }),

  logout: () =>
    set({
      user: null,
      isAuthenticated: false,
    }),
}));
