import { useEffect, useState } from 'react'
import { MapContainer, TileLayer, CircleMarker, Popup } from 'react-leaflet'
import { api } from '../api/client'
import { useHub } from '../App'

const STATUS_COLOR = {
  Online: '#22c55e',
  Warning: '#f59e0b',
  Error: '#ef4444',
  Offline: '#6b7280',
}

export default function LiveMonitoringMap() {
  const [devices, setDevices] = useState([])
  const hub = useHub()

  useEffect(() => {
    api.get('/devices').then(setDevices).catch(console.error)
  }, [])

  // Real-time device status updates
  useEffect(() => {
    hub.on('DeviceStatusChanged', ({ deviceId, status, lastSeen }) => {
      setDevices((prev) =>
        prev.map((d) =>
          d.id === deviceId ? { ...d, status, lastSeen } : d
        )
      )
    })
    return () => hub.off('DeviceStatusChanged')
  }, [hub])

  return (
    <div className="h-[calc(100vh-49px)] flex flex-col">
      {/* Legend */}
      <div className="flex items-center gap-4 px-4 py-2 bg-white border-b border-gray-200 text-xs">
        {Object.entries(STATUS_COLOR).map(([label, color]) => (
          <span key={label} className="flex items-center gap-1.5">
            <span className="w-3 h-3 rounded-full inline-block" style={{ background: color }} />
            {label}
          </span>
        ))}
        <span className="ml-auto text-gray-400">{devices.length} devices</span>
      </div>

      <MapContainer
        center={[20, 0]}
        zoom={2}
        className="flex-1"
        style={{ background: '#1a1a2e' }}
      >
        <TileLayer
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        />
        {devices.map((device) => (
          <CircleMarker
            key={device.id}
            center={[device.latitude, device.longitude]}
            radius={8}
            pathOptions={{
              fillColor: STATUS_COLOR[device.status] ?? '#6b7280',
              fillOpacity: 0.9,
              color: '#fff',
              weight: 1.5,
            }}
          >
            <Popup>
              <div className="text-sm">
                <p className="font-semibold">{device.name}</p>
                <p className="text-gray-500 font-mono text-xs">{device.hardwareId}</p>
                <p className="mt-1">
                  Status:{' '}
                  <span style={{ color: STATUS_COLOR[device.status] }}>{device.status}</span>
                </p>
                <p>Threshold: {device.alertThreshold} {device.unit}</p>
                <p className="text-gray-400 text-xs mt-1">
                  Last seen: {new Date(device.lastSeen).toLocaleTimeString()}
                </p>
              </div>
            </Popup>
          </CircleMarker>
        ))}
      </MapContainer>
    </div>
  )
}
