import { Suspense } from "react";
import { AuthCard } from "@/components/auth/auth-card";
export default function VerifyPage() { return <Suspense><div className="auth-shell"><AuthCard mode="verify" /></div></Suspense>; }
