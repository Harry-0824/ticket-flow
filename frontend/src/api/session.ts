export type AuthUser = {
  id: number
  email: string
  displayName: string
}

export type AuthSession = {
  token: string
  expiresAt: string
  user: AuthUser
}

export const AUTH_UNAUTHORIZED_EVENT = 'ticketflow:unauthorized'

const TOKEN_KEY = 'ticketflow.auth.token'
const EXPIRES_AT_KEY = 'ticketflow.auth.expiresAt'
const USER_KEY = 'ticketflow.auth.user'

const hasStorage = () => typeof window !== 'undefined' && !!window.localStorage

export const getStoredToken = () =>
  hasStorage() ? window.localStorage.getItem(TOKEN_KEY) : null

export const readStoredSession = (): AuthSession | null => {
  if (!hasStorage()) {
    return null
  }

  const token = window.localStorage.getItem(TOKEN_KEY)
  const expiresAt = window.localStorage.getItem(EXPIRES_AT_KEY)
  const userJson = window.localStorage.getItem(USER_KEY)

  if (!token || !expiresAt || !userJson) {
    return null
  }

  try {
    return {
      token,
      expiresAt,
      user: JSON.parse(userJson) as AuthUser,
    }
  } catch {
    clearStoredSession()
    return null
  }
}

export const saveStoredSession = (session: AuthSession) => {
  if (!hasStorage()) {
    return
  }

  window.localStorage.setItem(TOKEN_KEY, session.token)
  window.localStorage.setItem(EXPIRES_AT_KEY, session.expiresAt)
  window.localStorage.setItem(USER_KEY, JSON.stringify(session.user))
}

export const clearStoredSession = () => {
  if (!hasStorage()) {
    return
  }

  window.localStorage.removeItem(TOKEN_KEY)
  window.localStorage.removeItem(EXPIRES_AT_KEY)
  window.localStorage.removeItem(USER_KEY)
}

export const emitUnauthorized = () => {
  if (typeof window === 'undefined') {
    return
  }

  window.dispatchEvent(new CustomEvent(AUTH_UNAUTHORIZED_EVENT))
}
