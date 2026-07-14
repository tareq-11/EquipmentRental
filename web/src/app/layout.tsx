import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Stagehand | Equipment rental",
  description: "A composed foundation for event equipment rental in Jordan.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" dir="ltr">
      <body>{children}</body>
    </html>
  );
}
