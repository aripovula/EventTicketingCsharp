export function login(email: string, password: string) {
  return fetch('/api/v1/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ email, password }),
  })
}

export function fetchMe() {
  return fetch('/api/v1/auth/me', { credentials: 'include' })
}

export function logout() {
  return fetch('/api/v1/auth/logout', { method: 'POST', credentials: 'include' })
}

export function refreshSession() {
  return fetch('/api/v1/auth/refresh', { method: 'POST', credentials: 'include' })
}

export function register(name: string, email: string, password: string) {
  return fetch('/api/v1/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ name, email, password }),
  })
}
