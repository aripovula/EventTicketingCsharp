import { expect, type Page, type WebSocketRoute } from '@playwright/test'

const RS = '\x1e' // SignalR's JSON protocol ends every message with this record separator.

/**
 * Stands in for the API's /hubs/ticketing SignalR hub: answers negotiation and the handshake,
 * then lets a test push messages such as BookingMade to the page.
 */
export async function fakeHub(page: Page) {
  let socket: WebSocketRoute | undefined
  await page.route('**/hubs/ticketing/negotiate**', route => route.fulfill({ json: {
    negotiateVersion: 1, connectionId: 'test', connectionToken: 'test',
    availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text', 'Binary'] }],
  } }))
  await page.routeWebSocket('**/hubs/ticketing**', ws => {
    ws.onMessage(message => {
      if (String(message).includes('"protocol"')) {
        ws.send('{}' + RS)
        socket = ws
      }
    })
  })

  return {
    async push(target: string, eventId: number) {
      await expect.poll(() => socket !== undefined).toBe(true)
      socket!.send(JSON.stringify({ type: 1, target, arguments: [eventId] }) + RS)
    },
  }
}
