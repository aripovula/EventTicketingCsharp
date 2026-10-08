export const EVENT_TYPES = ['Music', 'Sports', 'Comedy', 'Business', 'Theater', 'Food', 'Tech', 'Art', 'Other'] as const
export type EventType = (typeof EVENT_TYPES)[number]

export const EVENT_TYPE_COLORS: Record<string, string> = {
  Music:    '#a855f7',
  Sports:   '#22c55e',
  Comedy:   '#f59e0b',
  Business: '#64748b',
  Theater:  '#ec4899',
  Food:     '#f97316',
  Tech:     '#06b6d4',
  Art:      '#ef4444',
  Other:    '#9ca3af',
}

export function colorForType(eventType: string): string {
  return EVENT_TYPE_COLORS[eventType] ?? EVENT_TYPE_COLORS.Other
}
