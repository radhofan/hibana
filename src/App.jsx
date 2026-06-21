import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Cartesian3, Color, EllipsoidTerrainProvider, ScreenSpaceEventHandler, ScreenSpaceEventType, Viewer } from 'cesium'
import 'cesium/Build/Cesium/Widgets/widgets.css'
import { Area, AreaChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getWeather, searchLocations } from './api/client.ts'

const weatherIcons = { clear: '☀', 'mostly-clear': '☀', 'partly-cloudy': '◒', overcast: '☁', fog: '≋', drizzle: '☂', rain: '☂', snow: '❄', thunderstorm: 'ϟ' }
const metric = (weather, imperial) => imperial ? weather?.units?.temperature === 'fahrenheit' : weather?.units?.temperature === 'celsius'

function Globe({ point, compare, onPick }) {
  const element = useRef(null); const viewer = useRef(null)
  useEffect(() => {
    const instance = new Viewer(element.current, { animation: false, baseLayerPicker: false, fullscreenButton: false, geocoder: false, homeButton: false, infoBox: false, navigationHelpButton: false, sceneModePicker: false, selectionIndicator: false, timeline: false, terrainProvider: new EllipsoidTerrainProvider() })
    instance.scene.globe.baseColor = Color.fromCssColorString('#102f50'); viewer.current = instance
    const handler = new ScreenSpaceEventHandler(instance.scene.canvas)
    handler.setInputAction(event => { const cartesian = instance.camera.pickEllipsoid(event.position, instance.scene.globe.ellipsoid); if (cartesian) { const cartographic = instance.scene.globe.ellipsoid.cartesianToCartographic(cartesian); onPick({ lat: Cartesian3 ? cartographic.latitude * 180 / Math.PI : 0, lng: cartographic.longitude * 180 / Math.PI }) } }, ScreenSpaceEventType.LEFT_CLICK)
    return () => { handler.destroy(); instance.destroy() }
  }, [onPick])
  useEffect(() => { const instance = viewer.current; if (!instance) return; instance.camera.flyTo({ destination: Cartesian3.fromDegrees(point.lng, point.lat, 12000000), duration: 1.2 }); instance.entities.removeAll(); ;[point, ...compare.map(item => ({ lat: item.coordinates.latitude, lng: item.coordinates.longitude }))].forEach((item, index) => instance.entities.add({ position: Cartesian3.fromDegrees(item.lng, item.lat), point: { pixelSize: index ? 9 : 13, color: index ? Color.CYAN : Color.GOLD, outlineColor: Color.WHITE, outlineWidth: 2 } })) }, [point, compare])
  return <div ref={element} className="world-map" aria-label="Interactive 3D globe" />
}

function conditionIcon(condition) { return weatherIcons[condition?.iconKey] ?? '◌' }
function formatTime(value) { return value ? new Intl.DateTimeFormat([], { hour: 'numeric', minute: '2-digit' }).format(new Date(value)) : '—' }
function formatDay(value) { return new Intl.DateTimeFormat([], { weekday: 'short', month: 'short', day: 'numeric' }).format(new Date(value)) }

