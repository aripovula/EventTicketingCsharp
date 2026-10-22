import { useEffect, useState, type ReactNode } from 'react'
import { fetchMe, logout } from '../api/auth'
import { refreshOnce } from '../api/session'
import { AuthContext, type User } from './authContext'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    // An expired access token gets one refresh attempt before giving up.
    fetchMe()
      .then(async res => {
        if (res.status !== 401) return res
        return (await refreshOnce()) ? fetchMe() : res
      })
      .then(res => (res.ok ? res.json() : null))
      .then(body => { if (body?.data) setUser(body.data) })
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [])

  // The access token lasts 15 minutes. Refreshing every 10 keeps a signed-in visitor signed in, so a
  // booking made after a long pause is still theirs rather than a guest's.
  useEffect(() => {
    if (!user) return
    const timer = setInterval(() => { void refreshOnce() }, 10 * 60 * 1000)
    return () => clearInterval(timer)
  }, [user])

  async function signOut() {
    try {
      await logout()
    } finally {
      setUser(null)
    }
  }

  return <AuthContext.Provider value={{ user, loading, signIn: setUser, signOut }}>{children}</AuthContext.Provider>
}
