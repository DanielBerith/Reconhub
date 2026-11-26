import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// The ASP.NET Core API (see src/ReconHub.Api/Properties/launchSettings.json).
const apiTarget = process.env.RECONHUB_API_URL ?? 'http://localhost:5080'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
      '/health': { target: apiTarget, changeOrigin: true },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    restoreMocks: true,
    unstubGlobals: true,
  },
})
