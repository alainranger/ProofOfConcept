#!/bin/bash
# scripts/dev.sh
# Utilitaires pour développement et debugging

set -e

PROJECT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )/.." && pwd )"

# Couleurs pour output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_help() {
    cat << EOF
${BLUE}ManageDeadlockPolly - Utilitaires de Développement${NC}

Usage: ./scripts/dev.sh <commande>

Commandes:
    ${GREEN}help${NC}              Affiche cette aide
    ${GREEN}status${NC}            Affiche l'état des conteneurs
    ${GREEN}logs${NC}              Affiche les logs live (app + SQL)
    ${GREEN}logs-app${NC}          Affiche les logs de l'app seulement
    ${GREEN}logs-sql${NC}          Affiche les logs de SQL Server seulement
    ${GREEN}shell-sql${NC}         Ouvre un shell sqlcmd dans SQL Server
    ${GREEN}shell-app${NC}         Ouvrir un shell bash dans le conteneur app
    ${GREEN}restart${NC}           Redémarre les conteneurs
    ${GREEN}stop${NC}              Arrête les conteneurs
    ${GREEN}clean${NC}             Arrête et supprime les volumes
    ${GREEN}reset-db${NC}          Réinitialise la base de données
    ${GREEN}test-deadlock${NC}     Reproduit manuellement un deadlock
    ${GREEN}port-check${NC}        Vérifie les ports actifs

Examples:
    ./scripts/dev.sh logs              # Suivi live des logs
    ./scripts/dev.sh shell-sql         # SQL Server shell
    ./scripts/dev.sh clean             # Clean slate

EOF
}

get_container_id() {
    local service=$1
    docker-compose ps -q "$service"
}

cmd_status() {
    echo -e "${BLUE}État des conteneurs:${NC}"
    docker-compose ps
}

cmd_logs() {
    echo -e "${BLUE}Logs live (Ctrl+C pour quitter)${NC}"
    docker-compose logs -f
}

cmd_logs_app() {
    echo -e "${BLUE}Logs de l'app (Ctrl+C pour quitter)${NC}"
    docker-compose logs -f deadlock-app
}

cmd_logs_sql() {
    echo -e "${BLUE}Logs de SQL Server (Ctrl+C pour quitter)${NC}"
    docker-compose logs -f sql-server
}

cmd_shell_sql() {
    local container=$(get_container_id sql-server)
    if [ -z "$container" ]; then
        echo -e "${RED}❌ Conteneur SQL Server non trouvé${NC}"
        echo "Lancer d'abord: docker-compose up -d"
        return 1
    fi
    
    echo -e "${BLUE}📝 Shell SQL Server (tapez 'quit' pour quitter)${NC}"
    docker exec -it "$container" /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa -P 'YourStrong!Pass2024'
}

cmd_shell_app() {
    local container=$(get_container_id deadlock-app)
    if [ -z "$container" ]; then
        echo -e "${RED}❌ Conteneur app non trouvé${NC}"
        return 1
    fi
    
    echo -e "${BLUE}📝 Shell app (tapez 'exit' pour quitter)${NC}"
    docker exec -it "$container" /bin/bash
}

cmd_restart() {
    echo -e "${BLUE}Redémarrage des conteneurs...${NC}"
    docker-compose restart
    echo -e "${GREEN}✓ Conteneurs redémarrés${NC}"
}

cmd_stop() {
    echo -e "${BLUE}Arrêt des conteneurs...${NC}"
    docker-compose stop
    echo -e "${GREEN}✓ Conteneurs arrêtés${NC}"
}

cmd_clean() {
    echo -e "${YELLOW}⚠️  Suppression des conteneurs ET des volumes${NC}"
    read -p "Confirmer? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        docker-compose down -v
        echo -e "${GREEN}✓ Nettoyage terminé${NC}"
    else
        echo "Annulé"
    fi
}

