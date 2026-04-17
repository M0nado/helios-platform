#!/bin/bash
# Example: Continuous Deployment Script
# This script demonstrates automated deployment workflow

set -e

echo "=== HELIOS Continuous Deployment Example ==="
echo

# Configuration
CONFIG_FILE="${1:-deployment.json}"
BACKUP_PATH="/backups/$(date +%Y%m%d_%H%M%S)"
TIMEOUT=120

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}[1/7] Running pre-deployment health checks...${NC}"
helios-cli health --timeout $TIMEOUT || {
    echo -e "${RED}Health check failed. Aborting deployment.${NC}"
    exit 1
}

echo -e "${GREEN}✓ Health check passed${NC}"
echo

echo -e "${YELLOW}[2/7] Creating backup...${NC}"
mkdir -p "$BACKUP_PATH"
helios-cli backup --path "$BACKUP_PATH" --compress || {
    echo -e "${RED}Backup failed. Aborting deployment.${NC}"
    exit 1
}
echo -e "${GREEN}✓ Backup created at $BACKUP_PATH${NC}"
echo

echo -e "${YELLOW}[3/7] Getting current status...${NC}"
helios-cli status --json > /tmp/pre-deploy-status.json
echo -e "${GREEN}✓ Status captured${NC}"
echo

echo -e "${YELLOW}[4/7] Deploying application...${NC}"
helios-cli deploy --config "$CONFIG_FILE" --timeout $TIMEOUT || {
    echo -e "${RED}Deployment failed. Consider restoring from backup.${NC}"
    echo "Backup location: $BACKUP_PATH"
    exit 1
}
echo -e "${GREEN}✓ Deployment completed${NC}"
echo

echo -e "${YELLOW}[5/7] Running post-deployment health checks...${NC}"
helios-cli health --timeout $TIMEOUT || {
    echo -e "${RED}Post-deployment health check failed.${NC}"
    exit 1
}
echo -e "${GREEN}✓ Post-deployment health check passed${NC}"
echo

echo -e "${YELLOW}[6/7] Verifying deployment status...${NC}"
helios-cli status --json > /tmp/post-deploy-status.json
echo -e "${GREEN}✓ Status verified${NC}"
echo

echo -e "${YELLOW}[7/7] Generating deployment report...${NC}"
cat << EOF > /tmp/deployment-report.txt
Deployment Report
=================
Timestamp: $(date)
Config File: $CONFIG_FILE
Backup Path: $BACKUP_PATH

Pre-Deployment Status:
$(cat /tmp/pre-deploy-status.json | jq '.' 2>/dev/null || echo "Could not parse status")

Post-Deployment Status:
$(cat /tmp/post-deploy-status.json | jq '.' 2>/dev/null || echo "Could not parse status")

Status: SUCCESSFUL
EOF

echo -e "${GREEN}✓ Report generated at /tmp/deployment-report.txt${NC}"
echo

echo -e "${GREEN}=== DEPLOYMENT COMPLETED SUCCESSFULLY ===${NC}"
