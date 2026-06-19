import { defineStore } from 'pinia'
import {
  clearStoredSession,
  readStoredSession,
  saveStoredSession,
  type AuthSession,
  type AuthUser,
} from '../api/session'

const storedSession = readStoredSession()

export const useAppStore = defineStore('app', {
  state: () => ({
    appName: 'TicketFlow',
    token: storedSession?.token ?? '',
    expiresAt: storedSession?.expiresAt ?? '',
    currentUser: storedSession?.user ?? null as AuthUser | null,
    authMessage: '',
  }),
  getters: {
    isAuthenticated: (state) => Boolean(state.token && state.currentUser),
  },
  actions: {
    setSession(session: AuthSession) {
      this.token = session.token
      this.expiresAt = session.expiresAt
      this.currentUser = session.user
      this.authMessage = ''
      saveStoredSession(session)
    },
    clearSession(message = '') {
      this.token = ''
      this.expiresAt = ''
      this.currentUser = null
      this.authMessage = message
      clearStoredSession()
    },
  },
})
