import { describe, expect, it } from 'vitest'
import {
  buildTicketQueryString,
  getTicketApiErrorMessage,
  TicketApiError,
} from './tickets'

describe('buildTicketQueryString', () => {
  it('serializes supported ticket filters', () => {
    expect(
      buildTicketQueryString({
        status: 'Open',
        priority: 'High',
        keyword: 'login error',
      }),
    ).toBe('?status=Open&priority=High&keyword=login+error')
  })

  it('omits empty ticket filters', () => {
    expect(buildTicketQueryString({ keyword: '' })).toBe('')
  })
})

describe('getTicketApiErrorMessage', () => {
  it('returns the first validation field message', () => {
    const error = new TicketApiError(400, {
      message: '請修正工單欄位後再送出。',
      errors: {
        title: ['標題為必填。'],
      },
    })

    expect(getTicketApiErrorMessage(error, 'fallback')).toBe('標題為必填。')
  })
})
