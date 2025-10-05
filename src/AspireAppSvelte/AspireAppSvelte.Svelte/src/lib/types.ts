export interface ApiConfig {
	baseUrl: string;
}

export interface WeatherForecast {
	date: string;
	temperatureC: number;
	temperatureF: number;
	summary: string | null;
}
