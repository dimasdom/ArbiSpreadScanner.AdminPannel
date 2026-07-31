import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
        },
    },
    test: {
        environment: 'jsdom',
        globals: true,
        setupFiles: ['./src/test/setup.ts'],
        // @mui/x-data-grid's ESM entry does a bare `import './index.css'`; it must go through
        // Vite's transform pipeline (which strips/handles CSS imports) instead of being treated
        // as an external Node module, or the import throws ERR_UNKNOWN_FILE_EXTENSION.
        server: {
            deps: {
                inline: ['@mui/x-data-grid'],
            },
        },
        coverage: {
            provider: 'v8',
            reporter: ['text', 'lcov'],
            all: true,
            include: ['src/**/*.{ts,tsx}'],
            exclude: ['src/**/*.test.{ts,tsx}', 'src/test/**', 'src/types/**', 'src/main.tsx', 'src/vite-env.d.ts'],
        },
    },
});
