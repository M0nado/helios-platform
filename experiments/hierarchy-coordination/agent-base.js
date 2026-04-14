/**
 * Base Agent class for all hierarchies
 */

class Agent {
  constructor(id, role = 'worker') {
    this.id = id;
    this.role = role;
    this.active = true;
    this.messageQueue = [];
    this.handlers = {};
    this.metrics = null;
    this.processedMessages = 0;
  }

  setMetrics(metrics) {
    this.metrics = metrics;
  }

  // Message sending with metrics
  send(recipientId, messageType, payload = {}, isBroadcast = false) {
    const message = {
      from: this.id,
      to: recipientId,
      type: messageType,
      payload,
      timestamp: Date.now(),
      id: `${this.id}-${messageType}-${Date.now()}`
    };

    // Record message size in bytes (rough estimation)
    const messageSize = JSON.stringify(message).length;
    
    if (this.metrics) {
      this.metrics.recordMessage(this.id, recipientId, messageType, messageSize, isBroadcast);
    }

    return message;
  }

  // Register message handler
  on(messageType, handler) {
    if (!this.handlers[messageType]) {
      this.handlers[messageType] = [];
    }
    this.handlers[messageType].push(handler);
  }

  // Process incoming message
  async receiveMessage(message) {
    if (!this.active) return;

    this.messageQueue.push(message);
    this.processedMessages++;

    const handlers = this.handlers[message.type] || [];
    for (const handler of handlers) {
      await handler.call(this, message);
    }
  }

  // Broadcast to multiple recipients
  broadcast(recipientIds, messageType, payload = {}) {
    const messages = recipientIds.map(id => 
      this.send(id, messageType, payload, true)
    );
    return messages;
  }

  // Fail this agent
  fail() {
    this.active = false;
    if (this.metrics) {
      this.metrics.recordFailure(this.id, 'agent-failure');
    }
  }

  // Recover this agent
  recover() {
    this.active = true;
  }

  // Get agent status
  getStatus() {
    return {
      id: this.id,
      role: this.role,
      active: this.active,
      messagesProcessed: this.processedMessages,
      queueLength: this.messageQueue.length
    };
  }
}

module.exports = Agent;
