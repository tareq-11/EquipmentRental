import Link from "next/link";

type SurfaceLayoutProps = Readonly<{
  children: React.ReactNode;
  label: string;
  homeHref?: string;
}>;

export function SurfaceLayout({ children, label, homeHref = "/" }: SurfaceLayoutProps) {
  return (
    <>
      <a href="#main-content" className="skip-link">Skip to main content</a>
      <header className="shell flex items-center justify-between border-b border-[#0c2633]/20 py-5">
        <Link href={homeHref} className="display text-2xl tracking-tight">
          STAGEHAND<span className="text-[#ec6b45]">/</span>
        </Link>
        <p className="text-xs font-bold tracking-[.16em] text-[#0c2633]/70">{label}</p>
      </header>
      <main id="main-content" tabIndex={-1}>{children}</main>
    </>
  );
}
