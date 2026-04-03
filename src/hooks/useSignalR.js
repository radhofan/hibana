import { useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'

export function useSignalR(url) {
  const connectionRef = useRef(null)
  const [connected, setConnected] = useState(false)
  const handlersRef = useRef({})

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connectionRef.current = connection

    // Re-attach any registered handlers after reconnect
    connection.onreconnected(() => {
      Object.entries(handlersRef.current).forEach(([event, fn]) =>
        connection.on(event, fn))
      setConnected(true)
    })

    connection.onclose(() => setConnected(false))

    connection.start()
      .then(() => setConnected(true))
      .catch((err) => console.error('SignalR connect error:', err))

    return () => {
      connection.stop()
    }
  }, [url])

  function on(event, handler) {
    handlersRef.current[event] = handler
    connectionRef.current?.on(event, handler)
  }

  function off(event) {
    delete handlersRef.current[event]
    connectionRef.current?.off(event)
  }

  return { connected, on, off }
}
