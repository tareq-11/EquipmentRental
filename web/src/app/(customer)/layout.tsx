import { SurfaceLayout } from "@/components/surface-layout";

export default function CustomerLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <SurfaceLayout label="CUSTOMER PORTAL">{children}</SurfaceLayout>;
}
