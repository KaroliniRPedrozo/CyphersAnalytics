import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import viteCompression from 'vite-plugin-compression'

export default defineConfig({
  plugins: [
    react(),
    // Gera versões .gz e .br dos seus arquivos para o navegador baixar muito mais rápido
    viteCompression({ algorithm: 'brotliCompress' }),
    viteCompression({ algorithm: 'gzip' })
  ],
  build: {
    // Reduz ainda mais o tamanho descartando logs em produção
    minify: 'esbuild',
    target: 'esnext',
  }
})