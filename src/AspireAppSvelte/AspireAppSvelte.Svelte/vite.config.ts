import tailwindcss from '@tailwindcss/vite';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig, loadEnv } from 'vite';

export default defineConfig(({ mode }) => {
	const env = loadEnv(mode, process.cwd(), '');

	return {
		plugins: [tailwindcss(), sveltekit()],
		server: {
			// Port dynamique configuré par Aspire
			port: parseInt(env.VITE_PORT) || 5173,
			host: true,
			cors: true,
			// Proxy pour rediriger les appels API
			proxy: {
				'/api': {
					// Utilise les variables d'environnement Aspire
					target: process.env.services__apiservice__https__0 ||
						process.env.services__apiservice__http__0 ||
						'https://localhost:7218',
					changeOrigin: true,
					rewrite: (path) => path.replace(/^\/api/, ''),
					secure: false,
				}
			}
		},
		preview: {
			port: parseInt(env.VITE_PREVIEW_PORT) || 4173,
			host: true
		}
	}
});