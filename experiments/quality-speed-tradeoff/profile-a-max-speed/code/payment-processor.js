/**
 * Payment Processor Module
 * Handles payment processing, validation, and transaction management
 * ~250 LOC per module
 */

class PaymentProcessor {
  constructor(config = {}) {
    this.config = {
      maxRetries: config.maxRetries || 3,
      timeout: config.timeout || 5000,
      minAmount: config.minAmount || 0.01,
      maxAmount: config.maxAmount || 1000000,
      currency: config.currency || 'USD',
      ...config
    };
    this.transactions = [];
    this.failedTransactions = [];
  }

  // Validate payment amount
  validateAmount(amount) {
    if (typeof amount !== 'number') {
      return { valid: false, error: 'Amount must be a number' };
    }
    if (amount < this.config.minAmount) {
      return { valid: false, error: `Amount below minimum of ${this.config.minAmount}` };
    }
    if (amount > this.config.maxAmount) {
      return { valid: false, error: `Amount exceeds maximum of ${this.config.maxAmount}` };
    }
    if (amount <= 0) {
      return { valid: false, error: 'Amount must be positive' };
    }
    return { valid: true };
  }

  // Validate card details
  validateCard(cardData) {
    if (!cardData || typeof cardData !== 'object') {
      return { valid: false, error: 'Invalid card data' };
    }

    const { number, expiry, cvc } = cardData;

    if (!number || typeof number !== 'string') {
      return { valid: false, error: 'Card number required' };
    }
    if (!/^\d{13,19}$/.test(number.replace(/\s/g, ''))) {
      return { valid: false, error: 'Invalid card number format' };
    }

    if (!expiry || typeof expiry !== 'string') {
      return { valid: false, error: 'Expiry date required' };
    }
    if (!/^\d{2}\/\d{2}$/.test(expiry)) {
      return { valid: false, error: 'Invalid expiry format (MM/YY)' };
    }

    const [month, year] = expiry.split('/');
    const currentDate = new Date();
    const currentYear = currentDate.getFullYear() % 100;
    const currentMonth = currentDate.getMonth() + 1;

    const expiryYear = parseInt(year);
    const expiryMonth = parseInt(month);

    if (expiryYear < currentYear || (expiryYear === currentYear && expiryMonth < currentMonth)) {
      return { valid: false, error: 'Card expired' };
    }

    if (!cvc || typeof cvc !== 'string') {
      return { valid: false, error: 'CVC required' };
    }
    if (!/^\d{3,4}$/.test(cvc)) {
      return { valid: false, error: 'Invalid CVC format' };
    }

    return { valid: true };
  }

  // Process payment transaction
  processPayment(paymentData) {
    const { amount, card, userId, description } = paymentData;

    // Validate amount
    const amountValidation = this.validateAmount(amount);
    if (!amountValidation.valid) {
      return { success: false, error: amountValidation.error };
    }

    // Validate card
    const cardValidation = this.validateCard(card);
    if (!cardValidation.valid) {
      return { success: false, error: cardValidation.error };
    }

    // Validate user ID
    if (!userId || typeof userId !== 'string') {
      return { success: false, error: 'User ID required' };
    }

    // Create transaction record
    const transaction = {
      id: this.generateTransactionId(),
      userId,
      amount,
      currency: this.config.currency,
      description: description || '',
      cardLast4: card.number.slice(-4),
      status: 'pending',
      timestamp: new Date(),
      retryCount: 0
    };

    try {
      // Simulate payment gateway call
      const gatewayResponse = this.callPaymentGateway(transaction);

      if (gatewayResponse.success) {
        transaction.status = 'completed';
        transaction.gatewayTransactionId = gatewayResponse.transactionId;
        this.transactions.push(transaction);
        return { success: true, transaction };
      } else {
        transaction.status = 'failed';
        transaction.error = gatewayResponse.error;
        this.failedTransactions.push(transaction);
        return { success: false, error: gatewayResponse.error };
      }
    } catch (error) {
      transaction.status = 'error';
      transaction.error = error.message;
      this.failedTransactions.push(transaction);
      return { success: false, error: error.message };
    }
  }

  // Call payment gateway
  callPaymentGateway(transaction) {
    // Simulate gateway response
    const random = Math.random();
    if (random > 0.05) {  // 95% success rate
      return {
        success: true,
        transactionId: `TXN-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
      };
    } else {
      return {
        success: false,
        error: 'Payment gateway declined transaction'
      };
    }
  }

  // Refund a transaction
  refundTransaction(transactionId) {
    const transaction = this.transactions.find(t => t.id === transactionId);

    if (!transaction) {
      return { success: false, error: 'Transaction not found' };
    }

    if (transaction.status === 'refunded') {
      return { success: false, error: 'Transaction already refunded' };
    }

    if (transaction.status !== 'completed') {
      return { success: false, error: 'Can only refund completed transactions' };
    }

    transaction.status = 'refunded';
    transaction.refundedAt = new Date();

    return { success: true, transaction };
  }

  // Get transaction by ID
  getTransaction(transactionId) {
    return this.transactions.find(t => t.id === transactionId) || null;
  }

  // Get user transactions
  getUserTransactions(userId) {
    return this.transactions.filter(t => t.userId === userId);
  }

  // Get transaction history
  getTransactionHistory(limit = 100) {
    return this.transactions.slice(-limit);
  }

  // Get statistics
  getStatistics() {
    const total = this.transactions.length;
    const successful = this.transactions.filter(t => t.status === 'completed').length;
    const refunded = this.transactions.filter(t => t.status === 'refunded').length;
    const failed = this.failedTransactions.length;
    const totalAmount = this.transactions
      .filter(t => t.status === 'completed')
      .reduce((sum, t) => sum + t.amount, 0);

    return {
      totalTransactions: total,
      successfulTransactions: successful,
      refundedTransactions: refunded,
      failedTransactions: failed,
      totalAmount,
      successRate: total > 0 ? (successful / total) * 100 : 0
    };
  }

  // Generate transaction ID
  generateTransactionId() {
    return `TXN-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  // Reset state (for testing)
  reset() {
    this.transactions = [];
    this.failedTransactions = [];
  }
}

module.exports = PaymentProcessor;
