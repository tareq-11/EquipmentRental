const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5066";
type Session = { accessToken: string; csrfToken: string };
let session: Session | null = null;

async function responseData(response: Response) {
  const result = await response.json().catch(() => null) as { message?: string; data?: unknown } | null;
  if (!response.ok) throw new Error(result?.message ?? "Unable to complete that action.");
  return result?.data;
}

export function setSession(value: Session) { session = value; }
export function clearSession() { session = null; }
async function csrfToken() {
  const response = await fetch(`${apiUrl}/api/auth/csrf`, { credentials: "include" });
  return (await responseData(response) as { csrfToken: string }).csrfToken;
}
export async function refreshSession() {
  const response = await fetch(`${apiUrl}/api/auth/refresh`, { method: "POST", credentials: "include", headers: { "X-CSRF-TOKEN": session?.csrfToken ?? await csrfToken() } });
  session = await responseData(response) as Session;
  return session;
}
export async function request(path: string, method: string, body?: unknown, retry = true): Promise<unknown> {
  if (!session) await refreshSession();
  const current = session!;
  const response = await fetch(`${apiUrl}${path}`, { method, credentials: "include", headers: { "Content-Type": "application/json", Authorization: `Bearer ${current.accessToken}`, ...(path === "/api/auth/logout" ? { "X-CSRF-TOKEN": current.csrfToken } : {}) }, body: body === undefined ? undefined : JSON.stringify(body) });
  if (response.status === 401 && retry) { try { await refreshSession(); return request(path, method, body, false); } catch { clearSession(); } }
  return responseData(response);
}
export async function logoutSession() { try { await request("/api/auth/logout", "POST", undefined, false); } finally { clearSession(); } }
