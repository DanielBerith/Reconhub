import { NavLink, Outlet } from 'react-router'
import './AppLayout.css'

const navigation = [{ to: '/', label: 'System status' }]

export function AppLayout() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <NavLink to="/" className="app-brand">
          <span className="app-brand-mark" aria-hidden="true">
            R
          </span>
          ReconHub
        </NavLink>
        <nav aria-label="Main">
          <ul className="app-nav">
            {navigation.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end
                  className={({ isActive }) => (isActive ? 'app-nav-link active' : 'app-nav-link')}
                >
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}
