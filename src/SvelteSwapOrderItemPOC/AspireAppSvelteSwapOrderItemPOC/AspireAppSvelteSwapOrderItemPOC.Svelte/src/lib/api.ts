const API_BASE = '/api'; // Utilise le proxy Vite

// Types pour les options de fetch
interface ApiOptions extends RequestInit {
    headers?: Record<string, string>;
}

// Type générique pour les réponses API
interface ApiResponse<T = unknown> {
    data?: T;
    error?: string;
    success: boolean;
}

export async function apiCall<T = unknown>(endpoint: string, options: ApiOptions = {}): Promise<T> {
    const url = `${API_BASE}${endpoint}`;

    const config: RequestInit = {
        headers: {
            'Content-Type': 'application/json',
            ...options.headers,
        },
        ...options,
    };

    try {
        const response = await fetch(url, config);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.json() as T;
    } catch (error) {
        console.error('API call failed:', error);
        throw error;
    }
}

// Méthodes spécifiques avec types
export const api = {
    get: <T = unknown>(endpoint: string): Promise<T> =>
        apiCall<T>(endpoint),

    post: <T = unknown>(endpoint: string, data: unknown): Promise<T> =>
        apiCall<T>(endpoint, {
            method: 'POST',
            body: JSON.stringify(data),
        }),

    put: <T = unknown>(endpoint: string, data: unknown): Promise<T> =>
        apiCall<T>(endpoint, {
            method: 'PUT',
            body: JSON.stringify(data),
        }),

    patch: <T = unknown>(endpoint: string, data: unknown): Promise<T> =>
        apiCall<T>(endpoint, {
            method: 'PATCH',
            body: JSON.stringify(data),
        }),

    delete: <T = unknown>(endpoint: string): Promise<T> =>
        apiCall<T>(endpoint, {
            method: 'DELETE',
        }),
};

// Types d'exemple pour votre application (à adapter selon vos besoins)
export interface ApiError {
    message: string;
    statusCode: number;
    details?: unknown;
}

export interface PaginatedResponse<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
}