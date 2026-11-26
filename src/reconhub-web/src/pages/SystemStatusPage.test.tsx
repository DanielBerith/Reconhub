import { screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { HealthResponse } from '../api/types'
import { renderRoute } from '../test/render'

function stubApi(response: () => Promise<Response>) {
  const fetchMock = vi.fn<(request: Request) => Promise<Response>>(response)
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

describe('SystemStatusPage', () => {
  it('shows the API health reported by GET /api/health', async () => {
    const health: HealthResponse = {
      status: 'Healthy',
      totalDurationMs: 12,
      checks: [{ name: 'database', status: 'Healthy', description: null, durationMs: 9 }],
    }
    const fetchMock = stubApi(() => Promise.resolve(Response.json(health)))

    renderRoute('/')

    const table = await screen.findByRole('table')
    expect(within(table).getByText('database')).toBeInTheDocument()
    expect(screen.getAllByText('Healthy')).toHaveLength(2)
    const request = fetchMock.mock.calls[0]?.[0]
    expect(request?.url).toMatch(/\/api\/health$/)
  })

  it('reports when the API cannot be reached', async () => {
    stubApi(() => Promise.reject(new TypeError('Failed to fetch')))

    renderRoute('/')

    expect(await screen.findByRole('alert')).toHaveTextContent('Cannot reach the ReconHub API')
  })

  it('renders the not-found page inside the layout for unknown routes', () => {
    stubApi(() => Promise.resolve(Response.json({})))

    renderRoute('/does-not-exist')

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
    expect(screen.getByRole('navigation', { name: 'Main' })).toBeInTheDocument()
  })
})
