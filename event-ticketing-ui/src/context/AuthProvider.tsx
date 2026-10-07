import { useEffect, useState, type ReactNode } from 'react'
import { fetchMe } from '../api/auth'
import { AuthContext, type User } from './authContext'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)

  useEffect(() => {
    fetchMe()
      .then(res => (res.ok ? res.json() : null))
      .then(body => { if (body?.data) setUser(body.data) })
      .catch(() => {})
  }, [])

  return <AuthContext.Provider value={{ user, signIn: setUser }}>{children}</AuthContext.Provider>
}
