import { Suspense } from "react";
import { AuthCard } from "@/components/auth/auth-card";
export default function LoginPage() { return <Suspense><div className="auth-shell"><AuthCard mode="login" /></div></Suspense>; }
