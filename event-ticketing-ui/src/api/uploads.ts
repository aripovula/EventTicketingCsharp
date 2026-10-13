/** Images bigger than this are refused before upload; the upload URL itself can't cap the size. */
export const MAX_IMAGE_BYTES = 5 * 1024 * 1024

export const IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif']

/**
 * Uploads an event image straight to Blob Storage: the API hands out a write-only URL that works for
 * five minutes, and the file goes there without passing through the API. Resolves to the image's URL.
 */
export async function uploadImage(file: File): Promise<string> {
  if (!IMAGE_TYPES.includes(file.type)) throw new Error('Images must be JPEG, PNG, WebP or GIF.')
  if (file.size > MAX_IMAGE_BYTES) throw new Error('Images must be 5 MB or smaller.')

  const res = await fetch('/api/v1/admin/uploads/sas-url', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ contentType: file.type }),
  })
  if (!res.ok) throw new Error(`Could not start the upload (${res.status}). Please try again.`)
  const { uploadUrl, imageUrl } = (await res.json()).data

  const put = await fetch(uploadUrl, {
    method: 'PUT',
    headers: { 'x-ms-blob-type': 'BlockBlob', 'x-ms-blob-content-type': file.type },
    body: file,
  })
  if (!put.ok) throw new Error(`The upload failed (${put.status}). Please try again.`)
  return imageUrl
}
