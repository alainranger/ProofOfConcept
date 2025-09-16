import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig, loadEnv } from 'vite';

export default defineConfig(({ mode }) => {
	const env = loadEnv(mode, process.cwd(), '');

	return {
		plugins: [sveltekit()],
		server: {
			port: parseInt(env.VITE_PORT)
		},
		build: {
			outDir: 'dist',
			rollupOptions: {
				input: './index.html'
			}
		}
	}
})