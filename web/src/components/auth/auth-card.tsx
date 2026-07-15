"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { setSession } from "@/lib/auth-session";

const jordanPhone = /^(?:\+962|00962|0)7[789]\d{7}$/;
const authSchema = z.object({
  fullName: z.string().min(2, "Enter your name.").optional(),
  email: z.email("Enter a valid email."),
  phoneNumber: z.string().regex(jordanPhone, "Use a Jordanian mobile number.").optional(),
  password: z.string().min(12, "Use at least 12 characters.").refine((value) => new TextEncoder().encode(value).length <= 72, "Use a password no longer than 72 UTF-8 bytes.").optional(),
  passwordConfirmation: z.string().optional(),
  termsAccepted: z.boolean().optional(),
  code: z.string().regex(/^\d{6}$/, "Enter the six-digit code.").optional(),
  newPassword: z.string().min(12, "Use at least 12 characters.").refine((value) => new TextEncoder().encode(value).length <= 72, "Use a password no longer than 72 UTF-8 bytes.").optional(),
  newPasswordConfirmation: z.string().optional(),
}).superRefine((values, context) => {
  if (values.password !== undefined && values.passwordConfirmation !== undefined && values.password !== values.passwordConfirmation) {
    context.addIssue({ code: "custom", path: ["passwordConfirmation"], message: "Passwords do not match." });
  }
  if (values.newPassword !== undefined && values.newPasswordConfirmation !== undefined && values.newPassword !== values.newPasswordConfirmation) context.addIssue({ code: "custom", path: ["newPasswordConfirmation"], message: "Passwords do not match." });
});

type AuthValues = z.input<typeof authSchema>;
type Mode = "register" | "login" | "verify" | "forgot" | "reset";

function returnPath(value: string | null) {
  return value?.startsWith("/") && !value.startsWith("//") && !value.startsWith("/\\") ? value : "/customer";
}

async function callApi(path: string, body: unknown) {
  const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5066"}/api/auth/${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" }, credentials: "include",
    body: JSON.stringify(body),
  });
  const result = await response.json().catch(() => null) as { message?: string; data?: unknown } | null;
  if (!response.ok) throw new Error(result?.message ?? "Unable to complete that action.");
  return result?.data;
}

