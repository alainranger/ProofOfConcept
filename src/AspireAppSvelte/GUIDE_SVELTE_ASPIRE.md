# Guide Complet : Intégrer une Application Svelte dans .NET Aspire

Ce guide détaille comment intégrer avec succès une application Svelte + TypeScript + Tailwind CSS dans un projet .NET Aspire, basé sur une implémentation réussie.

## 📋 Table des Matières

1. [Prérequis](#prérequis)
2. [Architecture de la Solution](#architecture-de-la-solution)
3. [Étape 1 : Création du Projet Svelte](#étape-1--création-du-projet-svelte)
4. [Étape 2 : Configuration Vite pour Aspire](#étape-2--configuration-vite-pour-aspire)
5. [Étape 3 : Configuration Aspire AppHost](#étape-3--configuration-aspire-apphost)
6. [Étape 4 : Client API et Communication](#étape-4--client-api-et-communication)
7. [Étape 5 : Démarrage et Tests](#étape-5--démarrage-et-tests)
8. [Bonnes Pratiques](#bonnes-pratiques)
9. [Dépannage](#dépannage)
10. [Déploiement](#déploiement)

---

## Prérequis

### Outils Requis

- **.NET 9.0 SDK** ou plus récent
- **Node.js 20+** avec npm
- **Visual Studio Code** ou Visual Studio 2022
- **.NET Aspire** workload installé

### Vérification

```bash
# Vérifier .NET
dotnet --version

# Vérifier Node.js
node --version
npm --version

# Vérifier Aspire
dotnet workload list
```

---

## Architecture de la Solution

```text
Solution/
├── AppHost/                    # Orchestrateur Aspire
├── ApiService/                 # API Backend (.NET)
└── Svelte/                     # Frontend Svelte + TypeScript
    ├── src/
    │   ├── lib/
    │   │   ├── types.ts       # Types partagés
    │   │   └── apiClient.ts   # Client API
    │   └── routes/
    │       ├── +layout.svelte # Layout principal
    │       └── +page.svelte   # Pages
    ├── vite.config.ts         # Configuration Vite + Proxy
    ├── package.json
    └── Dockerfile
```

---

## Étape 1 : Création du Projet Svelte

### 1.1 Initialisation du Projet

```bash
# Dans le dossier racine de votre solution
npx sv create AppName.Svelte --template minimal --types ts
cd AppName.Svelte

# Sélectionner lors de la création :
# ✅ TypeScript
# ✅ Tailwind CSS
# ✅ ESLint + Prettier (optionnel)
```

### 1.2 Structure des Dossiers

```text
src/
├── lib/
│   ├── types.ts              # Types TypeScript partagés
│   ├── apiClient.ts          # Client API
│   └── index.ts              # Exports
├── routes/
│   ├── +layout.svelte        # Layout avec navigation
│   ├── +page.svelte          # Page d'accueil
│   └── feature/
│       └── +page.svelte      # Pages fonctionnelles
└── app.html                  # Template HTML
```

### 1.3 Configuration package.json

```json
{
  "name": "appname.svelte",
  "scripts": {
    "dev": "vite dev",
    "build": "vite build",
    "preview": "vite preview"
  },
  "devDependencies": {
    "@sveltejs/kit": "^2.43.2",
    "svelte": "^5.39.5",
    "typescript": "^5.9.2",
    "vite": "^7.1.7",
    "tailwindcss": "^4.1.13"
  }
}
```

---

## Étape 2 : Configuration Vite pour Aspire

### 2.1 vite.config.ts - Configuration Complète

```typescript
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
```

### 2.2 Variables d'Environnement

Créer `.env` :

```env
# Développement local
VITE_API_BASE_URL=https://localhost:7218
VITE_PORT=5173
```

Créer `.env.example` :

```env
# Configuration exemple
VITE_API_BASE_URL=https://localhost:7218
VITE_PORT=5173
```

---

## Étape 3 : Configuration Aspire AppHost

### 3.1 Installation du Package NuGet

```bash
# Dans le projet AppHost
dotnet add package Aspire.Hosting.NodeJs
```

### 3.2 Configuration AppHost.cs

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Service API Backend
var apiService = builder.AddProject<Projects.AppName_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

// Service Web Blazor (optionnel)
builder.AddProject<Projects.AppName_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

// Service Svelte Frontend
var svelteApp = builder.AddExecutable("svelte-frontend", "npm", "../AppName.Svelte", "run", "dev")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
```

### 3.3 Points Clés de Configuration

- **`WithReference(apiService)`** : Crée la relation de dépendance
- **`WaitFor(apiService)`** : Assure que l'API démarre avant Svelte
- **`WithHttpEndpoint(env: "VITE_PORT")`** : Configure le port dynamiquement
- **`WithExternalHttpEndpoints()`** : Expose le service à l'extérieur
- **`PublishAsDockerFile()`** : Active le déploiement Docker

---

## Étape 4 : Client API et Communication

### 4.1 Types TypeScript (src/lib/types.ts)

```typescript
export interface WeatherForecast {
    date: string;
    temperatureC: number;
    temperatureF: number;
    summary: string | null;
}

export interface ApiResponse<T> {
    data: T;
    success: boolean;
    message?: string;
}
```

### 4.2 Client API (src/lib/apiClient.ts)

```typescript
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
```

### 4.3 Utilisation dans les Composants Svelte

```svelte
<script lang="ts">
    import { onMount } from 'svelte';
    import { createApiClient, type WeatherForecast } from '$lib';
    
    let forecasts: WeatherForecast[] | null = null;
    let loading = true;
    let error: string | null = null;

    onMount(async () => {
        try {
            const apiClient = createApiClient();
            forecasts = await apiClient.getWeatherAsync();
        } catch (err) {
            error = err instanceof Error ? err.message : 'An error occurred';
        } finally {
            loading = false;
        }
    });
</script>

{#if loading}
    <div class="flex items-center justify-center py-8">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        <span class="ml-2">Loading...</span>
    </div>
{:else if error}
    <div class="bg-red-50 border border-red-200 rounded-md p-4">
        <p class="text-red-800">Error: {error}</p>
    </div>
{:else if forecasts}
    <!-- Affichage des données -->
{/if}
```

---

## Étape 5 : Démarrage et Tests

### 5.1 Démarrage via Aspire (Recommandé)

```bash
# Dans le dossier AppHost
dotnet run
```

### 5.2 Démarrage Manuel (Développement)

```bash
# Terminal 1 : API + Dashboard Aspire
cd AppHost
dotnet run

# Terminal 2 : Frontend Svelte
cd AppName.Svelte
npm run dev
```

### 5.3 URLs de Test

- **Dashboard Aspire** : <https://localhost:17121>
- **API Backend** : <https://localhost:xxxx> (port généré par Aspire)
- **Frontend Svelte** : <http://localhost:5173>

### 5.4 Vérifications

1. **Console du navigateur** : Vérifier les logs de l'API client
2. **Network tab** : Confirmer les appels vers `/api/endpoint`
3. **Dashboard Aspire** : Vérifier le statut des services
4. **Variables d'environnement** : Confirmer la propagation des ports

---

## Bonnes Pratiques

### 🎯 Architecture

- **Séparation des préoccupations** : Types, clients API, composants
- **Configuration centralisée** : Variables d'environnement dans Aspire
- **Gestion d'erreurs** : Try-catch avec logging approprié
- **TypeScript strict** : Types explicites partout

### 🔒 Sécurité

- **Variables d'environnement** pour URLs sensibles
- **Validation côté client** avant envoi à l'API
- **CORS configuré** correctement
- **HTTPS en production**

### ⚡ Performance

- **Lazy loading** des composants Svelte
- **Optimisation Vite** pour le build de production
- **Mise en cache** appropriée des requêtes
- **Bundle splitting** automatique

### 🎨 Accessibilité

- **Labels ARIA** appropriés
- **Navigation au clavier** fonctionnelle
- **Structure sémantique** HTML
- **Indicateurs de chargement** visuels

---

## Dépannage

### ❌ Problème : Port non détecté par Aspire

**Symptôme** : Erreur "service-producer annotation is invalid"

**Solution** :

```csharp
// Dans AppHost.cs
.WithHttpEndpoint(env: "VITE_PORT")  // Au lieu de port fixe
```

### ❌ Problème : Appels API échouent

**Symptôme** : 404 ou CORS errors

**Solutions** :

1. Vérifier le nom du service dans vite.config.ts :

   ```typescript
   target: process.env.services__apiservice__https__0  // Nom exact du service
   ```

2. Vérifier le proxy Vite :

   ```typescript
   '/api': {
       target: 'URL_CORRECTE',
       changeOrigin: true,
       rewrite: (path) => path.replace(/^\/api/, '')
   }
   ```

### ❌ Problème : Variables d'environnement non propagées

**Solution** : Vérifier la configuration Aspire :

```csharp
.WithReference(apiService)  // Crée les variables services__*
```

### ❌ Problème : Build Docker échoue

**Solution** : Créer `.dockerignore` :

```text
node_modules
.env
.env.*
!.env.example
.git
```

---

## Déploiement

### 🐳 Dockerfile Optimisé

```dockerfile
# Build stage
FROM node:21-alpine3.19 AS builder

# Security updates
RUN apk update && apk upgrade --no-cache

WORKDIR /app

# Copy package files
COPY package*.json ./
RUN npm ci

# Copy source code
COPY . .
RUN npm run build

# Production stage
FROM node:21-alpine3.19 AS runner

# Security updates
RUN apk update && apk upgrade --no-cache && apk add --no-cache curl

# Create non-root user
RUN addgroup -g 1001 -S nodejs && adduser -S svelte -u 1001

WORKDIR /app

# Copy built application
COPY --from=builder /app/build ./build
COPY --from=builder /app/package*.json ./

# Install production dependencies
RUN npm ci --omit=dev

# Switch to non-root user
USER svelte

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:3000/ || exit 1

EXPOSE 3000
CMD ["node", "build"]
```

### ☁️ Déploiement Azure

```bash
# Publier avec Aspire
dotnet publish --os linux --arch x64 /t:PublishContainer
```

---

## 📚 Ressources Supplémentaires

### Documentation Officielle

- [.NET Aspire Documentation](https://learn.microsoft.com/aspire/)
- [Svelte Documentation](https://svelte.dev/docs)
- [SvelteKit Documentation](https://kit.svelte.dev/docs)
- [Vite Configuration](https://vitejs.dev/config/)

### Outils Utiles

- **VS Code Extensions** : Svelte for VS Code, Tailwind CSS IntelliSense
- **Debugging** : Browser DevTools, VS Code debugger
- **Testing** : Playwright (inclus dans le template Svelte)

---

## 🎉 Conclusion

Cette approche permet d'intégrer efficacement Svelte dans l'écosystème .NET Aspire avec :

- ✅ **Configuration automatique** des ports et variables d'environnement
- ✅ **Communication transparente** avec les APIs backend
- ✅ **Dashboard centralisé** pour monitoring
- ✅ **Déploiement unifié** via Docker
- ✅ **Expérience de développement** optimale

L'intégration tire parti des forces de chaque technologie tout en maintenant la cohérence architecturale de la solution .NET Aspire.

---

## Note de Version

Guide créé le 4 octobre 2025 - Basé sur .NET Aspire 9.5.1 et Svelte 5
