import { Suspense } from "react";
import { AuthCard } from "@/components/auth/auth-card";
export default function ResetPage() { return <Suspense><div className="auth-shell"><AuthCard mode="reset" /></div></Suspense>; }
