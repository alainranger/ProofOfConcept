import type { WeatherForecast } from '$lib/types';

export class ApiClient {
    private baseUrl: string;

    constructor(baseUrl: string) {
        this.baseUrl = baseUrl;
    }

    async getWeatherAsync(maxItems = 10): Promise<WeatherForecast[]> {
        try {
            const url = `${this.baseUrl}/weatherforecast`;
            console.log('🌤️ Fetching weather from:', url);

            const response = await fetch(url);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data: WeatherForecast[] = await response.json();
            console.log('🌤️ Weather data received:', data.length, 'forecasts');
            return data.slice(0, maxItems);
        } catch (error) {
            console.error('❌ Error fetching weather data:', error);
            throw error;
        }
    }
}

// Factory pour créer le client avec la bonne configuration
export const createApiClient = () => {
    // En développement, utilise le proxy Vite (/api)
    // En production, utilise l'URL de l'API directement
    const baseUrl = import.meta.env.DEV
        ? '/api'  // Utilise le proxy Vite
        : (import.meta.env.VITE_API_BASE_URL || 'https://localhost:7218');

    return new ApiClient(baseUrl);
};