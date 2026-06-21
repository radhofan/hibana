import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  define: { CESIUM_BASE_URL: JSON.stringify('/cesium') },
  server: { proxy: { '/api': 'http://localhost:5000' } },
})
