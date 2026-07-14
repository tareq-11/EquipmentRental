import { SurfaceLayout } from "@/components/surface-layout";

export default function AdminLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <SurfaceLayout label="ADMIN PORTAL">{children}</SurfaceLayout>;
}
