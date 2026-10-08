import { describe, expect, test } from 'vitest'
import { EVENT_TYPES, EVENT_TYPE_COLORS, colorForType } from './eventTypes'

describe('EVENT_TYPES', () => {
  test('contains all expected category names', () => {
    expect(EVENT_TYPES).toContain('Music')
    expect(EVENT_TYPES).toContain('Sports')
    expect(EVENT_TYPES).toContain('Comedy')
    expect(EVENT_TYPES).toContain('Business')
    expect(EVENT_TYPES).toContain('Theater')
    expect(EVENT_TYPES).toContain('Food')
    expect(EVENT_TYPES).toContain('Tech')
    expect(EVENT_TYPES).toContain('Art')
    expect(EVENT_TYPES).toContain('Other')
  })

  test('has 9 categories', () => {
    expect(EVENT_TYPES).toHaveLength(9)
  })
})

describe('EVENT_TYPE_COLORS', () => {
  test('has a color entry for every EVENT_TYPE', () => {
    for (const type of EVENT_TYPES) {
      expect(EVENT_TYPE_COLORS[type]).toBeDefined()
    }
  })

  test('all color values are valid CSS hex colors', () => {
    for (const color of Object.values(EVENT_TYPE_COLORS)) {
      expect(color).toMatch(/^#[0-9a-f]{6}$/i)
    }
  })

  test('each type has a distinct color', () => {
    const colors = Object.values(EVENT_TYPE_COLORS)
    const unique = new Set(colors)
    expect(unique.size).toBe(colors.length)
  })
})

describe('colorForType', () => {
  test('returns the correct color for Music', () => {
    expect(colorForType('Music')).toBe(EVENT_TYPE_COLORS['Music'])
  })

  test('returns the correct color for Sports', () => {
    expect(colorForType('Sports')).toBe(EVENT_TYPE_COLORS['Sports'])
  })

  test('returns the correct color for Tech', () => {
    expect(colorForType('Tech')).toBe(EVENT_TYPE_COLORS['Tech'])
  })

  test('returns the fallback Other color for an unknown type', () => {
    expect(colorForType('Unknown')).toBe(EVENT_TYPE_COLORS['Other'])
  })

  test('returns the fallback Other color for an empty string', () => {
    expect(colorForType('')).toBe(EVENT_TYPE_COLORS['Other'])
  })
})
