import { SurfaceLayout } from "@/components/surface-layout";

export default function OperationsLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <SurfaceLayout label="OPERATIONS PORTAL">{children}</SurfaceLayout>;
}
