import { useEffect, useState, type ReactNode } from 'react'
import { fetchMe, logout, refreshSession } from '../api/auth'
import { AuthContext, type User } from './authContext'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)

  useEffect(() => {
    // An expired access token gets one refresh attempt before giving up.
    fetchMe()
      .then(async res => {
        if (res.status !== 401) return res
        const refreshed = await refreshSession()
        return refreshed.ok ? fetchMe() : res
      })
      .then(res => (res.ok ? res.json() : null))
      .then(body => { if (body?.data) setUser(body.data) })
      .catch(() => {})
  }, [])

  async function signOut() {
    try {
      await logout()
    } finally {
      setUser(null)
    }
  }

  return <AuthContext.Provider value={{ user, signIn: setUser, signOut }}>{children}</AuthContext.Provider>
}
