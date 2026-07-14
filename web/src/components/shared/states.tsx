"use client";

import { useEffect, useRef, useState } from "react";

type RetryProps = Readonly<{ onRetry: () => void }>;

export function LoadingState() { return <div className="state-card" role="status" aria-live="polite"><span className="loading-mark" aria-hidden="true" />Loading the next detail...</div>; }
export function EmptyState() { return <div className="state-card"><strong>No equipment here yet.</strong><p>When there is something to review, it will appear in this space.</p></div>; }
export function ErrorState({ onRetry }: RetryProps) { return <div className="state-card state-error" role="alert"><strong>That detail could not be loaded.</strong><button type="button" onClick={onRetry}>Try again</button></div>; }
export function Notification({ message }: Readonly<{ message: string }>) { return <div className="notice" role="status" aria-live="polite">{message}</div>; }
export function ConfirmationDemo() {
  const [open, setOpen] = useState(false); const [saved, setSaved] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null); const dialogRef = useRef<HTMLElement>(null);
  function close() { setOpen(false); requestAnimationFrame(() => triggerRef.current?.focus()); }
  useEffect(() => {
    if (!open) return;
    const dialog = dialogRef.current; const focusable = () => Array.from(dialog?.querySelectorAll<HTMLElement>('button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])') ?? []);
    focusable()[0]?.focus();
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") { event.preventDefault(); close(); return; }
      if (event.key !== "Tab") return;
      const items = focusable(); if (items.length === 0) { event.preventDefault(); return; }
      const first = items[0]; const last = items[items.length - 1];
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    }
    document.addEventListener("keydown", onKeyDown); return () => document.removeEventListener("keydown", onKeyDown);
  }, [open]);
  return <><button ref={triggerRef} type="button" onClick={() => setOpen(true)}>Confirm action</button>{open ? <div className="dialog-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) close(); }}><section ref={dialogRef} className="dialog" role="dialog" aria-modal="true" aria-labelledby="confirm-title" tabIndex={-1}><h3 id="confirm-title">Save this foundation setting?</h3><p>This is a non-business preview. No rental data is changed.</p><div className="dialog-actions"><button type="button" onClick={close}>Cancel</button><button type="button" onClick={() => { close(); setSaved(true); }}>Save setting</button></div></section></div> : null}{saved ? <Notification message="Foundation preference saved." /> : null}</>;
}
