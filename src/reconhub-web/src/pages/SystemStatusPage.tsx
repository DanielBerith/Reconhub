import { useHealth } from '../api/health'
import type { HealthStatus } from '../api/types'
import './SystemStatusPage.css'

const statusClass: Record<HealthStatus, string> = {
  Healthy: 'status-badge ok',
  Degraded: 'status-badge warn',
  Unhealthy: 'status-badge bad',
}

/** Placeholder page for milestone 1: proves the web app can reach the API through the generated client. */
export function SystemStatusPage() {
  const health = useHealth()

  return (
    <section aria-labelledby="status-heading">
      <div className="status-header">
        <div>
          <h1 id="status-heading">System status</h1>
          <p className="page-subtitle">Health of the ReconHub API and its dependencies.</p>
        </div>
        <button
          type="button"
          className="button"
          onClick={() => void health.refetch()}
          disabled={health.isFetching}
        >
          {health.isFetching ? 'Checking…' : 'Refresh'}
        </button>
      </div>

      <div className="card">
        {health.isPending ? (
          <p role="status">Checking API health…</p>
        ) : health.isError ? (
          <div role="alert">
            <span className={statusClass.Unhealthy}>Unreachable</span>
            <p className="status-error">
              Cannot reach the ReconHub API. Is it running on port 5080?
            </p>
          </div>
        ) : (
          <>
            <div className="status-overall">
              <span className={statusClass[health.data.status]}>{health.data.status}</span>
              <span className="status-meta">
                Checked at {new Date(health.dataUpdatedAt).toLocaleTimeString()} in{' '}
                {health.data.totalDurationMs.toFixed(0)} ms
              </span>
            </div>
            <table className="status-checks">
              <thead>
                <tr>
                  <th scope="col">Check</th>
                  <th scope="col">Status</th>
                  <th scope="col">Duration</th>
                </tr>
              </thead>
              <tbody>
                {health.data.checks.map((check) => (
                  <tr key={check.name}>
                    <td>{check.name}</td>
                    <td>
                      <span className={statusClass[check.status]}>{check.status}</span>
                    </td>
                    <td>{check.durationMs.toFixed(0)} ms</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </>
        )}
      </div>
    </section>
  )
}
