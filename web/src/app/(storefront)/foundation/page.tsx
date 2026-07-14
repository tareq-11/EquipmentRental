"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { ConfirmationDemo, EmptyState, ErrorState, LoadingState, Notification } from "@/components/shared/states";
import { en } from "@/localization/en";

const schema = z.object({ label: z.string().min(3, "Use at least 3 characters.").max(100) });
type FormValues = z.infer<typeof schema>;

export default function FoundationPreviewPage() {
  const [mode, setMode] = useState<"loading" | "empty" | "error">("loading"); const [saved, setSaved] = useState(false);
  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { label: "" } });
  return <section className="shell foundation-preview"><p className="eyebrow">SYSTEM / 00B</p><h1 className="display">{en.foundation.title}</h1><p className="lede">{en.foundation.description}</p><div className="state-switch" aria-label="Preview state"><button type="button" onClick={() => setMode("loading")}>Loading</button><button type="button" onClick={() => setMode("empty")}>Empty</button><button type="button" onClick={() => setMode("error")}>Error</button></div><div className="preview-frame">{mode === "loading" ? <LoadingState /> : mode === "empty" ? <EmptyState /> : <ErrorState onRetry={() => setMode("loading")} />}</div><div className="preview-grid"><section><h2>Validation that speaks plainly</h2><form onSubmit={form.handleSubmit(() => setSaved(true))}><label htmlFor="label">Foundation label</label><input id="label" {...form.register("label")} aria-invalid={Boolean(form.formState.errors.label)} /><p className="field-error">{form.formState.errors.label?.message}</p><button type="submit">Save preview</button></form>{saved ? <Notification message={en.foundation.saved} /> : null}</section><section><h2>Confirmation before a change</h2><p>Dialogs use clear verbs, a cancellable path, and semantic labels.</p><ConfirmationDemo /></section></div></section>;
}
