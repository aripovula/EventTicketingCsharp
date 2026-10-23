import { MAX_IMAGE_BYTES, uploadImage } from './uploads'

const png = (size = 3) => new File([new Uint8Array(size)], 'poster.png', { type: 'image/png' })

const slot = { uploadUrl: 'http://blob.test/images/abc.png?sig=1', imageUrl: 'http://blob.test/images/abc.png', expiresAt: '2026-10-12T20:05:00Z' }

describe('uploadImage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('gets an upload URL, PUTs the file there and returns the image URL', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ data: slot, errors: [] })))
      .mockResolvedValueOnce(new Response(null, { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)
    const file = png()

    const imageUrl = await uploadImage(file)

    expect(imageUrl).toBe('http://blob.test/images/abc.png')
    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/v1/admin/uploads/sas-url', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ contentType: 'image/png' }),
    })
    expect(fetchMock).toHaveBeenNthCalledWith(2, slot.uploadUrl, {
      method: 'PUT',
      headers: { 'x-ms-blob-type': 'BlockBlob', 'x-ms-blob-content-type': 'image/png' },
      body: file,
    })
  })

  it('refuses files that are not images without calling the API', async () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)

    await expect(uploadImage(new File(['x'], 'notes.txt', { type: 'text/plain' }))).rejects.toThrow('Images must be JPEG, PNG, WebP or GIF.')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('refuses images over 5 MB without calling the API', async () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)

    await expect(uploadImage(png(MAX_IMAGE_BYTES + 1))).rejects.toThrow('Images must be 5 MB or smaller.')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('reports when no upload URL is given', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 403 })))

    await expect(uploadImage(png())).rejects.toThrow('Could not start the upload (403). Please try again.')
  })

  it('reports a failed upload', async () => {
    vi.stubGlobal('fetch', vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ data: slot, errors: [] })))
      .mockResolvedValueOnce(new Response(null, { status: 403 })))

    await expect(uploadImage(png())).rejects.toThrow('The upload failed (403). Please try again.')
  })

  it('says so when image storage refuses the connection', async () => {
    vi.stubGlobal('fetch', vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ data: slot, errors: [] })))
      .mockRejectedValueOnce(new TypeError('Failed to fetch')))

    await expect(uploadImage(png())).rejects.toThrow("Couldn't reach image storage, so the image wasn't uploaded. Please try again.")
  })
})
