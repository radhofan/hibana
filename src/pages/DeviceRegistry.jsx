import { useEffect, useState } from 'react'
import { api } from '../api/client'

const STATUS_BADGE = {
  Online: 'bg-green-100 text-green-700',
  Warning: 'bg-amber-100 text-amber-700',
  Error: 'bg-red-100 text-red-700',
  Offline: 'bg-gray-100 text-gray-600',
}

const EMPTY_FORM = {
  hardwareId: '', name: '', latitude: '', longitude: '',
  alertThreshold: '', unit: '°C',
}

export default function DeviceRegistry() {
  const [devices, setDevices] = useState([])
  const [form, setForm] = useState(EMPTY_FORM)
  const [editThreshold, setEditThreshold] = useState({})  // { [deviceId]: string }
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api.get('/devices').then(setDevices).catch(console.error)
  }, [])

  function set(field, value) { setForm((f) => ({ ...f, [field]: value })) }

  async function handleRegister(e) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const newDevice = await api.post('/devices', {
        ...form,
        latitude: parseFloat(form.latitude),
        longitude: parseFloat(form.longitude),
        alertThreshold: parseFloat(form.alertThreshold),
      })
      setDevices((prev) => [...prev, newDevice])
      setForm(EMPTY_FORM)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  async function handleThresholdUpdate(device) {
    const val = editThreshold[device.id]
    if (!val) return
    try {
      await api.put(`/devices/${device.id}/threshold`, { threshold: parseFloat(val) })
      setDevices((prev) =>
        prev.map((d) => d.id === device.id ? { ...d, alertThreshold: parseFloat(val) } : d)
      )
      setEditThreshold((prev) => { const n = { ...prev }; delete n[device.id]; return n })
    } catch (err) {
      alert(err.message)
    }
  }

  return (
    <div className="max-w-4xl mx-auto p-6 space-y-6">
      <h1 className="text-xl font-bold text-gray-900">Device Registry</h1>

      {/* Registration form */}
      <div className="bg-white border border-gray-200 rounded-xl p-5">
        <h2 className="text-sm font-semibold text-gray-700 mb-4">Register New Device</h2>
        <form onSubmit={handleRegister} className="grid grid-cols-2 gap-3">
          {[
            ['hardwareId', 'Hardware ID', 'e.g. DEVICE-001'],
            ['name', 'Name', 'e.g. Smart Meter 1'],
            ['latitude', 'Latitude', '37.7749'],
            ['longitude', 'Longitude', '-122.4194'],
            ['alertThreshold', 'Alert Threshold', '80'],
            ['unit', 'Unit', '°C'],
          ].map(([field, label, placeholder]) => (
            <div key={field}>
              <label className="block text-xs font-medium text-gray-600 mb-1">{label}</label>
              <input
                required
                value={form[field]}
                onChange={(e) => set(field, e.target.value)}
                placeholder={placeholder}
                className="w-full border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-500"
              />
            </div>
          ))}

          {error && (
            <p className="col-span-2 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{error}</p>
          )}

          <div className="col-span-2">
            <button
              type="submit"
              disabled={loading}
              className="bg-cyan-600 text-white rounded-lg px-5 py-2 text-sm font-medium hover:bg-cyan-700 disabled:opacity-50 transition-colors"
            >
              {loading ? 'Registering…' : 'Register Device'}
            </button>
          </div>
        </form>
      </div>

      {/* Device table */}
      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              {['Hardware ID', 'Name', 'Location', 'Status', 'Threshold', 'Last Seen', ''].map((h) => (
                <th key={h} className="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {devices.length === 0 ? (
              <tr><td colSpan={7} className="text-center text-gray-400 py-8 text-sm">No devices registered.</td></tr>
            ) : devices.map((d) => (
              <tr key={d.id} className="hover:bg-gray-50">
                <td className="px-4 py-3 font-mono text-xs text-gray-600">{d.hardwareId}</td>
                <td className="px-4 py-3 font-medium text-gray-800">{d.name}</td>
                <td className="px-4 py-3 text-gray-500 text-xs">
                  {d.latitude.toFixed(4)}, {d.longitude.toFixed(4)}
                </td>
                <td className="px-4 py-3">
                  <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_BADGE[d.status] ?? STATUS_BADGE.Offline}`}>
                    {d.status}
                  </span>
                </td>
                <td className="px-4 py-3">
                  <div className="flex items-center gap-1.5">
                    <input
                      type="number"
                      value={editThreshold[d.id] ?? d.alertThreshold}
                      onChange={(e) => setEditThreshold((prev) => ({ ...prev, [d.id]: e.target.value }))}
                      className="w-20 border border-gray-300 rounded px-2 py-0.5 text-xs focus:outline-none focus:ring-1 focus:ring-cyan-500"
                    />
                    <span className="text-xs text-gray-400">{d.unit}</span>
                    {editThreshold[d.id] !== undefined && (
                      <button
                        onClick={() => handleThresholdUpdate(d)}
                        className="text-xs text-cyan-600 hover:underline"
                      >
                        Save
                      </button>
                    )}
                  </div>
                </td>
                <td className="px-4 py-3 text-xs text-gray-400">
                  {new Date(d.lastSeen).toLocaleString()}
                </td>
                <td className="px-4 py-3" />
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
