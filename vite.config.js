import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import Inspect from "vite-plugin-inspect"


export default defineConfig({
  root: './src',
  publicDir: './public',
  build: {
    outDir: '../dist',
    chunkSizeWarningLimit: 600,
    emptyOutDir: true
  },
  server: {
    port: 3000,
    strictPort: true,
    host: 'localhost',
    open: true,
    hmr: {
      protocol: 'ws',
      host: 'localhost'
    }
  },
  plugins: [
    Inspect(),
    react({ include: /\.(fs|js|jsx|ts|tsx)$/, jsxRuntime: "automatic" })
  ],
  optimizeDeps: {
    include: ['react', 'react-dom']
  }
});