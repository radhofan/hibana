import { useEffect, useState } from 'react'
import { api } from '../api/client'
import { useHub } from '../App'

const SEVERITY_STYLE = {
  Critical: 'bg-red-100 text-red-700 border-red-200',
  Warning: 'bg-amber-100 text-amber-700 border-amber-200',
  Info: 'bg-blue-100 text-blue-700 border-blue-200',
}

const SEVERITY_DOT = {
  Critical: 'bg-red-500',
  Warning: 'bg-amber-500',
  Info: 'bg-blue-500',
}

function AgentAnalysisPanel({ analysis }) {
  const [expanded, setExpanded] = useState(false)

  return (
    <div className="mt-2">
      <button
        onClick={() => setExpanded((v) => !v)}
        className="flex items-center gap-1 text-xs font-medium text-indigo-600 hover:text-indigo-800 transition-colors"
      >
        <span
          className={`inline-block transition-transform duration-200 ${expanded ? 'rotate-90' : ''}`}
        >
          ▶
        </span>
        AI Analysis
      </button>

      {expanded && (
        <div className="mt-2 space-y-1.5 text-xs">
          {/* Planner assessment — gray */}
          <div className="rounded-lg bg-gray-100 border border-gray-200 px-3 py-2">
            <span className="font-semibold text-gray-600 block mb-0.5">Planner:</span>
            <span className="text-gray-700 leading-relaxed">{analysis.plannerAssessment}</span>
          </div>

          {/* Reviewer critique — blue */}
          <div className="rounded-lg bg-blue-50 border border-blue-200 px-3 py-2">
            <span className="font-semibold text-blue-700 block mb-0.5">Reviewer:</span>
            <span className="text-blue-800 leading-relaxed">{analysis.reviewerCritique}</span>
          </div>

          {/* Recommended action — green */}
          <div className="rounded-lg bg-green-50 border border-green-200 px-3 py-2">
            <span className="font-semibold text-green-700 block mb-0.5">Action:</span>
            <span className="text-green-800 leading-relaxed">{analysis.recommendedAction}</span>
          </div>
        </div>
      )}
    </div>
  )
}

export default function AlertFeed({ onRead }) {
  const [alerts, setAlerts] = useState([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(0)
  const [loading, setLoading] = useState(false)
  const hub = useHub()
  const PAGE_SIZE = 20

  useEffect(() => {
    onRead?.()
    fetchPage(0)
  }, [])

  async function fetchPage(p) {
    setLoading(true)
    try {
      const data = await api.get(`/alerts?page=${p}&size=${PAGE_SIZE}`)
      setAlerts(data.items)
      setTotal(data.total)
      setPage(p)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  // New alerts pushed via SignalR
  useEffect(() => {
    hub.on('AlertTriggered', (alert) => {
      setAlerts((prev) => [alert, ...prev.slice(0, PAGE_SIZE - 1)])
      setTotal((n) => n + 1)
    })

    // AI analysis ready — enrich the matching alert in state
    hub.on('AgentAnalysisReady', (payload) => {
      setAlerts((prev) =>
        prev.map((a) =>
          a.id === payload.alertId
            ? {
                ...a,
                agentAnalysis: {
                  plannerAssessment: payload.plannerAssessment,
                  reviewerCritique: payload.reviewerCritique,
                  recommendedAction: payload.recommendedAction,
                },
              }
            : a
        )
      )
    })

    return () => {
      hub.off('AlertTriggered')
      hub.off('AgentAnalysisReady')
    }
  }, [hub])

  async function acknowledge(id) {
    await api.post(`/alerts/${id}/acknowledge`)
    setAlerts((prev) => prev.map((a) => a.id === id ? { ...a, isAcknowledged: true } : a))
  }

  const totalPages = Math.ceil(total / PAGE_SIZE)

  return (
    <div className="max-w-3xl mx-auto p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-900">Alert Feed</h1>
        <span className="text-sm text-gray-400">{total} total alerts</span>
      </div>

      {loading ? (
        <p className="text-center text-gray-400 py-10">Loading…</p>
      ) : alerts.length === 0 ? (
        <p className="text-center text-gray-400 py-10">No alerts.</p>
      ) : (
        <div className="space-y-2">
          {alerts.map((alert) => (
            <div
              key={alert.id}
              className={`border rounded-xl p-4 flex items-start gap-3 transition-opacity ${
                alert.isAcknowledged ? 'opacity-50' : ''
              } ${SEVERITY_STYLE[alert.severity] ?? SEVERITY_STYLE.Info}`}
            >
              <span className={`mt-1 w-2.5 h-2.5 rounded-full flex-shrink-0 ${SEVERITY_DOT[alert.severity] ?? 'bg-blue-500'}`} />
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="font-semibold text-sm">{alert.deviceName}</span>
                  <span className="text-xs px-1.5 py-0.5 rounded bg-white/60 font-medium">
                    {alert.severity}
                  </span>
                  {alert.isAcknowledged && (
                    <span className="text-xs text-gray-400">Acknowledged</span>
                  )}
                </div>
                <p className="text-sm mt-0.5">{alert.message}</p>
                <p className="text-xs mt-1 opacity-70">
                  {new Date(alert.triggeredAt).toLocaleString()}
                </p>

                {/* AI Analysis expandable panel */}
                {alert.agentAnalysis && (
                  <AgentAnalysisPanel analysis={alert.agentAnalysis} />
                )}
              </div>
              {!alert.isAcknowledged && (
                <button
                  onClick={() => acknowledge(alert.id)}
                  className="flex-shrink-0 text-xs underline opacity-70 hover:opacity-100"
                >
                  Ack
                </button>
              )}
            </div>
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <div className="flex items-center justify-end gap-2 pt-2">
          <button
            onClick={() => fetchPage(Math.max(0, page - 1))}
            disabled={page === 0}
            className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50"
          >
            Previous
          </button>
          <span className="text-sm text-gray-500">
            Page {page + 1} of {totalPages}
          </span>
          <button
            onClick={() => fetchPage(Math.min(totalPages - 1, page + 1))}
            disabled={page >= totalPages - 1}
            className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50"
          >
            Next
          </button>
        </div>
      )}
    </div>
  )
}
