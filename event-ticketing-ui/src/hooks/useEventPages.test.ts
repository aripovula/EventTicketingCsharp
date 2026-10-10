import { act, renderHook, waitFor } from '@testing-library/react'
import { makeEvent } from '../test/fixtures'
import { useEventPages } from './useEventPages'

class FakeIntersectionObserver {
  observe() {}
  disconnect() {}
}

describe('useEventPages', () => {
  beforeEach(() => vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver))
  afterEach(() => vi.unstubAllGlobals())

  it('reload fetches the first page again', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ data: [makeEvent()], errors: [], meta: { nextCursor: null } })))
      .mockResolvedValueOnce(new Response(JSON.stringify({ data: [makeEvent(), makeEvent({ id: 2, title: 'New Gig' })], errors: [], meta: { nextCursor: null } })))
    vi.stubGlobal('fetch', fetchMock)
    const { result } = renderHook(() => useEventPages({ sort: 'name' }))
    await waitFor(() => expect(result.current.events).toHaveLength(1))

    act(() => result.current.reload())

    await waitFor(() => expect(result.current.events.map(e => e.title)).toEqual(['Jazz Night', 'New Gig']))
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})
