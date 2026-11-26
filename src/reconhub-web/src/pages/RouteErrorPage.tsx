import { isRouteErrorResponse, Link, useRouteError } from 'react-router'

/** Shown when rendering a route throws. */
export function RouteErrorPage() {
  const error = useRouteError()
  const message = isRouteErrorResponse(error)
    ? `${String(error.status)} ${error.statusText}`
    : error instanceof Error
      ? error.message
      : 'Unknown error'

  return (
    <main className="app-main" role="alert">
      <h1>Something went wrong</h1>
      <p className="page-subtitle">{message}</p>
      <Link to="/">Back to ReconHub</Link>
    </main>
  )
}
