import { describe, expect, it } from 'vitest'
import { buildTicketQueryString } from './tickets'

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
