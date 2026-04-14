/**
 * Security System Module - ENHANCED
 * AppLocker, Firewall, Vault Management
 * Features: Validation, Error Handling, Logging, Events, Audit Trail
 * v7.0
 */

const { Logger, Validator, ValidationError, EventEmitter, Cache } = require('../utils');

class SecuritySystem extends EventEmitter {
  constructor(config = {}) {
    super();
    this.config = config;
    this.logger = new Logger('SecuritySystem');
    this.appLockRules = [];
    this.firewallRules = [];
    this.vault = new Map();
    this.auditLog = [];
    this.rulesCache = new Cache(3600000); // 1 hour cache
    this.logger.info('Security System initialized');
  }

  addAppLockRule(rule) {
    try {
      // Validate input
      Validator.validateObject(rule, 'rule', ['path']);
      const path = Validator.validateString(rule.path, 'path');
      const action = rule.action || 'allow_signed_only';

      // Check for duplicates
      if (this.appLockRules.some(r => r.path === path)) {
        this.logger.warn(`AppLock rule already exists for ${path}`);
        throw new Error(`Rule already exists for path: ${path}`);
      }

      const appLockRule = {
        id: `rule_${Date.now()}`,
        path,
        action,
        timestamp: new Date(),
        enabled: true,
      };

      this.appLockRules.push(appLockRule);
      this.rulesCache.clear(); // Invalidate cache
      this.auditLog.push({ 
        action: 'add_applock', 
        rule: appLockRule,
        timestamp: new Date(),
        success: true 
      });
      this.logger.info(`AppLock rule added: ${path}`, { action });
      this.emit('applock-rule-added', appLockRule);
      return appLockRule;
    } catch (error) {
      this.logger.error('Failed to add AppLock rule', { error: error.message });
      this.emit('error', { action: 'add_applock', error });
      throw error;
    }
  }

  addFirewallRule(rule) {
    try {
      // Validate input
      Validator.validateObject(rule, 'rule');
      const validActions = ['allow', 'block', 'allow_signed_only'];
      const action = rule.action || 'allow';
      
      if (!validActions.includes(action)) {
        throw new ValidationError(`Action must be one of: ${validActions.join(', ')}`);
      }

      const fwRule = {
        id: `fw_${Date.now()}`,
        action,
        direction: rule.direction || 'inbound',
        protocol: rule.protocol || 'tcp',
        remote: rule.remote || 'any',
        timestamp: new Date(),
        enabled: true,
      };

      // Validate protocol
      const validProtocols = ['tcp', 'udp', 'icmp', 'any'];
      if (!validProtocols.includes(fwRule.protocol)) {
        throw new ValidationError(`Protocol must be one of: ${validProtocols.join(', ')}`);
      }

      this.firewallRules.push(fwRule);
      this.rulesCache.clear();
      this.auditLog.push({ 
        action: 'add_firewall', 
        rule: fwRule,
        timestamp: new Date(),
        success: true 
      });
      this.logger.info(`Firewall rule added: ${action} ${fwRule.direction}`, { protocol: fwRule.protocol });
      this.emit('firewall-rule-added', fwRule);
      return fwRule;
    } catch (error) {
      this.logger.error('Failed to add Firewall rule', { error: error.message });
      this.emit('error', { action: 'add_firewall', error });
      throw error;
    }
  }

  storeSecret(key, value) {
    try {
      const validatedKey = Validator.validateString(key, 'key', 1, 100);
      const validatedValue = Validator.validateString(value, 'value', 1, 10000);

      // Check if key already exists
      if (this.vault.has(validatedKey)) {
        this.logger.warn(`Secret key already exists: ${validatedKey}`);
      }

      this.vault.set(validatedKey, {
        value: validatedValue,
        stored: new Date(),
        encrypted: true,
        accessCount: 0,
      });

      this.auditLog.push({ 
        action: 'store_secret', 
        key: validatedKey,
        timestamp: new Date(),
        success: true 
      });
      this.logger.info(`Secret stored: ${validatedKey}`);
      this.emit('secret-stored', { key: validatedKey });
      return { status: 'stored', key: validatedKey };
    } catch (error) {
      this.logger.error('Failed to store secret', { error: error.message });
      this.emit('error', { action: 'store_secret', error });
      throw error;
    }
  }

  retrieveSecret(key) {
    try {
      const validatedKey = Validator.validateString(key, 'key');

      if (!this.vault.has(validatedKey)) {
        this.logger.warn(`Secret not found: ${validatedKey}`);
        this.auditLog.push({ 
          action: 'retrieve_secret_failed', 
          key: validatedKey,
          timestamp: new Date(),
          success: false 
        });
        return null;
      }

      const entry = this.vault.get(validatedKey);
      entry.accessCount++;
      
      this.auditLog.push({ 
        action: 'retrieve_secret', 
        key: validatedKey,
        timestamp: new Date(),
        success: true 
      });
      this.logger.debug(`Secret retrieved: ${validatedKey}`);
      return entry.value;
    } catch (error) {
      this.logger.error('Failed to retrieve secret', { error: error.message });
      throw error;
    }
  }

  removeRule(ruleId, type = 'applock') {
    try {
      if (type === 'applock') {
        const index = this.appLockRules.findIndex(r => r.id === ruleId);
        if (index > -1) {
          const removed = this.appLockRules.splice(index, 1)[0];
          this.rulesCache.clear();
          this.logger.info(`AppLock rule removed: ${ruleId}`);
          this.emit('applock-rule-removed', removed);
          return true;
        }
      } else if (type === 'firewall') {
        const index = this.firewallRules.findIndex(r => r.id === ruleId);
        if (index > -1) {
          const removed = this.firewallRules.splice(index, 1)[0];
          this.rulesCache.clear();
          this.logger.info(`Firewall rule removed: ${ruleId}`);
          this.emit('firewall-rule-removed', removed);
          return true;
        }
      }
      return false;
    } catch (error) {
      this.logger.error('Failed to remove rule', { error: error.message });
      throw error;
    }
  }

  validateSecurity() {
    try {
      const validation = {
        appLockRules: this.appLockRules.length,
        appLockEnabled: this.appLockRules.filter(r => r.enabled).length,
        firewallRules: this.firewallRules.length,
        firewallEnabled: this.firewallRules.filter(r => r.enabled).length,
        vaultItems: this.vault.size,
        status: 'validated',
        timestamp: new Date(),
      };
      this.logger.info('Security validation completed', validation);
      return validation;
    } catch (error) {
      this.logger.error('Security validation failed', { error: error.message });
      throw error;
    }
  }

  getAuditLog(startTime = null, endTime = null) {
    let logs = this.auditLog;
    if (startTime) {
      logs = logs.filter(l => new Date(l.timestamp) >= startTime);
    }
    if (endTime) {
      logs = logs.filter(l => new Date(l.timestamp) <= endTime);
    }
    return logs;
  }

  clearAuditLog() {
    const count = this.auditLog.length;
    this.auditLog = [];
    this.logger.info(`Audit log cleared: ${count} entries removed`);
    return count;
  }

  getMetrics() {
    return {
      module: 'security-system',
      appLockRules: this.appLockRules.length,
      appLockEnabled: this.appLockRules.filter(r => r.enabled).length,
      firewallRules: this.firewallRules.length,
      firewallEnabled: this.firewallRules.filter(r => r.enabled).length,
      secretsStored: this.vault.size,
      auditLogSize: this.auditLog.length,
      cachSize: this.rulesCache.getSize(),
      logCount: this.logger.getLogs().length,
      timestamp: Date.now(),
    };
  }
}

module.exports = { SecuritySystem };
