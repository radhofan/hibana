import { useEffect, useState } from 'react'
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, AreaChart, Area, Legend,
} from 'recharts'
import { api } from '../api/client'
import { useHub } from '../App'

export default function AnalyticsDashboard() {
  const [devices, setDevices] = useState([])
  const [selectedId, setSelectedId] = useState(null)
  const [telemetry, setTelemetry] = useState([])
  const [livePoints, setLivePoints] = useState([])
  const hub = useHub()

  useEffect(() => {
    api.get('/devices').then((devs) => {
      setDevices(devs)
      if (devs.length > 0) setSelectedId(devs[0].id)
    })
  }, [])

  useEffect(() => {
    if (!selectedId) return
    api.get(`/devices/${selectedId}/telemetry?hours=24`).then(setTelemetry)
    setLivePoints([])
  }, [selectedId])

  // Real-time incoming readings
  useEffect(() => {
    hub.on('TelemetryReceived', (reading) => {
      if (reading.deviceId === selectedId) {
        setLivePoints((prev) => [
          ...prev.slice(-59),
          { time: new Date(reading.timestamp).toLocaleTimeString(), value: reading.value },
        ])
      }
    })
    return () => hub.off('TelemetryReceived')
  }, [hub, selectedId])

  const historicalData = telemetry.map((r) => ({
    time: new Date(r.timestamp).toLocaleTimeString(),
    value: r.value,
  }))

  const liveData = livePoints.length > 0 ? livePoints : historicalData.slice(-60)
  const selectedDevice = devices.find((d) => d.id === selectedId)

  return (
    <div className="max-w-5xl mx-auto p-6 space-y-6">
      {/* Device selector */}
      <div className="flex items-center gap-4">
        <h1 className="text-xl font-bold text-gray-900">Analytics Dashboard</h1>
        <select
          value={selectedId ?? ''}
          onChange={(e) => setSelectedId(e.target.value)}
          className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-500"
        >
          {devices.map((d) => (
            <option key={d.id} value={d.id}>{d.name}</option>
          ))}
        </select>
        {hub.connected && (
          <span className="text-xs text-green-600 bg-green-50 px-2 py-0.5 rounded-full">
            Live
          </span>
        )}
      </div>

      {/* Live chart */}
      <div className="bg-white border border-gray-200 rounded-xl p-5">
        <h2 className="text-sm font-semibold text-gray-600 mb-4">
          Real-time Readings — {selectedDevice?.name}
          {selectedDevice && (
            <span className="ml-2 text-gray-400 font-normal">({selectedDevice.unit})</span>
          )}
        </h2>
        {liveData.length === 0 ? (
          <p className="text-gray-400 text-sm text-center py-12">No data yet.</p>
        ) : (
          <ResponsiveContainer width="100%" height={260}>
            <AreaChart data={liveData}>
              <defs>
                <linearGradient id="colorValue" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#06b6d4" stopOpacity={0.3} />
                  <stop offset="95%" stopColor="#06b6d4" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis dataKey="time" tick={{ fontSize: 11 }} interval="preserveStartEnd" />
              <YAxis tick={{ fontSize: 11 }} />
              <Tooltip />
              <Area
                type="monotone"
                dataKey="value"
                stroke="#06b6d4"
                strokeWidth={2}
                fill="url(#colorValue)"
                dot={false}
                isAnimationActive={false}
              />
              {selectedDevice && (
                <Line
                  type="monotone"
                  dataKey={() => selectedDevice.alertThreshold}
                  stroke="#ef4444"
                  strokeDasharray="4 4"
                  dot={false}
                  name="threshold"
                  strokeWidth={1.5}
                />
              )}
            </AreaChart>
          </ResponsiveContainer>
        )}
      </div>

      {/* All devices summary */}
      <div className="bg-white border border-gray-200 rounded-xl p-5">
        <h2 className="text-sm font-semibold text-gray-600 mb-4">Devices Overview</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          {devices.map((d) => (
            <button
              key={d.id}
              onClick={() => setSelectedId(d.id)}
              className={`text-left p-3 rounded-lg border transition-colors ${
                d.id === selectedId
                  ? 'border-cyan-500 bg-cyan-50'
                  : 'border-gray-200 hover:border-gray-300'
              }`}
            >
              <p className="text-xs font-semibold text-gray-700 truncate">{d.name}</p>
              <p className={`text-xs mt-0.5 ${
                d.status === 'Warning' ? 'text-amber-600' :
                d.status === 'Online' ? 'text-green-600' : 'text-gray-400'
              }`}>{d.status}</p>
              <p className="text-xs text-gray-400 mt-1">
                Threshold: {d.alertThreshold} {d.unit}
              </p>
            </button>
          ))}
        </div>
      </div>
    </div>
  )
}