cmd_reset_db() {
    local container=$(get_container_id sql-server)
    if [ -z "$container" ]; then
        echo -e "${RED}❌ Conteneur SQL Server non trouvé${NC}"
        return 1
    fi
    
    echo -e "${BLUE}Réinitialisation de la base de données...${NC}"
    docker exec -i "$container" /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa -P 'YourStrong!Pass2024' << EOF
USE DeadlockTestDb;
GO
UPDATE dbo.DeadlockTest SET Value = 0;
GO
SELECT * FROM dbo.DeadlockTest;
GO
EOF
    echo -e "${GREEN}✓ Base réinitialisée${NC}"
}

cmd_test_deadlock() {
    local container=$(get_container_id sql-server)
    if [ -z "$container" ]; then
        echo -e "${RED}❌ Conteneur SQL Server non trouvé${NC}"
        return 1
    fi
    
    echo -e "${BLUE}Préparation du test deadlock...${NC}"
    echo -e "${YELLOW}⚠️  Ouvrez 2 terminaux supplémentaires et collez les commandes${NC}"
    echo ""
    
    echo -e "${BLUE}Session 1 (exécuter en premier):${NC}"
    cat << 'EOF'
docker exec -it manage_deadlock_polly-sql-server-1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong!Pass2024' << "SQL"
USE DeadlockTestDb;
BEGIN TRAN;
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 1;
WAITFOR DELAY '00:00:03';
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 2;
COMMIT TRAN;
GO
SQL
EOF
    
    echo ""
    echo -e "${BLUE}Session 2 (exécuter pendant que Session 1 attend):${NC}"
    cat << 'EOF'
docker exec -it manage_deadlock_polly-sql-server-1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong!Pass2024' << "SQL"
USE DeadlockTestDb;
WAITFOR DELAY '00:00:01';
BEGIN TRAN;
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 2;
WAITFOR DELAY '00:00:03';
UPDATE dbo.DeadlockTest SET Value = Value + 1 WHERE Id = 1;
COMMIT TRAN;
GO
SQL
EOF
    
    echo ""
    echo -e "${YELLOW}L'une des sessions doit voir: 'Msg 1205, Level 13 - Deadlock victim'${NC}"
}

cmd_port_check() {
    echo -e "${BLUE}Vérification des ports...${NC}"
    
    # Port SQL Server
    if lsof -i :1433 &>/dev/null; then
        echo -e "${GREEN}✓${NC} Port 1433 (SQL Server) - EN ÉCOUTE"
    else
        echo -e "${RED}✗${NC} Port 1433 (SQL Server) - LIBRE (conteneur arrêté?)"
    fi
    
    # Port optionnel app (si exposée)
    if lsof -i :8080 &>/dev/null; then
        echo -e "${GREEN}✓${NC} Port 8080 (App) - EN ÉCOUTE"
    else
        echo -e "${YELLOW}-${NC} Port 8080 (App) - Non en écoute (normal si app termine)"
    fi
    
    echo ""
    echo "Conteneurs actifs:"
    docker-compose ps
}

# Main
cd "$PROJECT_DIR"

case "${1:-help}" in
    help)
        print_help
        ;;
    status)
        cmd_status
        ;;
    logs)
        cmd_logs
        ;;
    logs-app)
        cmd_logs_app
        ;;
    logs-sql)
        cmd_logs_sql
        ;;
    shell-sql)
        cmd_shell_sql
        ;;
    shell-app)
        cmd_shell_app
        ;;
    restart)
        cmd_restart
        ;;
    stop)
        cmd_stop
        ;;
    clean)
        cmd_clean
        ;;
    reset-db)
        cmd_reset_db
        ;;
    test-deadlock)
        cmd_test_deadlock
        ;;
    port-check)
        cmd_port_check
        ;;
    *)
        echo -e "${RED}Commande inconnue: $1${NC}"
        print_help
        exit 1
        ;;
esac
