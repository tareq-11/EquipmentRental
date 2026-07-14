import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "@/components/providers";

export const metadata: Metadata = {
  title: "Stagehand | Equipment rental",
  description: "A composed foundation for event equipment rental in Jordan.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" dir="ltr">
      <body><Providers>{children}</Providers></body>
    </html>
  );
}
