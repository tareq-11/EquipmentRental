import { Suspense } from "react";
import { AuthCard } from "@/components/auth/auth-card";
export default function RegisterPage() { return <Suspense><div className="auth-shell"><AuthCard mode="register" /></div></Suspense>; }
