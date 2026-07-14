import Link from "next/link";

export default function StorefrontLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <>
      <a href="#main-content" className="skip-link">Skip to main content</a>
      <header className="shell flex items-center justify-between py-6">
        <Link href="/" className="display text-2xl tracking-tight">STAGEHAND<span className="text-[#ec6b45]">/</span></Link>
        <nav aria-label="Primary navigation" className="flex gap-5 text-sm font-semibold">
          <a href="#how-it-works">How it works</a>
          <Link href="/account">Account</Link>
        </nav>
      </header>
      <main id="main-content" tabIndex={-1}>{children}</main>
      <footer className="shell border-t border-[#0c2633]/20 py-8 text-sm">Jordan event equipment rental · Foundation preview</footer>
    </>
  );
}
