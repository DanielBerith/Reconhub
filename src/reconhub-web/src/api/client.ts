import createClient from 'openapi-fetch'
import type { paths } from './schema.gen'

/**
 * VITE_API_BASE_URL points at the API directly (CORS). When empty, requests go to the page's own origin and
 * the Vite dev server proxies /api to the backend. Always absolute: relative Request URLs only resolve in browsers.
 */
function resolveBaseUrl(): string {
  const configured = import.meta.env.VITE_API_BASE_URL?.trim()
  return configured && configured.length > 0 ? configured : window.location.origin
}

/**
 * The single, typed client for the ReconHub API. Paths, parameters and responses come from
 * src/api/schema.gen.ts, generated from the backend's OpenAPI document (`npm run api:generate`).
 */
export const apiClient = createClient<paths>({
  baseUrl: resolveBaseUrl(),
  // Resolve fetch at call time so tests can stub it.
  fetch: (request) => globalThis.fetch(request),
})
