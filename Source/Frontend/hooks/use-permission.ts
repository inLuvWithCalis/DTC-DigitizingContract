import { useCallback } from "react";

import { useAuthStore } from "@/hooks/use-auth-store";
import {
  hasAnyPermission,
  hasPermission,
  type RbacPermission,
} from "@/lib/rbac";

export function usePermission() {
  const permissions = useAuthStore((state) => state.user?.permissions);

  const can = useCallback(
    (permission: RbacPermission) => hasPermission(permissions, permission),
    [permissions],
  );
  const canAny = useCallback(
    (requiredPermissions: readonly RbacPermission[]) =>
      hasAnyPermission(permissions, requiredPermissions),
    [permissions],
  );

  return { can, canAny, permissions: permissions ?? [] };
}
