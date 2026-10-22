import { refreshSession } from './auth'

let refreshing: Promise<boolean> | null = null

/**
 * Refreshes the session once, however many callers ask at the same moment. The server treats a
 * reused refresh token as stolen and revokes the whole session, so two refreshes must never race.
 */
export function refreshOnce(): Promise<boolean> {
  refreshing ??= refreshSession()
    .then(res => res.ok, () => false)
    .finally(() => { refreshing = null })
  return refreshing
}

/**
 * fetch with the auth cookies, for endpoints that need a signed-in user. The access token lives
 * 15 minutes, so a 401 gets one session refresh and one retry before the caller sees it.
 */
export async function authFetch(url: string, init: RequestInit = {}): Promise<Response> {
  const options: RequestInit = { ...init, credentials: 'include' }
  const res = await fetch(url, options)
  if (res.status !== 401) return res
  return (await refreshOnce()) ? fetch(url, options) : res
}
