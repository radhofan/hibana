import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom'
import LiveMonitoringMap from './pages/LiveMonitoringMap'
import AnalyticsDashboard from './pages/AnalyticsDashboard'
import DeviceRegistry from './pages/DeviceRegistry'
import AlertFeed from './pages/AlertFeed'
import { useSignalR } from './hooks/useSignalR'
import { createContext, useContext, useState } from 'react'

const SignalRContext = createContext(null)
export function useHub() { return useContext(SignalRContext) }

function StatusDot({ connected }) {
  return (
    <span className={`inline-block w-2 h-2 rounded-full ${connected ? 'bg-green-400' : 'bg-gray-400'}`} />
  )
}

const navItems = [
  { to: '/', label: 'Live Map' },
  { to: '/dashboard', label: 'Analytics' },
  { to: '/devices', label: 'Devices' },
  { to: '/alerts', label: 'Alerts' },
]

export default function App() {
  const hub = useSignalR('/hubs/telemetry')
  const [alertCount, setAlertCount] = useState(0)

  // Track unread alert count via SignalR
  hub.on('AlertTriggered', () => setAlertCount((n) => n + 1))

  return (
    <SignalRContext.Provider value={hub}>
      <BrowserRouter>
        <nav className="bg-gray-900 text-white px-6 py-3 flex items-center gap-8">
          <span className="font-bold text-cyan-400 text-lg tracking-tight">IoT Hub</span>
          <div className="flex gap-1 flex-1">
            {navItems.map(({ to, label }) => (
              <NavLink
                key={to}
                to={to}
                end={to === '/'}
                className={({ isActive }) =>
                  `px-3 py-1.5 rounded-md text-sm font-medium transition-colors relative ${
                    isActive ? 'bg-gray-700 text-white' : 'text-gray-400 hover:text-white hover:bg-gray-800'
                  }`
                }
              >
                {label}
                {label === 'Alerts' && alertCount > 0 && (
                  <span className="absolute -top-1 -right-1 bg-red-500 text-white text-xs rounded-full w-4 h-4 flex items-center justify-center">
                    {alertCount > 9 ? '9+' : alertCount}
                  </span>
                )}
              </NavLink>
            ))}
          </div>
          <div className="flex items-center gap-2 text-xs text-gray-400">
            <StatusDot connected={hub.connected} />
            {hub.connected ? 'Live' : 'Disconnected'}
          </div>
        </nav>

        <main className="min-h-[calc(100vh-49px)] bg-gray-50">
          <Routes>
            <Route path="/" element={<LiveMonitoringMap />} />
            <Route path="/dashboard" element={<AnalyticsDashboard />} />
            <Route path="/devices" element={<DeviceRegistry />} />
            <Route path="/alerts" element={<AlertFeed onRead={() => setAlertCount(0)} />} />
          </Routes>
        </main>
      </BrowserRouter>
    </SignalRContext.Provider>
  )
}
