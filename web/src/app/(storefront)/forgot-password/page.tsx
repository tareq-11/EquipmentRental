import { Suspense } from "react";
import { AuthCard } from "@/components/auth/auth-card";
export default function ForgotPage() { return <Suspense><div className="auth-shell"><AuthCard mode="forgot" /></div></Suspense>; }
