/**
 * HELIOS GUI Dashboard - 8-Tab Interface
 * v7.0 - Production Ready
 */

class GUIDashboard {
  constructor(config = {}) {
    this.version = '7.0';
    this.tabs = [
      { id: 1, name: 'Overview', data: {} },
      { id: 2, name: 'Performance', data: {} },
      { id: 3, name: 'Security', data: {} },
      { id: 4, name: 'Resources', data: {} },
      { id: 5, name: 'Alerts', data: {} },
      { id: 6, name: 'Analytics', data: {} },
      { id: 7, name: 'Logs', data: {} },
      { id: 8, name: 'Settings', data: {} },
    ];
    this.widgets = new Map();
  }

  getTab(tabId) {
    return this.tabs.find(t => t.id === tabId);
  }

  updateTabData(tabId, data) {
    const tab = this.getTab(tabId);
    if (tab) {
      tab.data = { ...tab.data, ...data, updated: Date.now() };
    }
    return tab;
  }

  addWidget(tabId, widget) {
    const key = `${tabId}_${Date.now()}`;
    this.widgets.set(key, { ...widget, tabId, id: key });
    return this.widgets.get(key);
  }

  getWidgets(tabId) {
    return Array.from(this.widgets.values()).filter(w => w.tabId === tabId);
  }

  renderDashboard() {
    return {
      version: this.version,
      tabs: this.tabs,
      widgets: Array.from(this.widgets.values()),
      rendered: Date.now(),
    };
  }

  getMetrics() {
    return {
      tabs: this.tabs.length,
      widgets: this.widgets.size,
      version: this.version,
    };
  }
}

module.exports = { GUIDashboard };
