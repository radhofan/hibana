const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000/api/v1'

async function request<T>(path: string): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`)
  if (!response.ok) {
    const problem = await response.json().catch(() => ({})) as { detail?: string }
    throw new Error(problem.detail ?? 'The request could not be completed.')
  }
  return response.json() as Promise<T>
}

export const getWeather = (latitude: number, longitude: number, units: 'metric' | 'imperial') =>
  request<any>(`/weather?latitude=${latitude}&longitude=${longitude}&hourlyHours=24&dailyDays=7&units=${units}`)

export const searchLocations = (query: string) =>
  request<any[]>(`/locations/search?query=${encodeURIComponent(query)}`)
