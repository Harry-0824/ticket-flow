import { describe, expect, it } from 'vitest'
import { useFilters } from './useFilters'

describe('useFilters', () => {
  it('returns the existing filter defaults, options, and labels', () => {
    const filters = useFilters()

    expect(filters.statusFilter.value).toBe('')
    expect(filters.priorityFilter.value).toBe('')
    expect(filters.keywordFilter.value).toBe('')
    expect(filters.statusOptions).toEqual([
      'Open',
      'InProgress',
      'Done',
      'Archived',
    ])
    expect(filters.priorityOptions).toEqual(['Low', 'Medium', 'High'])
    expect(filters.statusLabels.Open).toBe('待處理')
    expect(filters.priorityLabels.High).toBe('高')
  })

  it('clears all filter refs', () => {
    const filters = useFilters()
    filters.statusFilter.value = 'Open'
    filters.priorityFilter.value = 'High'
    filters.keywordFilter.value = 'login'

    filters.clearFilters()

    expect(filters.statusFilter.value).toBe('')
    expect(filters.priorityFilter.value).toBe('')
    expect(filters.keywordFilter.value).toBe('')
  })
})
