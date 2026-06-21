const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000/api/v1'
async function request(path) { const response = await fetch(`${baseUrl}${path}`); if (!response.ok) { const problem = await response.json().catch(() => ({})); throw new Error(problem.detail ?? 'The request could not be completed.'); } return response.json() }
export const getWeather = (latitude, longitude, units) => request(`/weather?latitude=${latitude}&longitude=${longitude}&hourlyHours=24&dailyDays=7&units=${units}`)
export const searchLocations = query => request(`/locations/search?query=${encodeURIComponent(query)}`)
