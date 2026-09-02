"use client";

import { ChangeEvent, useMemo, useRef, useState } from "react";
import Link from "next/link";
import {
  Camera,
  CheckCircle2,
  ExternalLink,
  Eye,
  ImagePlus,
  KeyRound,
  Loader2,
  Mail,
  RefreshCw,
  Shield,
  Sparkles,
  Trash2,
  User,
} from "lucide-react";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Progress } from "@/components/ui/progress";
import { ConfirmDialog } from "@/components/ui/custom/confirm-dialog";
import { getApiErrorMessage } from "@/lib/api-error";
import {
  profileApi,
  resolveProfileImageUrl,
  type SystemAdminProfile,
} from "@/services/profile-api";

interface ProfileHeroBannerProps {
  profile: SystemAdminProfile;
  isReloading?: boolean;
  onReload: () => void | Promise<void>;
  onProfileChanged: (profile: SystemAdminProfile) => void;
}

export function ProfileHeroBanner({
  profile,
  isReloading = false,
  onReload,
  onProfileChanged,
}: ProfileHeroBannerProps) {
  const avatarInputRef = useRef<HTMLInputElement>(null);
  const coverInputRef = useRef<HTMLInputElement>(null);
  const [activeMutation, setActiveMutation] = useState<
    "avatar" | "cover" | null
  >(null);
  const [deleteTarget, setDeleteTarget] = useState<"avatar" | "cover" | null>(
    null,
  );
  const [previewImage, setPreviewImage] = useState<{
    url: string;
    title: string;
    type: "avatar" | "cover";
  } | null>(null);
  const [imageError, setImageError] = useState("");

  const avatarUrl = resolveProfileImageUrl(profile.imageUrl);
  const coverUrl = resolveProfileImageUrl(profile.coverImageUrl);

  const handleFileChange = async (
    kind: "avatar" | "cover",
    event: ChangeEvent<HTMLInputElement>,
  ) => {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;

    const maximumSize = kind === "avatar" ? 5 * 1024 * 1024 : 8 * 1024 * 1024;
    if (!["image/png", "image/jpeg"].includes(file.type)) {
      setImageError("Chỉ chấp nhận ảnh PNG hoặc JPEG.");
      return;
    }
    if (file.size > maximumSize) {
      setImageError(
        kind === "avatar"
          ? "Ảnh đại diện không được vượt quá 5 MB."
          : "Ảnh bìa không được vượt quá 8 MB.",
      );
      return;
    }

    setActiveMutation(kind);
    setImageError("");
    try {
      const updated =
        kind === "avatar"
          ? await profileApi.uploadAvatar(file, profile.rowVersion)
          : await profileApi.uploadCover(file, profile.rowVersion);
      onProfileChanged(updated);
    } catch (error) {
      setImageError(getApiErrorMessage(error, "Không thể cập nhật ảnh hồ sơ."));
    } finally {
      setActiveMutation(null);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    const target = deleteTarget;
    setActiveMutation(target);
    setImageError("");
    try {
      const updated =
        target === "avatar"
          ? await profileApi.deleteAvatar(profile.rowVersion)
          : await profileApi.deleteCover(profile.rowVersion);
      onProfileChanged(updated);
      setDeleteTarget(null);
    } catch (error) {
      setImageError(getApiErrorMessage(error, "Không thể xóa ảnh hồ sơ."));
    } finally {
      setActiveMutation(null);
    }
  };

  // Calculate profile completion percentage
  const completion = useMemo(() => {
    const fields = [
      Boolean(profile.fullName?.trim()),
      Boolean(profile.email?.trim()),
      Boolean(profile.username?.trim()),
      Boolean(!profile.mustChangePassword),
    ];

    const completedCount = fields.filter(Boolean).length;
    const percentage = Math.round((completedCount / fields.length) * 100);

    return {
      percentage,
      completedCount,
      totalCount: fields.length,
    };
  }, [profile]);

  const initials = useMemo(() => {
    if (!profile.fullName) return "SA";
    const parts = profile.fullName.trim().split(/\s+/);
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }, [profile.fullName]);

  return (
    <>
      <div className="relative overflow-hidden rounded-2xl border border-border/80 bg-card shadow-sm">
        <input
          ref={avatarInputRef}
          type="file"
          accept="image/png,image/jpeg"
          className="hidden"
          onChange={(event) => void handleFileChange("avatar", event)}
        />
        <input
          ref={coverInputRef}
          type="file"
          accept="image/png,image/jpeg"
          className="hidden"
          onChange={(event) => void handleFileChange("cover", event)}
        />

        {/* Decorative gradient banner background */}
        <div
          className="relative h-32 w-full bg-gradient-to-r from-primary/20 via-primary/10 to-accent/30 bg-cover bg-center dark:from-primary/25 dark:via-primary/15 dark:to-accent/15 sm:h-36"
          style={coverUrl ? { backgroundImage: `url(${coverUrl})` } : undefined}
        >
          {coverUrl && <div className="absolute inset-0 bg-black/25" />}
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_20%,rgba(255,255,255,0.2),transparent_70%)] dark:bg-[radial-gradient(circle_at_30%_20%,rgba(255,255,255,0.05),transparent_70%)]" />

          {/* Cover Action Buttons */}
          <div className="absolute left-4 top-4 z-10 flex items-center gap-2">
            <Button
              type="button"
              size="sm"
              variant="secondary"
              className="h-8 bg-card/85 text-xs shadow-sm backdrop-blur-md hover:bg-card"
              disabled={activeMutation !== null}
              onClick={() => coverInputRef.current?.click()}
            >
              {activeMutation === "cover" ? (
                <Loader2 className="size-3.5 animate-spin" />
              ) : (
                <ImagePlus className="size-3.5" />
              )}
              {coverUrl ? "Đổi ảnh bìa" : "Thêm ảnh bìa"}
            </Button>
            {coverUrl && (
              <>
                <Button
                  type="button"
                  size="sm"
                  variant="secondary"
                  className="h-8 bg-card/85 text-xs shadow-sm backdrop-blur-md hover:bg-card gap-1.5"
                  onClick={() =>
                    setPreviewImage({
                      url: coverUrl,
                      title: `Ảnh bìa - ${profile.fullName || "System Admin"}`,
                      type: "cover",
                    })
                  }
                >
                  <Eye className="size-3.5" />
                  <span className="hidden sm:inline">Xem ảnh bìa</span>
                </Button>
                <Button
                  type="button"
                  size="icon"
                  variant="secondary"
                  className="size-8 bg-card/85 shadow-sm backdrop-blur-md hover:bg-card"
                  disabled={activeMutation !== null}
                  onClick={() => setDeleteTarget("cover")}
                  aria-label="Xóa ảnh bìa"
                  title="Xóa ảnh bìa"
                >
                  <Trash2 className="size-3.5 text-destructive" />
                </Button>
              </>
            )}
          </div>

          <div className="absolute right-4 top-4 hidden items-center gap-2 sm:flex">
            <Badge
              variant="secondary"
              className="bg-card/80 backdrop-blur-md border border-border/60 text-xs font-normal"
            >
              System Admin
            </Badge>
            <Badge
              variant="secondary"
              className="bg-card/80 backdrop-blur-md border border-border/60 text-xs font-normal flex items-center gap-1.5"
            >
              <span
                className={`size-2 rounded-full ${
                  profile.isActive
                    ? "bg-emerald-500 animate-pulse"
                    : "bg-destructive"
                }`}
              />
              <span>{profile.isActive ? "Hoạt động" : "Tạm khóa"}</span>
            </Badge>
          </div>
        </div>

        <div className="relative px-5 pb-6 pt-0 sm:px-8">
          <div className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between -mt-10 sm:-mt-12">
            {/* Avatar and Info */}
            <div className="flex flex-col sm:flex-row items-center sm:items-end gap-4 text-center sm:text-left">
              <div className="group relative">
                {/* Avatar Display with click to zoom or upload & hover overlay */}
                <div
                  className="relative cursor-pointer overflow-hidden rounded-2xl border-4 border-card shadow-md transition-all duration-200 group-hover:shadow-lg"
                  onClick={() => {
                    if (avatarUrl) {
                      setPreviewImage({
                        url: avatarUrl,
                        title: `Ảnh đại diện - ${profile.fullName || "System Admin"}`,
                        type: "avatar",
                      });
                    } else {
                      avatarInputRef.current?.click();
                    }
                  }}
                  title={
                    avatarUrl
                      ? "Nhấp để xem ảnh đại diện kích thước lớn"
                      : "Nhấp để thêm ảnh đại diện"
                  }
                >
                  <Avatar className="size-24 sm:size-28 rounded-xl bg-gradient-to-tr from-primary to-primary/80 text-primary-foreground">
                    {avatarUrl && (
                      <AvatarImage
                        src={avatarUrl}
                        alt={profile.fullName || "System Admin"}
                        className="rounded-xl object-cover"
                      />
                    )}
                    <AvatarFallback className="rounded-xl text-2xl font-bold bg-primary text-primary-foreground">
                      {initials}
                    </AvatarFallback>
                  </Avatar>

                  {/* Hover Overlay */}
                  <div className="absolute inset-0 flex flex-col items-center justify-center gap-1 bg-black/45 opacity-0 backdrop-blur-[1.5px] transition-opacity duration-200 group-hover:opacity-100 text-white">
                    {activeMutation === "avatar" ? (
                      <Loader2 className="size-5 animate-spin" />
                    ) : avatarUrl ? (
                      <>
                        <Eye className="size-5" />
                        <span className="text-[10px] font-medium tracking-tight">
                          Xem ảnh
                        </span>
                      </>
                    ) : (
                      <>
                        <Camera className="size-5" />
                        <span className="text-[10px] font-medium tracking-tight">
                          Thêm ảnh
                        </span>
                      </>
                    )}
                  </div>
                </div>

                {/* Active Status Badge (Top-Left) */}
                <div
                  className={`absolute -top-1 -left-1 flex size-5 items-center justify-center rounded-full border-2 border-card text-white shadow-sm ${
                    profile.isActive ? "bg-emerald-500" : "bg-destructive"
                  }`}
                  title={
                    profile.isActive
                      ? "Tài khoản đang hoạt động"
                      : "Tài khoản tạm khóa"
                  }
                >
                  <CheckCircle2 className="size-3 stroke-[2.5]" />
                </div>

                {/* Floating Camera Button (Bottom-Right) */}
                <button
                  type="button"
                  className="absolute -bottom-1 -right-1 flex size-8 items-center justify-center rounded-full border-2 border-card bg-primary text-primary-foreground shadow-md transition-transform duration-150 hover:scale-110 hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring active:scale-95 disabled:pointer-events-none disabled:opacity-50"
                  disabled={activeMutation !== null}
                  onClick={(e) => {
                    e.stopPropagation();
                    avatarInputRef.current?.click();
                  }}
                  aria-label={
                    avatarUrl ? "Đổi ảnh đại diện" : "Thêm ảnh đại diện"
                  }
                  title={avatarUrl ? "Đổi ảnh đại diện" : "Thêm ảnh đại diện"}
                >
                  {activeMutation === "avatar" ? (
                    <Loader2 className="size-4 animate-spin" />
                  ) : (
                    <Camera className="size-4" />
                  )}
                </button>

                {/* Delete Avatar Button (Top-Right) */}
                {avatarUrl && (
                  <button
                    type="button"
                    className="absolute -top-1 -right-1 flex size-7 items-center justify-center rounded-full border-2 border-card bg-destructive text-destructive-foreground shadow-md opacity-100 transition-all duration-150 hover:scale-110 hover:bg-destructive/90 sm:opacity-0 sm:group-hover:opacity-100 focus:outline-none"
                    disabled={activeMutation !== null}
                    onClick={(e) => {
                      e.stopPropagation();
                      setDeleteTarget("avatar");
                    }}
                    aria-label="Xóa ảnh đại diện"
                    title="Xóa ảnh đại diện"
                  >
                    <Trash2 className="size-3.5" />
                  </button>
                )}
              </div>

              <div className="space-y-1.5">
                <div className="flex flex-wrap items-center justify-center sm:justify-start gap-2">
                  <h1 className="text-xl sm:text-2xl font-bold tracking-tight text-foreground">
                    {profile.fullName || "System Admin"}
                  </h1>
                  <Badge
                    variant="outline"
                    className="font-mono text-xs font-normal"
                  >
                    #{profile.systemAdminId}
                  </Badge>
                </div>

                <div className="flex flex-wrap items-center justify-center sm:justify-start gap-3 text-xs sm:text-sm text-muted-foreground">
                  {profile.username && (
                    <span className="flex items-center gap-1">
                      <User className="size-3.5" />
                      <span>@{profile.username}</span>
                    </span>
                  )}
                  {profile.email && (
                    <span className="flex items-center gap-1">
                      <Mail className="size-3.5 text-primary" />
                      <span>{profile.email}</span>
                    </span>
                  )}
                  {profile.roleName && (
                    <span className="flex items-center gap-1">
                      <Shield className="size-3.5 text-primary" />
                      <span className="font-medium text-foreground">
                        {profile.roleName}
                      </span>
                    </span>
                  )}
                </div>
              </div>
            </div>

            {/* Action buttons */}
            <div className="flex items-center justify-center sm:justify-end gap-2.5">
              <Button
                variant="outline"
                size="sm"
                onClick={onReload}
                disabled={isReloading}
                className="gap-1.5 shadow-sm text-xs sm:text-sm"
                title="Làm mới dữ liệu hồ sơ"
              >
                <RefreshCw
                  className={`size-3.5 ${isReloading ? "animate-spin" : ""}`}
                />
                <span>Làm mới</span>
              </Button>
              <Button
                asChild
                size="sm"
                variant="secondary"
                className="gap-1.5 shadow-sm text-xs sm:text-sm"
              >
                <Link href="/change-password">
                  <KeyRound className="size-3.5" />
                  <span>Đổi mật khẩu</span>
                </Link>
              </Button>
            </div>
          </div>

          {/* Profile Completion Bar */}
          <div className="mt-6 rounded-xl border border-border/60 bg-muted/30 p-3 sm:p-4">
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 mb-2">
              <div className="flex items-center gap-2">
                <Sparkles className="size-4 text-primary" />
                <span className="text-xs sm:text-sm font-medium text-foreground">
                  Mức độ hoàn thiện tài khoản
                </span>
                <span className="text-xs text-muted-foreground">
                  ({completion.completedCount}/{completion.totalCount} tiêu chí)
                </span>
              </div>
              <div className="flex items-center gap-2">
                <span className="text-xs font-semibold text-primary">
                  {completion.percentage}%
                </span>
                <span className="text-xs text-muted-foreground">
                  {completion.percentage === 100
                    ? "Tài khoản đã hoàn tất thiết lập"
                    : "Cần cập nhật thêm"}
                </span>
              </div>
            </div>
            <Progress value={completion.percentage} className="h-2" />
          </div>
          {imageError && (
            <p className="mt-3 text-sm text-destructive" role="alert">
              {imageError}
            </p>
          )}
        </div>
      </div>

      {/* Delete Image Confirmation Dialog */}
      <ConfirmDialog
        isOpen={deleteTarget !== null}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => void handleDelete()}
        title={deleteTarget === "avatar" ? "Xóa ảnh đại diện?" : "Xóa ảnh bìa?"}
        description="Ảnh sẽ bị xóa khỏi hồ sơ và private storage. Thao tác này không thể hoàn tác."
        icon={<Trash2 className="size-5 text-destructive" />}
        confirmText="Xóa ảnh"
        variant="destructive"
        isLoading={activeMutation !== null}
      />

      {/* Full Image Preview Modal Dialog */}
      <Dialog
        open={previewImage !== null}
        onOpenChange={(open) => !open && setPreviewImage(null)}
      >
        <DialogContent className="max-w-3xl overflow-hidden p-0 gap-0 border-border/80 bg-background/95 backdrop-blur-xl shadow-2xl">
          <DialogHeader className="p-4 sm:p-5 border-b border-border/60">
            <DialogTitle className="text-base font-semibold flex items-center gap-2">
              {previewImage?.type === "avatar" ? (
                <User className="size-4 text-primary" />
              ) : (
                <ImagePlus className="size-4 text-primary" />
              )}
              <span>{previewImage?.title}</span>
            </DialogTitle>
            <DialogDescription className="text-xs text-muted-foreground">
              Xem ảnh hồ sơ Quản trị viên ở kích thước đầy đủ
            </DialogDescription>
          </DialogHeader>

          <div className="relative flex items-center justify-center bg-black/5 dark:bg-black/40 p-4 sm:p-6 min-h-[300px] max-h-[70vh] overflow-auto">
            {previewImage?.url && (
              // eslint-disable-next-line @next/next/no-img-element
              <img
                src={previewImage.url}
                alt={previewImage.title}
                className={`max-h-[60vh] max-w-full object-contain shadow-md ${
                  previewImage.type === "avatar"
                    ? "rounded-2xl border-4 border-card"
                    : "rounded-xl border border-border"
                }`}
              />
            )}
          </div>

          <DialogFooter className="p-3 sm:p-4 border-t border-border/60 bg-muted/20 flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-2">
              {previewImage?.url && (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="gap-1.5 text-xs"
                  onClick={() => window.open(previewImage.url, "_blank")}
                >
                  <ExternalLink className="size-3.5" />
                  <span>Mở tab mới</span>
                </Button>
              )}
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="gap-1.5 text-xs"
                onClick={() => {
                  const type = previewImage?.type;
                  setPreviewImage(null);
                  if (type === "avatar") {
                    avatarInputRef.current?.click();
                  } else {
                    coverInputRef.current?.click();
                  }
                }}
              >
                <Camera className="size-3.5" />
                <span>Đổi ảnh</span>
              </Button>
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="gap-1.5 text-xs text-destructive hover:bg-destructive/10"
                onClick={() => {
                  const type = previewImage?.type;
                  setPreviewImage(null);
                  if (type) setDeleteTarget(type);
                }}
              >
                <Trash2 className="size-3.5" />
                <span>Xóa ảnh</span>
              </Button>
            </div>

            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => setPreviewImage(null)}
            >
              Đóng
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
