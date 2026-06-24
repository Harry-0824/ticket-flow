import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getAuthApiErrorMessage,
  login as loginRequest,
  register as registerRequest,
} from '../api/auth'
import { useAuth } from './useAuth'

vi.mock('../api/auth', () => ({
  getAuthApiErrorMessage: vi.fn((_error: unknown, fallback: string) => fallback),
  login: vi.fn(),
  register: vi.fn(),
}))

const mockedGetAuthApiErrorMessage = vi.mocked(getAuthApiErrorMessage)
const mockedLogin = vi.mocked(loginRequest)
const mockedRegister = vi.mocked(registerRequest)

const session = {
  token: 'token',
  expiresAt: '2026-06-24T00:00:00Z',
  user: {
    id: 1,
    email: 'alex@example.com',
    displayName: 'Alex Chen',
  },
}

describe('useAuth', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('logs in and returns the auth session', async () => {
    mockedLogin.mockResolvedValueOnce(session)
    const auth = useAuth()
    const input = {
      email: 'alex@example.com',
      password: 'Password123!',
    }

    await expect(auth.login(input)).resolves.toEqual(session)

    expect(mockedLogin).toHaveBeenCalledWith(input)
    expect(auth.errorMessage.value).toBe('')
  })

  it('sets an error message when login fails', async () => {
    const error = new Error('Invalid login')
    mockedLogin.mockRejectedValueOnce(error)
    mockedGetAuthApiErrorMessage.mockReturnValueOnce('Invalid credentials.')
    const auth = useAuth()

    await expect(
      auth.login({
        email: 'alex@example.com',
        password: 'wrong',
      }),
    ).resolves.toBeUndefined()

    expect(mockedGetAuthApiErrorMessage).toHaveBeenCalledWith(
      error,
      '登入失敗，請確認帳號密碼後再試。',
    )
    expect(auth.errorMessage.value).toBe('Invalid credentials.')
  })

  it('registers and returns the auth session', async () => {
    mockedRegister.mockResolvedValueOnce(session)
    const auth = useAuth()
    const input = {
      email: 'alex@example.com',
      displayName: 'Alex Chen',
      password: 'Password123!',
    }

    await expect(auth.register(input)).resolves.toEqual(session)

    expect(mockedRegister).toHaveBeenCalledWith(input)
    expect(auth.errorMessage.value).toBe('')
  })
})
