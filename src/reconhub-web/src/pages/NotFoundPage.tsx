import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <section>
      <h1>Page not found</h1>
      <p className="page-subtitle">The page you are looking for does not exist.</p>
      <Link to="/">Go to system status</Link>
    </section>
  )
}
