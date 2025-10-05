<script lang="ts">
	import '../app.css';
	import favicon from '$lib/assets/favicon.svg';
	import { page } from '$app/stores';

	let { children } = $props();

	const currentPath = $derived($page.url.pathname);
	const navItems = [
		{ href: '/', label: 'Home', icon: '🏠' },
		{ href: '/weather', label: 'Weather', icon: '🌤️' }
	];

	function isActivePath(href: string): boolean {
		return href === '/' ? currentPath === '/' : currentPath.startsWith(href);
	}
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

<div class="min-h-screen bg-gray-50">
	<nav class="border-b bg-white shadow-sm">
		<div class="container mx-auto px-4">
			<div class="flex h-16 items-center justify-between">
				<div class="flex items-center space-x-8">
					<a href="/" class="text-xl font-bold text-gray-900"> AspireApp 💗 Svelte </a>

					<div class="hidden space-x-4 md:flex">
						{#each navItems as item}
							<a
								href={item.href}
								class="rounded-md px-3 py-2 text-sm font-medium transition-colors duration-200 {isActivePath(
									item.href
								)
									? 'bg-blue-100 text-blue-700'
									: 'text-gray-700 hover:bg-gray-100 hover:text-blue-600'}"
								aria-current={isActivePath(item.href) ? 'page' : undefined}
							>
								<span class="mr-2">{item.icon}</span>
								{item.label}
							</a>
						{/each}
					</div>
				</div>
			</div>
		</div>
	</nav>

	<main class="flex-1">
		{@render children?.()}
	</main>

	<footer class="mt-auto border-t bg-white">
		<div class="container mx-auto px-4 py-4">
			<p class="text-center text-sm text-gray-600">
				© 2025 AspireApp2 Svelte - Built with SvelteKit & Tailwind CSS
			</p>
		</div>
	</footer>
</div>

<style>
	:global(html, body) {
		height: 100%;
	}
</style>
