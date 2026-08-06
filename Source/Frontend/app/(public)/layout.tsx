export default function PublicLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <main className="relative z-10 flex min-w-0 grow flex-col overflow-hidden">
      <div className="flex min-w-0 grow flex-col overflow-hidden">
        {children}
      </div>
      <footer className="z-20 mt-auto flex shrink-0 items-center justify-between border-t border-border bg-card/50 px-3 py-2.5 text-xs text-muted-foreground backdrop-blur-sm sm:px-6">
        <div className="truncate">
          Copyright &copy; 2026 DTC.,Ltd. All rights reserved.
        </div>
        <div className="flex items-center gap-4">
          <span className="hidden sm:inline">Version 1.0.0</span>
        </div>
      </footer>
    </main>
  );
}
