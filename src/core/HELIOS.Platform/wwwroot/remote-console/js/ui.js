// UI Manager for Remote Access Console
class UIManager {
    constructor() {
        this.currentTab = 'dashboard';
        this.connections = [];
        this.sessions = [];
        this.initializeEventListeners();
        this.loadInitialData();
    }

    initializeEventListeners() {
        // Navigation
        document.querySelectorAll('[data-tab]').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const tabName = link.dataset.tab;
                this.switchTab(tabName);
            });
        });

        // Connection Modal
        document.getElementById('createConnBtn').addEventListener('click', () => this.openConnectionModal());
        document.getElementById('cancelConnBtn').addEventListener('click', () => this.closeConnectionModal());
        document.querySelector('.close').addEventListener('click', () => this.closeConnectionModal());

        // Connection Form
        document.getElementById('connectionForm').addEventListener('submit', (e) => this.handleCreateConnection(e));

        // Command Execution
        document.getElementById('executeBtn').addEventListener('click', () => this.executeCommand());

        // File Manager
        document.getElementById('browseBtn').addEventListener('click', () => this.listRemoteFiles());
        document.getElementById('uploadBtn').addEventListener('click', () => document.getElementById('fileInput').click());
        document.getElementById('fileInput').addEventListener('change', (e) => this.handleFileUpload(e));

        // Session Management
        document.getElementById('createSessionBtn').addEventListener('click', () => this.showCreateSessionModal());

        // Settings
        document.getElementById('saveSettingsBtn').addEventListener('click', () => this.saveSettings());

        // Monitoring
        document.getElementById('startMonitoringBtn').addEventListener('click', () => this.startMonitoring());
        document.getElementById('stopMonitoringBtn').addEventListener('click', () => this.stopMonitoring());
        document.getElementById('generateReportBtn').addEventListener('click', () => this.generateReport());

        // Modal backdrop close
        window.addEventListener('click', (e) => {
            const modal = document.getElementById('connectionModal');
            if (e.target === modal) {
                this.closeConnectionModal();
            }
        });
    }

    switchTab(tabName) {
        // Hide all tabs
        document.querySelectorAll('.tab-content').forEach(tab => {
            tab.classList.remove('active');
        });

        // Remove active class from nav links
        document.querySelectorAll('.nav-link').forEach(link => {
            link.classList.remove('active');
        });

        // Show selected tab
        const tabElement = document.getElementById(tabName);
        if (tabElement) {
            tabElement.classList.add('active');
            document.querySelector(`[data-tab="${tabName}"]`).classList.add('active');
            this.currentTab = tabName;

            // Refresh data for tab if needed
            if (tabName === 'connections') {
                this.refreshConnections();
            } else if (tabName === 'sessions') {
                this.refreshSessions();
            }
        }
    }

    openConnectionModal() {
        document.getElementById('connectionModal').classList.add('active');
    }

    closeConnectionModal() {
        document.getElementById('connectionModal').classList.remove('active');
        document.getElementById('connectionForm').reset();
    }

    async handleCreateConnection(event) {
        event.preventDefault();

        const connectionInfo = {
            remoteHost: document.getElementById('remoteHost').value,
            remotePort: parseInt(document.getElementById('remotePort').value),
            protocol: document.getElementById('protocol').value,
            credentials: {
                encryptedUsername: document.getElementById('username').value,
                encryptedPassword: document.getElementById('password').value,
                encryptionAlgorithm: 'AES-256-GCM'
            },
            useSecureTunneling: document.getElementById('useSecureTunneling').checked
        };

        try {
            const response = await apiClient.createConnection(connectionInfo);
            if (response.success) {
                this.showNotification('Connection created successfully', 'success');
                this.closeConnectionModal();
                this.refreshConnections();
            } else {
                this.showNotification(`Error: ${response.message}`, 'error');
            }
        } catch (error) {
            this.showNotification(`Failed to create connection: ${error.message}`, 'error');
        }
    }

    async refreshConnections() {
        try {
            const response = await apiClient.listConnections();
            if (response.success) {
                this.connections = response.data || [];
                this.renderConnections();
                this.updateConnectionSelects();
            }
        } catch (error) {
            console.error('Failed to refresh connections:', error);
        }
    }

    renderConnections() {
        const connectionsList = document.getElementById('connectionsList');
        
        if (!this.connections || this.connections.length === 0) {
            connectionsList.innerHTML = '<p>No connections. Create a new connection to get started.</p>';
            return;
        }

        connectionsList.innerHTML = this.connections.map(conn => `
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center;">
                    <div>
                        <h4>${conn.remoteHost}:${conn.remotePort}</h4>
                        <p style="color: #95a5a6; font-size: 14px;">Protocol: ${conn.protocol}</p>
                        <p style="color: #95a5a6; font-size: 14px;">Status: <span class="status-badge ${conn.state.toLowerCase()}">${conn.state}</span></p>
                    </div>
                    <div style="display: flex; gap: 10px;">
                        <button class="btn btn-primary" onclick="uiManager.connectToHost('${conn.connectionId}')">Connect</button>
                        <button class="btn btn-secondary" onclick="uiManager.disconnectFromHost('${conn.connectionId}')">Disconnect</button>
                        <button class="btn btn-danger" onclick="uiManager.removeConnection('${conn.connectionId}')">Remove</button>
                    </div>
                </div>
            </div>
        `).join('');
    }

    updateConnectionSelects() {
        const selects = [
            'commandConnectionSelect',
            'monitoringConnectionSelect',
            'fileConnectionSelect'
        ];

        selects.forEach(selectId => {
            const select = document.getElementById(selectId);
            const currentValue = select.value;
            select.innerHTML = '<option value="">-- Select a connection --</option>';
            this.connections.forEach(conn => {
                const option = document.createElement('option');
                option.value = conn.connectionId;
                option.textContent = `${conn.remoteHost}:${conn.remotePort}`;
                select.appendChild(option);
            });
            select.value = currentValue;
        });
    }

    async connectToHost(connectionId) {
        try {
            const response = await apiClient.connectToRemote(connectionId);
            if (response.success) {
                this.showNotification('Connected successfully', 'success');
                this.refreshConnections();
            } else {
                this.showNotification(`Connection failed: ${response.message}`, 'error');
            }
        } catch (error) {
            this.showNotification(`Failed to connect: ${error.message}`, 'error');
        }
    }

    async disconnectFromHost(connectionId) {
        try {
            const response = await apiClient.disconnectRemote(connectionId);
            if (response.success) {
                this.showNotification('Disconnected successfully', 'success');
                this.refreshConnections();
            } else {
                this.showNotification(`Disconnection failed: ${response.message}`, 'error');
            }
        } catch (error) {
            this.showNotification(`Failed to disconnect: ${error.message}`, 'error');
        }
    }

    async removeConnection(connectionId) {
        if (confirm('Are you sure you want to remove this connection?')) {
            try {
                const response = await apiClient.removeConnection(connectionId);
                if (response.success) {
                    this.showNotification('Connection removed', 'success');
                    this.refreshConnections();
                }
            } catch (error) {
                this.showNotification(`Failed to remove connection: ${error.message}`, 'error');
            }
        }
    }

    async executeCommand() {
        const connectionId = document.getElementById('commandConnectionSelect').value;
        const command = document.getElementById('commandInput').value;
        const timeout = parseInt(document.getElementById('commandTimeout').value);

        if (!connectionId || !command) {
            this.showNotification('Please select a connection and enter a command', 'error');
            return;
        }

        const request = {
            connectionId,
            command,
            timeoutSeconds: timeout,
            captureOutput: true,
            streamOutput: false
        };

        try {
            document.getElementById('executionStatus').textContent = 'Running...';
            document.getElementById('executionStatus').className = 'status-badge pending';
            
            const response = await apiClient.executeCommand(request);
            
            if (response.success) {
                const result = response.data;
                document.getElementById('commandOutput').textContent = 
                    `Exit Code: ${result.exitCode}\n\n${result.standardOutput}\n\n${result.standardError}`;
                
                document.getElementById('executionStatus').textContent = result.status;
                document.getElementById('executionStatus').className = `status-badge ${result.status === 'Completed' ? 'success' : 'error'}`;
                
                this.showNotification('Command executed successfully', 'success');
            } else {
                this.showNotification(`Command failed: ${response.message}`, 'error');
            }
        } catch (error) {
            this.showNotification(`Failed to execute command: ${error.message}`, 'error');
            document.getElementById('executionStatus').textContent = 'Error';
            document.getElementById('executionStatus').className = 'status-badge error';
        }
    }

    async listRemoteFiles() {
        const connectionId = document.getElementById('fileConnectionSelect').value;
        const remotePath = document.getElementById('remotePath').value;

        if (!connectionId) {
            this.showNotification('Please select a connection', 'error');
            return;
        }

        try {
            const response = await apiClient.listFiles(connectionId, remotePath);
            if (response.success) {
                const files = response.data || [];
                this.renderFileList(files);
            }
        } catch (error) {
            this.showNotification(`Failed to list files: ${error.message}`, 'error');
        }
    }

    renderFileList(files) {
        const table = document.getElementById('filesTable');
        if (!files || files.length === 0) {
            table.innerHTML = '<tr><td colspan="5">No files found</td></tr>';
            return;
        }

        table.innerHTML = files.map(file => `
            <tr>
                <td>${file.name}</td>
                <td>${file.isDirectory ? 'Directory' : 'File'}</td>
                <td>${this.formatBytes(file.sizeBytes)}</td>
                <td>${new Date(file.modifiedAt).toLocaleDateString()}</td>
                <td>
                    <button class="btn btn-primary" onclick="uiManager.downloadRemoteFile('${file.path}')" style="padding: 5px 10px; font-size: 12px;">Download</button>
                    <button class="btn btn-danger" onclick="uiManager.deleteRemoteFile('${file.path}')" style="padding: 5px 10px; font-size: 12px;">Delete</button>
                </td>
            </tr>
        `).join('');
    }

    async handleFileUpload(event) {
        const file = event.target.files[0];
        if (!file) return;

        const connectionId = document.getElementById('fileConnectionSelect').value;
        const remotePath = document.getElementById('remotePath').value;

        if (!connectionId) {
            this.showNotification('Please select a connection', 'error');
            return;
        }

        try {
            const response = await apiClient.uploadFile(
                connectionId,
                file.name,
                `${remotePath}${file.name}`
            );

            if (response.success) {
                this.showNotification('File uploaded successfully', 'success');
                this.listRemoteFiles();
            }
        } catch (error) {
            this.showNotification(`Failed to upload file: ${error.message}`, 'error');
        }
    }

    async downloadRemoteFile(remotePath) {
        const connectionId = document.getElementById('fileConnectionSelect').value;
        try {
            const response = await apiClient.downloadFile(connectionId, remotePath, remotePath);
            if (response.success) {
                this.showNotification('File downloaded successfully', 'success');
            }
        } catch (error) {
            this.showNotification(`Failed to download file: ${error.message}`, 'error');
        }
    }

    async deleteRemoteFile(remotePath) {
        if (!confirm('Are you sure you want to delete this file?')) return;

        const connectionId = document.getElementById('fileConnectionSelect').value;
        try {
            const response = await apiClient.deleteFile(connectionId, remotePath);
            if (response.success) {
                this.showNotification('File deleted successfully', 'success');
                this.listRemoteFiles();
            }
        } catch (error) {
            this.showNotification(`Failed to delete file: ${error.message}`, 'error');
        }
    }

    async refreshSessions() {
        try {
            const response = await apiClient.listActiveSessions();
            if (response.success) {
                this.sessions = response.data || [];
                this.renderSessions();
            }
        } catch (error) {
            console.error('Failed to refresh sessions:', error);
        }
    }

    renderSessions() {
        const table = document.getElementById('sessionsTable');
        if (!this.sessions || this.sessions.length === 0) {
            table.innerHTML = '<tr><td colspan="6">No active sessions</td></tr>';
            return;
        }

        table.innerHTML = this.sessions.map(session => `
            <tr>
                <td>${session.sessionId}</td>
                <td>${session.username}</td>
                <td><span class="status-badge ${session.status.toLowerCase()}">${session.status}</span></td>
                <td>${new Date(session.createdAt).toLocaleString()}</td>
                <td>${new Date(session.lastActivityAt).toLocaleString()}</td>
                <td>
                    <button class="btn btn-danger" onclick="uiManager.terminateSession('${session.sessionId}')" style="padding: 5px 10px; font-size: 12px;">Terminate</button>
                </td>
            </tr>
        `).join('');
    }

    async terminateSession(sessionId) {
        if (!confirm('Are you sure you want to terminate this session?')) return;

        try {
            const response = await apiClient.terminateSession(sessionId);
            if (response.success) {
                this.showNotification('Session terminated', 'success');
                this.refreshSessions();
            }
        } catch (error) {
            this.showNotification(`Failed to terminate session: ${error.message}`, 'error');
        }
    }

    async startMonitoring() {
        const connectionId = document.getElementById('monitoringConnectionSelect').value;
        if (!connectionId) {
            this.showNotification('Please select a connection', 'error');
            return;
        }

        try {
            const response = await apiClient.startMonitoring(connectionId, 60);
            if (response.success) {
                this.showNotification('Monitoring started', 'success');
                document.getElementById('startMonitoringBtn').disabled = true;
                document.getElementById('stopMonitoringBtn').disabled = false;
            }
        } catch (error) {
            this.showNotification(`Failed to start monitoring: ${error.message}`, 'error');
        }
    }

    async stopMonitoring() {
        try {
            this.showNotification('Monitoring stopped', 'success');
            document.getElementById('startMonitoringBtn').disabled = false;
            document.getElementById('stopMonitoringBtn').disabled = true;
        } catch (error) {
            this.showNotification(`Failed to stop monitoring: ${error.message}`, 'error');
        }
    }

    async generateReport() {
        const connectionId = document.getElementById('monitoringConnectionSelect').value;
        if (!connectionId) {
            this.showNotification('Please select a connection', 'error');
            return;
        }

        try {
            const response = await apiClient.generateDiagnosticReport(connectionId);
            if (response.success) {
                const report = response.data;
                const reportHtml = `
                    <h4>Diagnostic Report</h4>
                    <p><strong>Generated:</strong> ${new Date(report.generatedAt).toLocaleString()}</p>
                    <p><strong>Average CPU:</strong> ${report.averageCpuUsage.toFixed(2)}%</p>
                    <p><strong>Average Memory:</strong> ${report.averageMemoryUsage.toFixed(2)}%</p>
                    <p><strong>Peak CPU:</strong> ${report.peakCpuUsage.toFixed(2)}%</p>
                    <p><strong>Peak Memory:</strong> ${report.peakMemoryUsage.toFixed(2)}%</p>
                `;
                document.getElementById('healthStatus').innerHTML = reportHtml;
                this.showNotification('Report generated successfully', 'success');
            }
        } catch (error) {
            this.showNotification(`Failed to generate report: ${error.message}`, 'error');
        }
    }

    saveSettings() {
        const settings = {
            apiEndpoint: document.getElementById('apiEndpoint').value,
            refreshInterval: parseInt(document.getElementById('refreshInterval').value),
            commandTimeout: parseInt(document.getElementById('globalTimeout').value),
            autoRefresh: document.getElementById('autoRefresh').checked,
            requireMFA: document.getElementById('requireMFA').checked,
            enableAuditLog: document.getElementById('enableAuditLog').checked,
            enableEncryption: document.getElementById('enableEncryption').checked
        };

        localStorage.setItem('appSettings', JSON.stringify(settings));
        this.showNotification('Settings saved successfully', 'success');
    }

    showNotification(message, type = 'info') {
        const container = document.getElementById('notificationContainer');
        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        notification.textContent = message;

        container.appendChild(notification);

        setTimeout(() => {
            notification.remove();
        }, 5000);
    }

    formatBytes(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
    }

    async loadInitialData() {
        await this.refreshConnections();
        await this.refreshSessions();
    }

    showCreateSessionModal() {
        // Implementation for creating sessions
        this.showNotification('Create session feature coming soon', 'info');
    }
}

// Initialize UI when DOM is ready
let uiManager;
document.addEventListener('DOMContentLoaded', () => {
    uiManager = new UIManager();
});
