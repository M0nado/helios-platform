// Main Application Controller
class RemoteAccessApp {
    constructor() {
        this.config = this.loadConfiguration();
        this.apiClient = window.apiClient;
        this.wsClient = null;
        this.monitoringActive = false;
        this.initializeApp();
    }

    loadConfiguration() {
        const savedSettings = localStorage.getItem('appSettings');
        return {
            apiEndpoint: savedSettings?.apiEndpoint || 'https://localhost:5000/api/remote',
            refreshInterval: savedSettings?.refreshInterval || 60000,
            commandTimeout: savedSettings?.commandTimeout || 300,
            autoRefresh: savedSettings?.autoRefresh !== false,
            requireMFA: savedSettings?.requireMFA !== false,
            enableAuditLog: savedSettings?.enableAuditLog !== false,
            enableEncryption: savedSettings?.enableEncryption !== false
        };
    }

    async initializeApp() {
        console.log('Initializing Remote Access Application...');
        
        // Check authentication
        if (!this.isAuthenticated()) {
            window.location.href = '/login';
            return;
        }

        // Initialize WebSocket connection for real-time updates
        this.initializeWebSocket();

        // Setup auto-refresh if enabled
        if (this.config.autoRefresh) {
            setInterval(() => this.refreshDashboard(), this.config.refreshInterval);
        }

        console.log('Remote Access Application initialized successfully');
    }

    isAuthenticated() {
        const token = localStorage.getItem('accessToken');
        return token && token.length > 0;
    }

    initializeWebSocket() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        const wsUrl = `${protocol}//${window.location.host}/api/remote/ws`;

        try {
            this.wsClient = new WebSocket(wsUrl);

            this.wsClient.onopen = () => {
                console.log('WebSocket connected');
                this.updateConnectionStatus('connected');
            };

            this.wsClient.onmessage = (event) => {
                const data = JSON.parse(event.data);
                this.handleWebSocketMessage(data);
            };

            this.wsClient.onerror = (error) => {
                console.error('WebSocket error:', error);
                this.updateConnectionStatus('error');
            };

            this.wsClient.onclose = () => {
                console.log('WebSocket closed');
                this.updateConnectionStatus('disconnected');
                // Attempt to reconnect after 5 seconds
                setTimeout(() => this.initializeWebSocket(), 5000);
            };
        } catch (error) {
            console.error('Failed to initialize WebSocket:', error);
        }
    }

    handleWebSocketMessage(data) {
        const { type, payload } = data;

        switch (type) {
            case 'diagnostics':
                this.updateDiagnostics(payload);
                break;
            case 'command_output':
                this.updateCommandOutput(payload);
                break;
            case 'file_transfer':
                this.updateFileTransfer(payload);
                break;
            case 'session_event':
                this.handleSessionEvent(payload);
                break;
            case 'alert':
                this.handleAlert(payload);
                break;
            default:
                console.log('Unknown message type:', type);
        }
    }

    updateDiagnostics(diagnostics) {
        // Update dashboard metrics
        if (diagnostics.cpuUsagePercent !== undefined) {
            const cpuFill = document.getElementById('cpuUsage');
            const cpuPercent = document.getElementById('cpuPercent');
            if (cpuFill && cpuPercent) {
                cpuFill.style.width = `${diagnostics.cpuUsagePercent}%`;
                cpuPercent.textContent = `${diagnostics.cpuUsagePercent.toFixed(1)}%`;
            }
        }

        if (diagnostics.memoryUsagePercent !== undefined) {
            const memFill = document.getElementById('memoryUsage');
            const memPercent = document.getElementById('memoryPercent');
            if (memFill && memPercent) {
                memFill.style.width = `${diagnostics.memoryUsagePercent}%`;
                memPercent.textContent = `${diagnostics.memoryUsagePercent.toFixed(1)}%`;
            }
        }

        if (diagnostics.diskUsagePercent !== undefined) {
            const diskFill = document.getElementById('diskUsage');
            const diskPercent = document.getElementById('diskPercent');
            if (diskFill && diskPercent) {
                diskFill.style.width = `${diagnostics.diskUsagePercent}%`;
                diskPercent.textContent = `${diagnostics.diskUsagePercent.toFixed(1)}%`;
            }
        }
    }

    updateCommandOutput(output) {
        const outputPanel = document.getElementById('commandOutput');
        if (outputPanel) {
            outputPanel.textContent += output + '\n';
            outputPanel.scrollTop = outputPanel.scrollHeight;
        }
    }

    updateFileTransfer(transfer) {
        const transfersList = document.getElementById('transfersList');
        if (transfersList) {
            const progress = (transfer.bytesTransferred / transfer.fileSizeBytes) * 100;
            transfersList.innerHTML += `
                <div class="transfer-item">
                    <p>${transfer.fileName} - ${progress.toFixed(1)}%</p>
                    <div class="progress-bar">
                        <div class="progress-fill" style="width: ${progress}%"></div>
                    </div>
                </div>
            `;
        }
    }

    handleSessionEvent(event) {
        console.log('Session event:', event);
        if (uiManager) {
            uiManager.refreshSessions();
        }
    }

    handleAlert(alert) {
        if (uiManager) {
            uiManager.showNotification(alert.message, alert.type || 'info');
        }
    }

    updateConnectionStatus(status) {
        const statusBadge = document.getElementById('connectionStatus');
        if (statusBadge) {
            statusBadge.textContent = status.charAt(0).toUpperCase() + status.slice(1);
            statusBadge.className = `status-badge ${status}`;
        }
    }

    async refreshDashboard() {
        try {
            // Fetch and update dashboard statistics
            const connections = await this.apiClient.listConnections();
            const sessions = await this.apiClient.listActiveSessions();

            if (connections.success && connections.data) {
                document.getElementById('activeConnections').textContent = connections.data.length;
            }

            if (sessions.success && sessions.data) {
                document.getElementById('activeSessions').textContent = sessions.data.length;
            }
        } catch (error) {
            console.error('Error refreshing dashboard:', error);
        }
    }

    // Export functions to global scope for use in HTML
    static getInstance() {
        if (!window.remoteAccessApp) {
            window.remoteAccessApp = new RemoteAccessApp();
        }
        return window.remoteAccessApp;
    }
}