export default function App() {
  const [point, setPoint] = useState({ lat: -6.9175, lng: 107.6191 })
  const [unit, setUnit] = useState(() => sessionStorage.getItem('hibana-unit') ?? 'metric')
  const [search, setSearch] = useState('')
  const [compare, setCompare] = useState(() => JSON.parse(sessionStorage.getItem('hibana-compare') ?? '[]'))
  const [recent, setRecent] = useState(() => JSON.parse(localStorage.getItem('hibana-recent') ?? '[]'))

  const weatherQuery = useQuery({ queryKey: ['weather', point.lat, point.lng, unit], queryFn: () => getWeather(point.lat, point.lng, unit), retry: 1 })
  const searchQuery = useQuery({ queryKey: ['locations', search.trim()], queryFn: () => searchLocations(search.trim()), enabled: false, retry: 1 })
  const weather = weatherQuery.data
  const results = searchQuery.data ?? []
  const status = weatherQuery.isPending ? 'loading' : weatherQuery.isError ? 'error' : 'ready'
  const error = weatherQuery.error instanceof Error ? weatherQuery.error.message : 'Weather data is temporarily unavailable.'

  useEffect(() => { if (weather) setRecent(items => [{ name: weather.location.name, displayName: weather.location.displayName, lat: point.lat, lng: point.lng }, ...items.filter(item => item.displayName !== weather.location.displayName)].slice(0, 5)) }, [weather, point.lat, point.lng])
  useEffect(() => { sessionStorage.setItem('hibana-unit', unit) }, [unit])
  useEffect(() => { sessionStorage.setItem('hibana-compare', JSON.stringify(compare)) }, [compare])
  useEffect(() => { localStorage.setItem('hibana-recent', JSON.stringify(recent)) }, [recent])

  const pick = useCallback(location => setPoint({ lat: Number(location.lat.toFixed(4)), lng: Number(location.lng.toFixed(4)) }), [])
  const searchCity = async event => {
    event.preventDefault(); if (search.trim().length < 2) return
    await searchQuery.refetch()
  }
  const choose = item => { pick({ lat: item.latitude, lng: item.longitude }); setSearch(item.name) }
  const addCompare = () => weather && setCompare(items => items.some(item => item.location.displayName === weather.location.displayName) || items.length === 4 ? items : [...items, weather])
  const hourly = useMemo(() => weather?.hourly?.slice(0, 24).map(item => ({ ...item, label: formatTime(item.time) })) ?? [], [weather])

  return <main className="app-shell">
    <Globe point={point} compare={compare} onPick={pick} />
    <div className="atmosphere" />
    <header className="topbar"><div><span className="eyebrow">GLOBAL WEATHER EXPLORER</span><h1>hibana</h1></div><p>Click anywhere to explore weather</p><button className="unit-toggle" onClick={() => setUnit(value => value === 'metric' ? 'imperial' : 'metric')}>{unit === 'metric' ? '°C / km/h' : '°F / mph'}</button></header>
    <section className="search-card"><form onSubmit={searchCity}><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search a city" aria-label="Search a city" /><button>Search</button></form>{results.length > 0 && <div className="search-results">{results.map(item => <button key={`${item.latitude}-${item.longitude}`} onClick={() => choose(item)}><strong>{item.name}</strong><span>{[item.region, item.country].filter(Boolean).join(', ')}</span></button>)}</div>}</section>
    {recent.length > 0 && <section className="recent-card"><span className="eyebrow">RECENT LOCATIONS</span>{recent.map(item => <button key={item.displayName} onClick={() => pick(item)}>{item.name}</button>)}</section>}
    <aside className="weather-panel">
      {status === 'loading' && <div className="skeleton"><i /><i /><i /><i /></div>}
      {status === 'error' && <div className="error-state"><span>Weather unavailable</span><p>{error}</p><button onClick={() => weatherQuery.refetch()}>Try again</button></div>}
      {status === 'ready' && weather && <>
        <div className="location-row"><div><span className="eyebrow">SELECTED LOCATION</span><h2>{weather.location.displayName}</h2><p>{weather.location.localTime ? `Local time ${formatTime(weather.location.localTime)}` : `${point.lat.toFixed(4)}°, ${point.lng.toFixed(4)}°`}</p></div><button className="compare-button" onClick={addCompare}>+ Compare</button></div>
        <div className="hero-weather"><span>{conditionIcon(weather.current.condition)}</span><strong>{Math.round(weather.current.temperature)}°</strong><div><b>{weather.current.condition.label}</b><p>Feels like {Math.round(weather.current.feelsLike)}°</p></div></div>
        <div className="metrics"><Metric label="Humidity" value={`${weather.current.humidityPercent}%`} /><Metric label="Wind" value={`${Math.round(weather.current.wind.speed)} ${weather.units.windSpeed}`} /><Metric label="Pressure" value={`${Math.round(weather.current.pressure)} hPa`} /><Metric label="Visibility" value={`${Math.round(weather.current.visibility / 1000)} km`} /><Metric label="Cloud cover" value={`${weather.current.cloudCoverPercent}%`} /><Metric label="Sunset" value={formatTime(weather.sun.sunset)} /></div>
        <section><h3>Next 24 hours</h3><div className="chart"> <ResponsiveContainer width="100%" height={140}><AreaChart data={hourly}><defs><linearGradient id="temperature" x1="0" x2="0" y1="0" y2="1"><stop offset="0%" stopColor="#9cd8ff" stopOpacity={.7}/><stop offset="100%" stopColor="#9cd8ff" stopOpacity={0}/></linearGradient></defs><XAxis dataKey="label" tick={{ fill: '#9bacbf', fontSize: 10 }} interval={5} axisLine={false} tickLine={false}/><YAxis hide domain={['dataMin - 2','dataMax + 2']}/><Tooltip contentStyle={{background:'#102033',border:'1px solid #294763',borderRadius:10}} /><Area type="monotone" dataKey="temperature" stroke="#d8f0ff" fill="url(#temperature)" strokeWidth={2}/></AreaChart></ResponsiveContainer></div><div className="hourly-list">{hourly.map(item => <div key={item.time}><span>{item.label}</span><b>{conditionIcon(item.condition)} {Math.round(item.temperature)}°</b><small>{item.precipitationProbability ?? 0}% rain · {Math.round(item.windSpeed ?? 0)} {weather.units.windSpeed}</small></div>)}</div></section>
        <section><h3>7-day outlook</h3><div className="daily-list">{weather.daily.map(day => <div key={day.date}><span>{formatDay(day.date)}</span><b>{conditionIcon(day.condition)} {day.condition.label}</b><span>{Math.round(day.minimumTemperature)}° <em>{Math.round(day.maximumTemperature)}°</em></span><small>{day.precipitationProbability ?? 0}% rain · ↑ {formatTime(day.sunrise)} · ↓ {formatTime(day.sunset)}</small></div>)}</div></section>
      </>}
    </aside>
    {compare.length > 0 && <section className="comparison"><div><span className="eyebrow">SESSION COMPARISON</span><button onClick={() => setCompare([])}>Clear</button></div>{compare.map(item => <article key={item.location.displayName}><span>{conditionIcon(item.current.condition)}</span><b>{item.location.name}</b><strong>{Math.round(item.current.temperature)}°</strong></article>)}</section>}
  </main>
}

function Metric({ label, value }) { return <div><span>{label}</span><b>{value}</b></div> }
