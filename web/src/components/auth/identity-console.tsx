"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { logoutSession, refreshSession, request } from "@/lib/auth-session";

type Role = "Customer" | "OperationsEmployee" | "Admin";
type Profile = { id: string; fullName: string; email: string; phoneNumber: string; role: Role; accountStatus: string; bookingStatus: string; isEmailVerified: boolean; isPhoneConfirmed: boolean };
type AccountPage = { items: Profile[]; totalCount: number; page: number };
const customerRoles: Role[] = ["Customer", "OperationsEmployee", "Admin"];
const operationsRoles: Role[] = ["OperationsEmployee", "Admin"];
const adminRoles: Role[] = ["Admin"];
const profileSchema = z.object({ fullName: z.string().min(2, "Enter your name."), phoneNumber: z.string().regex(/^(?:\+962|00962|0)7[789]\d{7}$/, "Use a Jordanian mobile number.") });

function useProtectedRoute(allowedRoles: Role[]) {
  const router = useRouter();
  const query = useQuery({ queryKey: ["profile"], queryFn: () => request("/api/auth/profile", "GET") as Promise<Profile>, retry: false });
  const allowed = query.data ? allowedRoles.includes(query.data.role) : false;
  useEffect(() => {
    if ((query.data && !allowed) || query.error) {
      const returnTo = `${window.location.pathname}${window.location.search}`;
      router.replace(`/login?returnTo=${encodeURIComponent(returnTo)}`);
    }
  }, [allowed, query.data, query.error, router]);
  return { profile: allowed ? query.data : null, error: query.error instanceof Error ? query.error.message : "" };
}

export function AccountConsole() {
  const { profile, error } = useProtectedRoute(customerRoles);
  const client = useQueryClient();
  const form = useForm<z.input<typeof profileSchema>>({ resolver: zodResolver(profileSchema), values: profile ? { fullName: profile.fullName, phoneNumber: profile.phoneNumber } : undefined });
  const update = useMutation({ mutationFn: (values: z.input<typeof profileSchema>) => request("/api/auth/profile", "PUT", values), onSuccess: () => void client.invalidateQueries({ queryKey: ["profile"] }) });
  const refresh = useMutation({ mutationFn: refreshSession });
  const router = useRouter();
  const logout = useMutation({ mutationFn: logoutSession, onSettled: () => { client.clear(); router.replace("/login"); } });
  if (!profile) return <PortalState error={error} />;
  const mutationError = update.error ?? refresh.error ?? logout.error;
  return <section className="shell portal"><p className="eyebrow">CUSTOMER ACCOUNT</p><h1 className="display">Profile and security</h1><p className="portal-copy">Rental records are introduced later. Your account and booking access are managed here.</p><div className="portal-grid"><form className="portal-card" onSubmit={form.handleSubmit((values) => update.mutate(values))} noValidate><h2>Profile</h2><label>Full name<input {...form.register("fullName")} />{form.formState.errors.fullName ? <span className="field-error">{form.formState.errors.fullName.message}</span> : null}</label><label>Jordan mobile<input {...form.register("phoneNumber")} />{form.formState.errors.phoneNumber ? <span className="field-error">{form.formState.errors.phoneNumber.message}</span> : null}</label><button disabled={update.isPending}>Save profile</button>{update.isSuccess ? <p className="form-success">Profile saved.</p> : null}</form><section className="portal-card"><h2>Account access</h2><dl><dt>Email</dt><dd>{profile.email}</dd><dt>Email verification</dt><dd>{profile.isEmailVerified ? "Verified" : "Not verified"}</dd><dt>Booking access</dt><dd>{profile.bookingStatus}</dd><dt>Phone confirmation</dt><dd>{profile.isPhoneConfirmed ? "Confirmed" : "Awaiting direct contact"}</dd></dl><div className="portal-actions"><button disabled={refresh.isPending} onClick={() => refresh.mutate()}>Refresh session</button><button disabled={logout.isPending} className="secondary" onClick={() => logout.mutate()}>Sign out</button></div>{refresh.isSuccess ? <p className="form-success">Session is active.</p> : null}</section></div>{mutationError instanceof Error ? <p className="field-error" role="alert">{mutationError.message}</p> : null}</section>;
}