// Keyboard shortcuts
document.addEventListener('keydown', (e) => {
    if (e.ctrlKey || e.metaKey) {
        switch (e.key) {
            case 'k':
                e.preventDefault();
                document.getElementById('commandInput').focus();
                break;
            case 's':
                e.preventDefault();
                document.getElementById('saveSettingsBtn').click();
                break;
        }
    }
});

// Initialize app when page loads
window.addEventListener('load', () => {
    RemoteAccessApp.getInstance();
});

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (window.remoteAccessApp && window.remoteAccessApp.wsClient) {
        window.remoteAccessApp.wsClient.close();
    }
});


// Static AIHub preview for the built-in server GUI. Live data comes from generated reports when served by HELIOS.
function renderAIHubControlPlane() {
    const party = [
        { id: 'hermes-branch-sentinel', icon: '🛡️', family: 'Hermes', type: 'Branch/autofix', xp: 74, specialization: 'GitHub branches, commits, PR safety, merge/prune packets' },
        { id: 'xcore-native-sprinter', icon: '⚡', family: 'XCore', type: 'C++ performance', xp: 81, specialization: 'Hot paths, memory, graph comparisons, native scoring clusters' },
        { id: 'xcore-fsharp-oracle', icon: '🔮', family: 'XCore', type: 'F# learning', xp: 79, specialization: 'Prediction, async queries, scoring, ranking, branch priority' },
        { id: 'hermes-cloud-keeper', icon: '☁️', family: 'Hermes', type: 'Azure/Bicep', xp: 68, specialization: 'Key Vault, Bicep what-if, SQL/Cosmos/vector, hybrid policy' }
    ];
    const commands = [
        'scripts/setup/bootstrap-local-tools.sh',
        'python3 scripts/integrations/aihub_unified_control_plane.py',
        'python3 scripts/integrations/aihub_supershell_vault_wizard.py',
        'python3 scripts/analysis/aihub_language_skill_profiles.py',
        'scripts/setup/simple-build.sh module',
        'scripts/setup/simple-build.sh quick',
        'scripts/setup/simple-build.sh absorb',
        'scripts/setup/simple-build.sh finish',
        'python3 scripts/agents/branch_test_autofix_plan.py',
        'python3 scripts/build_graph/build_graph.py run --profile full --max-workers 2',
        'python3 scripts/dashboard/generate-gui.py'
    ];
    const modules = [
        'C# orchestration foundation: secure API, vault, GUI state, policy flags, engine routing.',
        'F# learning engine: prediction, branch issue scoring, agent XP ranking, merge selection.',
        'C++ native performance engine: weighted similarity, memory pressure, fast graph comparison.',
        'Python AIHub glue: provider setup, Linux libraries, API bridges, report automation.',
        'YAML/Bicep/JSON/MD fabric: workflows, cloud IaC, live flags, knowledge ledgers.'
    ];
    const grid = document.getElementById('aihubPartyGrid');
    if (grid) {
        const search = document.getElementById('aihubSearch');
        const query = search ? search.value.toLowerCase() : '';
        const visibleParty = party.filter(p => !query || `${p.id} ${p.family} ${p.type} ${p.specialization}`.toLowerCase().includes(query));
        grid.innerHTML = visibleParty.map(p => `
            <div class="card aihub-agent-card">
                <span class="aihub-agent-icon">${p.icon}</span>
                <h3>${p.id}</h3>
                <p>${p.family} · ${p.type}</p>
                <div class="xp-bar"><div class="xp-fill" style="width:${p.xp}%"></div></div>
                <p>XP ${p.xp} · ${p.specialization}</p>
            </div>`).join('');
    }
    const commandList = document.getElementById('aihubCommandList');
    if (commandList) {
        commandList.innerHTML = commands.map(c => `<code>${c}</code>`).join('');
    }
    const modulePlan = document.getElementById('aihubModulePlan');
    if (modulePlan) {
        modulePlan.innerHTML = modules.map(m => `<div class="aihub-module-item">${m}</div>`).join('');
    }
}

document.addEventListener('DOMContentLoaded', () => {
    renderAIHubControlPlane();
    const refresh = document.getElementById('refreshAIHubBtn');
    if (refresh) {
        refresh.addEventListener('click', renderAIHubControlPlane);
    }
});
