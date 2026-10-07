import { createContext } from 'react'

export interface User {
  userId: number
  name: string
  email: string
  role: 'user' | 'admin'
}

export interface AuthContextValue {
  user: User | null
  signIn: (user: User) => void
  signOut: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