export function OperationsConsole() {
  const { profile, error } = useProtectedRoute(operationsRoles);
  const [form] = [useForm<{ customerId: string }>({ defaultValues: { customerId: "" } })];
  const confirm = useMutation({ mutationFn: ({ customerId }: { customerId: string }) => request(`/api/operations/accounts/${customerId}/confirm-phone`, "POST") });
  if (!profile) return <PortalState error={error} />;
  return <section className="shell portal"><p className="eyebrow">OPERATIONS PORTAL</p><h1 className="display">Account contact desk</h1><p className="portal-copy">Direct-contact phone confirmation is the only M1 operations action. Rental workflow screens are intentionally unavailable.</p><form className="portal-card narrow" onSubmit={form.handleSubmit((values) => confirm.mutate(values))}><h2>Confirm customer phone</h2><label>Customer account ID<input {...form.register("customerId", { required: "Enter a customer account ID." })} placeholder="UUID from the admin account list" />{form.formState.errors.customerId ? <span className="field-error">{form.formState.errors.customerId.message}</span> : null}</label><button disabled={confirm.isPending}>Record direct confirmation</button>{confirm.isSuccess ? <p className="form-success">Phone confirmation recorded.</p> : null}{confirm.error instanceof Error ? <p className="field-error" role="alert">{confirm.error.message}</p> : null}</form></section>;
}

export function AdminConsole() {
  const { profile, error } = useProtectedRoute(adminRoles);
  const client = useQueryClient();
  const accounts = useQuery({ queryKey: ["accounts", 1], queryFn: () => request("/api/admin/accounts?page=1&pageSize=10", "GET") as Promise<AccountPage>, enabled: Boolean(profile), retry: false });
  const update = useMutation({ mutationFn: ({ account, bookingStatus }: { account: Profile; bookingStatus: string }) => { const reason = window.prompt(`Reason to mark booking access ${bookingStatus.toLowerCase()}:`); if (!reason) return Promise.resolve(); return request(`/api/admin/accounts/${account.id}`, "PUT", { bookingStatus, reason }); }, onSuccess: () => void client.invalidateQueries({ queryKey: ["accounts"] }) });
  const manage = useMutation({ mutationFn: (account: Profile) => { const role = window.prompt("Role: Customer, OperationsEmployee, or Admin", account.role); const accountStatus = window.prompt("Account status: Active, Suspended, or Disabled", account.accountStatus); const reason = window.prompt("Reason for this security administration change:"); if (!role || !accountStatus || !reason) return Promise.resolve(); if (!adminRoles.includes(role as Role) && !customerRoles.includes(role as Role)) throw new Error("Use Customer, OperationsEmployee, or Admin for the role."); if (!["Active", "Suspended", "Disabled"].includes(accountStatus)) throw new Error("Use Active, Suspended, or Disabled for the account status."); return request(`/api/admin/accounts/${account.id}`, "PUT", { role, accountStatus, reason }); }, onSuccess: () => void client.invalidateQueries({ queryKey: ["accounts"] }) });
  if (!profile) return <PortalState error={error} />;
  const actionError = accounts.error ?? update.error ?? manage.error;
  return <section className="shell portal"><p className="eyebrow">ADMIN PORTAL</p><h1 className="display">Account control room</h1><p className="portal-copy">M1 account, role, booking-access, and employee controls. Catalog and rental administration are later milestones.</p><section className="portal-card"><div className="table-title"><h2>Accounts</h2><span>{accounts.data?.totalCount ?? 0} total</span></div><div className="account-list">{accounts.data?.items.map((account) => <article key={account.id}><div><strong>{account.fullName}</strong><span>{account.email}</span><small>{account.role} / {account.accountStatus} / booking: {account.bookingStatus}</small></div><div className="portal-actions"><button className="secondary" disabled={manage.isPending} onClick={() => manage.mutate(account)}>Manage role/account</button><button className="secondary" disabled={update.isPending} onClick={() => update.mutate({ account, bookingStatus: account.bookingStatus === "Eligible" ? "Suspended" : "Eligible" })}>{account.bookingStatus === "Eligible" ? "Suspend booking" : "Restore booking"}</button></div></article>)}</div>{actionError instanceof Error ? <p className="field-error" role="alert">{actionError.message}</p> : null}</section></section>;
}

function PortalState({ error }: { error: string }) { return <section className="shell portal"><p className="eyebrow">SECURE PORTAL</p><h1 className="display">Checking account access</h1>{error ? <p className="field-error" role="alert">{error}</p> : <p>Loading your permitted workspace.</p>}</section>; }
