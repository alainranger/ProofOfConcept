#!/bin/bash
# scripts/setup.sh
# Script de setup initial pour le projet

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

echo "═══════════════════════════════════════════════════════════"
echo "  Setup ManageDeadlockPolly"
echo "═══════════════════════════════════════════════════════════"
echo ""

# Vérifications prérequis
echo "🔍 Vérification des prérequis..."

if ! command -v docker &> /dev/null; then
    echo "❌ Docker n'est pas installé"
    echo "   Installer: https://www.docker.com/products/docker-desktop"
    exit 1
fi
echo "✓ Docker OK"

if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose n'est pas installé"
    exit 1
fi
echo "✓ Docker Compose OK"

# Optionnel: .NET SDK (pour local build)
if ! command -v dotnet &> /dev/null; then
    echo "⚠ .NET SDK non détecté (optionnel si tu utilises Docker)"
else
    DOTNET_VERSION=$(dotnet --version)
    echo "✓ .NET SDK OK (version: $DOTNET_VERSION)"
fi

echo ""
echo "📦 Nettoyage des build antérieurs..."
cd "$PROJECT_DIR"

# Nettoyer les fichiers précédents
rm -rf bin obj dist 2>/dev/null || true
find . -name "*.log" -delete 2>/dev/null || true

echo "✓ Nettoyage terminé"

echo ""
echo "🐳 Construction des images Docker..."

docker-compose build --no-cache

if [ $? -eq 0 ]; then
    echo ""
    echo "═══════════════════════════════════════════════════════════"
    echo "  ✅ Setup réussi!"
    echo "═══════════════════════════════════════════════════════════"
    echo ""
    echo "Prochaines étapes:"
    echo ""
    echo "  1. Lancer l'application:"
    echo "     cd $PROJECT_DIR && docker-compose up --build"
    echo ""
    echo "  2. Ou directement:"
    echo "     docker-compose up"
    echo ""
    echo "  3. Dans un autre terminal, voir les logs:"
    echo "     docker-compose logs -f"
    echo ""
    echo "  4. Pour arrêter:"
    echo "     docker-compose down"
    echo ""
else
    echo ""
    echo "❌ Erreur lors du build Docker"
    exit 1
fi
