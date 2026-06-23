import { describe, expect, it } from 'vitest'
import { AuthApiError, getAuthApiErrorMessage } from './auth'

describe('getAuthApiErrorMessage', () => {
  it('returns the first validation field message', () => {
    const error = new AuthApiError(400, {
      message: '請修正註冊欄位後再送出。',
      errors: {
        email: ['Email 已被註冊。'],
      },
    })

    expect(getAuthApiErrorMessage(error, 'fallback')).toBe('Email 已被註冊。')
  })

  it('returns a readable invalid login message for 401', () => {
    expect(getAuthApiErrorMessage(new AuthApiError(401), 'fallback')).toBe(
      'Email 或密碼不正確。',
    )
  })
})
