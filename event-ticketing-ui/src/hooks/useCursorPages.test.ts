import { act, renderHook, waitFor } from '@testing-library/react'
import { useCursorPages } from './useCursorPages'

const page = (data: string[], nextCursor: string | null = null) =>
  new Response(JSON.stringify({ data, errors: [], meta: { nextCursor } }))

describe('useCursorPages', () => {
  it('loads the first page', async () => {
    const fetchPage = vi.fn().mockResolvedValue(page(['a', 'b']))

    const { result } = renderHook(() => useCursorPages<string>(fetchPage))

    await waitFor(() => expect(result.current.loading).toBe(false))
    expect(result.current.items).toEqual(['a', 'b'])
    expect(fetchPage).toHaveBeenCalledWith(null)
  })

  it('starts over when given a new fetchPage', async () => {
    const first = vi.fn().mockResolvedValue(page(['a']))
    const second = vi.fn().mockResolvedValue(page(['z']))
    const { result, rerender } = renderHook(({ fetchPage }) => useCursorPages<string>(fetchPage), {
      initialProps: { fetchPage: first },
    })
    await waitFor(() => expect(result.current.items).toEqual(['a']))

    rerender({ fetchPage: second })

    await waitFor(() => expect(result.current.items).toEqual(['z']))
    expect(second).toHaveBeenCalledWith(null)
  })

  it('ignores a late response from the previous fetchPage', async () => {
    let resolveOld: (res: Response) => void = () => {}
    const old = vi.fn().mockReturnValue(new Promise<Response>(resolve => { resolveOld = resolve }))
    const current = vi.fn().mockResolvedValue(page(['new']))
    const { result, rerender } = renderHook(({ fetchPage }) => useCursorPages<string>(fetchPage), {
      initialProps: { fetchPage: old },
    })

    rerender({ fetchPage: current })
    await waitFor(() => expect(result.current.items).toEqual(['new']))
    await act(async () => resolveOld(page(['stale'])))

    expect(result.current.items).toEqual(['new'])
  })

  it('reload fetches the first page again', async () => {
    const fetchPage = vi.fn().mockResolvedValueOnce(page(['a'])).mockResolvedValueOnce(page(['new', 'a']))
    const { result } = renderHook(() => useCursorPages<string>(fetchPage))
    await waitFor(() => expect(result.current.items).toEqual(['a']))

    act(() => result.current.reload())

    await waitFor(() => expect(result.current.items).toEqual(['new', 'a']))
    expect(fetchPage).toHaveBeenNthCalledWith(2, null)
  })
})
