import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The .NET API runs under the "https" launch profile at https://localhost:7105.
// We proxy /api there so the browser talks same-origin to the Vite dev server
// (no CORS, and bearer tokens flow straight through). secure:false accepts the
// ASP.NET dev certificate. Override with VITE_API_TARGET if your port differs.
const API_TARGET = process.env.VITE_API_TARGET || 'https://localhost:7105'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: API_TARGET,
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
