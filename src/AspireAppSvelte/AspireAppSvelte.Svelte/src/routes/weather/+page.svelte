<script lang="ts">
	import { onMount } from 'svelte';
	import { createApiClient, type WeatherForecast } from '$lib';

	let forecasts: WeatherForecast[] | null = null;
	let loading = true;
	let error: string | null = null;

	onMount(async () => {
		try {
			const weatherApi = createApiClient();
			forecasts = await weatherApi.getWeatherAsync();
		} catch (err) {
			error = err instanceof Error ? err.message : 'An error occurred';
			console.error('Failed to load weather data:', err);
		} finally {
			loading = false;
		}
	});

	function formatDate(dateString: string): string {
		return new Date(dateString).toLocaleDateString();
	}
</script>

<svelte:head>
	<title>Weather</title>
</svelte:head>

<div class="container mx-auto px-4 py-8">
	<h1 class="mb-4 text-3xl font-bold text-gray-900">Weather</h1>

	<p class="mb-6 text-gray-600">
		This component demonstrates showing data loaded from a backend API service.
	</p>

	{#if loading}
		<div class="flex items-center justify-center py-8">
			<div class="h-8 w-8 animate-spin rounded-full border-b-2 border-blue-600"></div>
			<span class="ml-2 text-gray-600">Loading...</span>
		</div>
	{:else if error}
		<div class="mb-4 rounded-md border border-red-200 bg-red-50 p-4">
			<p class="text-red-800">Error loading weather data: {error}</p>
		</div>
	{:else if forecasts}
		<div class="overflow-x-auto">
			<table class="min-w-full rounded-lg border border-gray-200 bg-white shadow">
				<thead class="bg-gray-50">
					<tr>
						<th
							class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
						>
							Date
						</th>
						<th
							class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
							aria-label="Temperature in Celsius"
						>
							Temp. (C)
						</th>
						<th
							class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
							aria-label="Temperature in Fahrenheit"
						>
							Temp. (F)
						</th>
						<th
							class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
						>
							Summary
						</th>
					</tr>
				</thead>
				<tbody class="divide-y divide-gray-200">
					{#each forecasts as forecast (forecast.date)}
						<tr class="hover:bg-gray-50">
							<td class="px-6 py-4 text-sm whitespace-nowrap text-gray-900">
								{formatDate(forecast.date)}
							</td>
							<td class="px-6 py-4 text-sm whitespace-nowrap text-gray-900">
								{forecast.temperatureC}
							</td>
							<td class="px-6 py-4 text-sm whitespace-nowrap text-gray-900">
								{forecast.temperatureF}
							</td>
							<td class="px-6 py-4 text-sm whitespace-nowrap text-gray-900">
								{forecast.summary || 'N/A'}
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</div>