export function AuthCard({ mode }: { mode: Mode }) {
  const router = useRouter();
  const search = useSearchParams();
  const intendedReturnPath = returnPath(search.get("returnTo"));
  const isRegister = mode === "register";
  const isLogin = mode === "login";
  const isCode = mode === "verify" || mode === "reset";
  const schema = authSchema.superRefine((values, context) => {
    if (isRegister && values.termsAccepted !== true) context.addIssue({ code: "custom", path: ["termsAccepted"], message: "Accept the terms to continue." });
    if ((isLogin || isRegister) && !values.password) context.addIssue({ code: "custom", path: ["password"], message: "Use at least 12 characters." });
    if (isRegister && !values.passwordConfirmation) context.addIssue({ code: "custom", path: ["passwordConfirmation"], message: "Confirm your password." });
    if (isCode && !values.code) context.addIssue({ code: "custom", path: ["code"], message: "Enter the six-digit code." });
    if (mode === "reset" && !values.newPassword) context.addIssue({ code: "custom", path: ["newPassword"], message: "Use at least 12 characters." });
  });
  const form = useForm<AuthValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: search.get("email") ?? "", termsAccepted: false },
  });
  const mutation = useMutation({
    mutationFn: async (values: AuthValues) => {
        if (isRegister) return callApi("register", { fullName: values.fullName, email: values.email, phoneNumber: values.phoneNumber, password: values.password, passwordConfirmation: values.passwordConfirmation, termsAccepted: values.termsAccepted });
      if (isLogin) {
        const data = await callApi("login", { email: values.email, password: values.password }) as { accessToken: string; csrfToken: string };
        setSession(data);
        return data;
      }
      if (mode === "verify") return callApi("verify-email", { email: values.email, code: values.code, purpose: "EmailVerification" });
      if (mode === "forgot") return callApi("forgot-password", { email: values.email });
      return callApi("reset-password", { email: values.email, code: values.code, newPassword: values.newPassword, newPasswordConfirmation: values.newPasswordConfirmation });
    },
    onSuccess: () => {
      if (isRegister) router.push(`/verify-email?email=${encodeURIComponent(form.getValues("email"))}&returnTo=${encodeURIComponent(intendedReturnPath)}`);
      if (isLogin) router.replace(intendedReturnPath);
      if (mode === "verify") router.replace(intendedReturnPath);
      if (mode === "forgot") router.push(`/reset-password?email=${encodeURIComponent(form.getValues("email"))}`);
      if (mode === "reset") router.push(`/login?email=${encodeURIComponent(form.getValues("email"))}`);
    },
  });
  const resend = useMutation({ mutationFn: () => callApi("resend-verification", { email: form.getValues("email") }) });
  const title = ({ register: "Start with a verified address", login: "Welcome back to the loading bay", verify: "Enter the code from your email", forgot: "Recover your account", reset: "Set a new password" } as const)[mode];
  const submit = ({ register: "Create account", login: "Sign in", verify: "Verify email", forgot: "Send recovery code", reset: "Reset password" } as const)[mode];

  return <section className="auth-card" aria-labelledby="auth-title"><div className="auth-tag">ACCOUNT / {mode.toUpperCase()}</div><h1 id="auth-title">{title}</h1><p className="auth-copy">Your cart stays on this device. Signing in only unlocks the account actions that need a verified email.</p><form onSubmit={form.handleSubmit((values) => mutation.mutate(values))} noValidate>
    {isRegister ? <><Field form={form} name="fullName" label="Full name" autoComplete="name" /><Field form={form} name="phoneNumber" label="Jordan mobile" placeholder="0791234567" autoComplete="tel" /></> : null}
    <Field form={form} name="email" label="Email address" type="email" autoComplete="email" />
    {isCode ? <Field form={form} name="code" label="Six-digit code" inputMode="numeric" autoComplete="one-time-code" /> : null}
    {isLogin || isRegister ? <Field form={form} name="password" label="Password" type="password" autoComplete={isLogin ? "current-password" : "new-password"} /> : null}
    {isRegister ? <Field form={form} name="passwordConfirmation" label="Confirm password" type="password" autoComplete="new-password" /> : null}
    {mode === "reset" ? <><Field form={form} name="newPassword" label="New password" type="password" autoComplete="new-password" /><Field form={form} name="newPasswordConfirmation" label="Confirm new password" type="password" autoComplete="new-password" /></> : null}
    {isRegister ? <label className="check"><input type="checkbox" {...form.register("termsAccepted")} /> I accept the rental terms and privacy notice.{form.formState.errors.termsAccepted ? <span className="field-error">{form.formState.errors.termsAccepted.message}</span> : null}</label> : null}
    <button disabled={mutation.isPending} type="submit">{mutation.isPending ? "Working..." : submit}</button>
    {resend.isSuccess ? <p className="form-success">If the account exists, a new code is on its way.</p> : null}{mutation.error ? <p className="field-error" role="alert">{mutation.error.message}</p> : null}{resend.error ? <p className="field-error" role="alert">{resend.error.message}</p> : null}
   </form><nav className="auth-links" aria-label="Account links">{isLogin ? <><Link href={`/register?returnTo=${encodeURIComponent(intendedReturnPath)}`}>Create account</Link><Link href="/forgot-password">Forgot password?</Link></> : null}{isRegister ? <Link href={`/login?returnTo=${encodeURIComponent(intendedReturnPath)}`}>Already have an account?</Link> : null}{mode === "forgot" ? <Link href={`/reset-password?email=${encodeURIComponent(form.getValues("email"))}`}>I have a code</Link> : null}{mode === "verify" ? <button type="button" disabled={resend.isPending} onClick={() => resend.mutate()}>Resend code</button> : null}</nav></section>;
}

function Field({ form, name, label, ...props }: { form: ReturnType<typeof useForm<AuthValues>>; name: keyof AuthValues; label: string; [key: string]: unknown }) {
  const error = form.formState.errors[name]?.message;
  return <label className="auth-field">{label}<input {...form.register(name)} {...props} />{typeof error === "string" ? <span className="field-error">{error}</span> : null}</label>;
}
