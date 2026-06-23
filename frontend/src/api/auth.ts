import { API_BASE_URL } from './config'
import type { AuthSession } from './session'

export type RegisterInput = {
  email: string
  displayName: string
  password: string
}

export type LoginInput = {
  email: string
  password: string
}

export type AuthValidationError = {
  message: string
  errors: Record<string, string[]>
}

const authUrl = (path: string) => `${API_BASE_URL}/auth${path}`

// AuthApiError 保留 status 與後端 validation payload，讓表單可顯示欄位級錯誤而不是只顯示通用失敗。
export class AuthApiError extends Error {
  readonly status: number
  readonly validation?: AuthValidationError

  constructor(status: number, validation?: AuthValidationError) {
    super(validation?.message ?? `Auth API request failed with status ${status}`)
    this.status = status
    this.validation = validation
  }
}

const requestAuth = async <T>(url: string, body: unknown): Promise<T> => {
  // 註冊與登入都是公開 POST endpoint，不帶 JWT；成功後才由回傳 session 建立登入狀態。
  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(body),
  })

  if (!response.ok) {
    throw new AuthApiError(response.status, await readAuthValidationError(response))
  }

  return response.json() as Promise<T>
}

const readAuthValidationError = async (
  response: Response,
): Promise<AuthValidationError | undefined> => {
  // 後端只在 400 回傳 validation shape，其他錯誤交由 status 與 fallback message 呈現。
  if (response.status !== 400) {
    return undefined
  }

  try {
    return (await response.json()) as AuthValidationError
  } catch {
    return undefined
  }
}

export const getAuthApiErrorMessage = (error: unknown, fallback: string) => {
  if (!(error instanceof AuthApiError)) {
    return fallback
  }

  const fieldMessage = Object.values(error.validation?.errors ?? {})
    .flat()
    .find(Boolean)

  if (fieldMessage) {
    return fieldMessage
  }

  if (error.status === 401) {
    return 'Email 或密碼不正確。'
  }

  return error.validation?.message ?? fallback
}

export const register = (input: RegisterInput) =>
  requestAuth<AuthSession>(authUrl('/register'), input)

export const login = (input: LoginInput) =>
  requestAuth<AuthSession>(authUrl('/login'), input)
