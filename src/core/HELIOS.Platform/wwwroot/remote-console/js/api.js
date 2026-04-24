// API Client for Remote Access
class RemoteAccessApiClient {
    constructor(baseUrl = 'https://localhost:5000/api/remote') {
        this.baseUrl = baseUrl;
        this.accessToken = localStorage.getItem('accessToken') || '';
    }

    async request(method, endpoint, data = null) {
        const url = `${this.baseUrl}${endpoint}`;
        const options = {
            method,
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.accessToken}`
            }
        };

        if (data) {
            options.body = JSON.stringify(data);
        }

        try {
            const response = await fetch(url, options);
            
            if (!response.ok) {
                if (response.status === 401) {
                    this.handleUnauthorized();
                }
                throw new Error(`API Error: ${response.statusText}`);
            }

            const responseData = await response.json();
            return responseData;
        } catch (error) {
            console.error(`API Request Failed: ${error.message}`);
            throw error;
        }
    }

    // Connection Management
    async createConnection(connectionInfo) {
        return this.request('POST', '/connections/create', connectionInfo);
    }

    async connectToRemote(connectionId) {
        return this.request('POST', `/connections/${connectionId}/connect`);
    }

    async disconnectRemote(connectionId) {
        return this.request('POST', `/connections/${connectionId}/disconnect`);
    }

    async getConnection(connectionId) {
        return this.request('GET', `/connections/${connectionId}`);
    }

    async listConnections() {
        return this.request('GET', '/connections');
    }

    async getConnectionStatus(connectionId) {
        return this.request('GET', `/connections/${connectionId}/status`);
    }

    async removeConnection(connectionId) {
        return this.request('DELETE', `/connections/${connectionId}`);
    }

    // Command Execution
    async executeCommand(request) {
        return this.request('POST', '/commands/execute', request);
    }

    async cancelCommand(requestId) {
        return this.request('POST', `/commands/${requestId}/cancel`);
    }

    async getCommandStatus(requestId) {
        return this.request('GET', `/commands/${requestId}/status`);
    }

    async getExecutionHistory(connectionId, maxResults = 100) {
        return this.request('GET', `/commands/${connectionId}/history?maxResults=${maxResults}`);
    }

    // Monitoring & Diagnostics
    async collectDiagnostics(connectionId) {
        return this.request('GET', `/diagnostics/${connectionId}`);
    }

    async startMonitoring(connectionId, intervalSeconds = 60) {
        return this.request('POST', `/monitoring/${connectionId}/start?intervalSeconds=${intervalSeconds}`);
    }

    async stopMonitoring(monitoringSessionId) {
        return this.request('POST', `/monitoring/${monitoringSessionId}/stop`);
    }

    async getHealthStatus(connectionId) {
        return this.request('GET', `/health/${connectionId}`);
    }

    async generateDiagnosticReport(connectionId) {
        return this.request('GET', `/report/${connectionId}`);
    }

    // File Transfer
    async uploadFile(connectionId, localPath, remotePath) {
        return this.request('POST', `/files/${connectionId}/upload?localPath=${encodeURIComponent(localPath)}&remotePath=${encodeURIComponent(remotePath)}`);
    }

    async downloadFile(connectionId, remotePath, localPath) {
        return this.request('POST', `/files/${connectionId}/download?remotePath=${encodeURIComponent(remotePath)}&localPath=${encodeURIComponent(localPath)}`);
    }

    async listFiles(connectionId, remotePath) {
        return this.request('GET', `/files/${connectionId}/list?remotePath=${encodeURIComponent(remotePath)}`);
    }

    async deleteFile(connectionId, remotePath) {
        return this.request('DELETE', `/files/${connectionId}?remotePath=${encodeURIComponent(remotePath)}`);
    }

    // Session Management
    async createSession(userId, username, authMethod) {
        return this.request('POST', `/sessions/create?userId=${encodeURIComponent(userId)}&username=${encodeURIComponent(username)}&authMethod=${authMethod}`);
    }

    async getSession(sessionId) {
        return this.request('GET', `/sessions/${sessionId}`);
    }

    async listActiveSessions() {
        return this.request('GET', '/sessions');
    }

    async terminateSession(sessionId) {
        return this.request('POST', `/sessions/${sessionId}/terminate`);
    }

    async getActivityHistory(sessionId) {
        return this.request('GET', `/sessions/${sessionId}/activity`);
    }

    handleUnauthorized() {
        localStorage.removeItem('accessToken');
        window.location.href = '/login';
    }
}

// Create global API client instance
window.apiClient = new RemoteAccessApiClient();
