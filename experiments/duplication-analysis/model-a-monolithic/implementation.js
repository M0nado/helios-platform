/**
 * MODEL A: Monolithic (No Duplication)
 * Single 500KB module with all features integrated
 * - All functionality in one place
 * - No code duplication
 * - High complexity, tight coupling
 */

class MonolithicSystem {
  constructor() {
    this.users = new Map();
    this.products = new Map();
    this.orders = new Map();
    this.payments = new Map();
    this.notifications = new Map();
    this.analytics = new Map();
    this.cache = new Map();
  }

  // ============= SHARED UTILITIES (1 instance) =============
  validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  generateId() {
    return Math.random().toString(36).substr(2, 9);
  }

  getCurrentTimestamp() {
    return new Date().toISOString();
  }

  deepClone(obj) {
    return JSON.parse(JSON.stringify(obj));
  }

  validateRequired(value, fieldName) {
    if (value === null || value === undefined || value === '') {
      throw new Error(`${fieldName} is required`);
    }
    return true;
  }

  sanitizeString(str) {
    return str.trim().toLowerCase();
  }

  hashPassword(password) {
    let hash = 0;
    for (let i = 0; i < password.length; i++) {
      hash = ((hash << 5) - hash) + password.charCodeAt(i);
      hash = hash & hash;
    }
    return Math.abs(hash).toString(36);
  }

  // ============= USER MANAGEMENT =============
  registerUser(email, password, profile) {
    this.validateRequired(email, 'email');
    this.validateRequired(password, 'password');
    
    if (!this.validateEmail(email)) {
      throw new Error('Invalid email format');
    }

    const sanitizedEmail = this.sanitizeString(email);
    if (Array.from(this.users.values()).some(u => this.sanitizeString(u.email) === sanitizedEmail)) {
      throw new Error('Email already registered');
    }

    const user = {
      id: this.generateId(),
      email: sanitizedEmail,
      password: this.hashPassword(password),
      profile: this.deepClone(profile),
      createdAt: this.getCurrentTimestamp(),
      lastLogin: null,
      isActive: true,
    };

    this.users.set(user.id, user);
    this.logAnalytics('user.registered', { userId: user.id });
    return user;
  }

  loginUser(email, password) {
    this.validateRequired(email, 'email');
    this.validateRequired(password, 'password');

    const sanitizedEmail = this.sanitizeString(email);
    const user = Array.from(this.users.values()).find(u => this.sanitizeString(u.email) === sanitizedEmail);

    if (!user) {
      this.logAnalytics('user.login_failed', { email: sanitizedEmail });
      throw new Error('User not found');
    }

    if (user.password !== this.hashPassword(password)) {
      this.logAnalytics('user.login_failed', { userId: user.id });
      throw new Error('Invalid password');
    }

    user.lastLogin = this.getCurrentTimestamp();
    this.logAnalytics('user.login', { userId: user.id });
    return { userId: user.id, token: this.generateId() };
  }

  updateUser(userId, updates) {
    this.validateRequired(userId, 'userId');
    const user = this.users.get(userId);
    if (!user) throw new Error('User not found');

    Object.assign(user, this.deepClone(updates));
    this.logAnalytics('user.updated', { userId: user.id });
    return user;
  }

  getUser(userId) {
    const user = this.users.get(userId);
    if (!user) throw new Error('User not found');
    return this.deepClone(user);
  }

  // ============= PRODUCT MANAGEMENT =============
  createProduct(name, price, inventory) {
    this.validateRequired(name, 'name');
    this.validateRequired(price, 'price');
    this.validateRequired(inventory, 'inventory');

    if (price < 0) throw new Error('Price must be non-negative');
    if (inventory < 0) throw new Error('Inventory must be non-negative');

    const product = {
      id: this.generateId(),
      name: this.sanitizeString(name),
      price: parseFloat(price),
      inventory: parseInt(inventory),
      createdAt: this.getCurrentTimestamp(),
      isActive: true,
      reviews: [],
      rating: 0,
    };

    this.products.set(product.id, product);
    this.logAnalytics('product.created', { productId: product.id, price });
    return product;
  }

  updateProduct(productId, updates) {
    this.validateRequired(productId, 'productId');
    const product = this.products.get(productId);
    if (!product) throw new Error('Product not found');

    Object.assign(product, this.deepClone(updates));
    this.logAnalytics('product.updated', { productId: product.id });
    return product;
  }

  getProduct(productId) {
    const product = this.products.get(productId);
    if (!product) throw new Error('Product not found');
    return this.deepClone(product);
  }

  listProducts(filters = {}) {
    let products = Array.from(this.products.values());

    if (filters.minPrice !== undefined) {
      products = products.filter(p => p.price >= filters.minPrice);
    }
    if (filters.maxPrice !== undefined) {
      products = products.filter(p => p.price <= filters.maxPrice);
    }
    if (filters.inStock === true) {
      products = products.filter(p => p.inventory > 0);
    }

    return products.map(p => this.deepClone(p));
  }

  addProductReview(productId, userId, rating, comment) {
    this.validateRequired(productId, 'productId');
    this.validateRequired(userId, 'userId');
    this.validateRequired(rating, 'rating');

    const product = this.products.get(productId);
    if (!product) throw new Error('Product not found');
    if (!this.users.get(userId)) throw new Error('User not found');

    if (rating < 1 || rating > 5) throw new Error('Rating must be between 1 and 5');

    const review = {
      id: this.generateId(),
      userId,
      rating: parseInt(rating),
      comment: this.sanitizeString(comment),
      createdAt: this.getCurrentTimestamp(),
    };

    product.reviews.push(review);
    product.rating = product.reviews.reduce((sum, r) => sum + r.rating, 0) / product.reviews.length;
    this.logAnalytics('product.reviewed', { productId, userId, rating });
    return review;
  }

  // ============= ORDER MANAGEMENT =============
  createOrder(userId, items) {
    this.validateRequired(userId, 'userId');
    this.validateRequired(items, 'items');

    const user = this.users.get(userId);
    if (!user) throw new Error('User not found');

    let total = 0;
    const orderItems = [];

    for (const item of items) {
      const product = this.products.get(item.productId);
      if (!product) throw new Error(`Product ${item.productId} not found`);
      if (product.inventory < item.quantity) throw new Error(`Insufficient inventory for ${product.name}`);

      const itemTotal = product.price * item.quantity;
      total += itemTotal;

      orderItems.push({
        productId: product.id,
        productName: product.name,
        quantity: item.quantity,
        unitPrice: product.price,
        itemTotal,
      });

      product.inventory -= item.quantity;
    }

    const order = {
      id: this.generateId(),
      userId,
      items: orderItems,
      total,
      status: 'pending',
      createdAt: this.getCurrentTimestamp(),
      paymentStatus: 'unpaid',
      shippingAddress: null,
    };

    this.orders.set(order.id, order);
    this.logAnalytics('order.created', { orderId: order.id, userId, total });
    this.sendNotification(userId, `Order ${order.id} created with total $${total.toFixed(2)}`);
    return order;
  }

  getOrder(orderId) {
    const order = this.orders.get(orderId);
    if (!order) throw new Error('Order not found');
    return this.deepClone(order);
  }

  updateOrderStatus(orderId, status) {
    this.validateRequired(orderId, 'orderId');
    this.validateRequired(status, 'status');

    const validStatuses = ['pending', 'confirmed', 'shipped', 'delivered', 'cancelled'];
    if (!validStatuses.includes(status)) throw new Error(`Invalid status: ${status}`);

    const order = this.orders.get(orderId);
    if (!order) throw new Error('Order not found');

    order.status = status;
    this.logAnalytics('order.status_changed', { orderId, status });
    this.sendNotification(order.userId, `Order ${orderId} status: ${status}`);
    return order;
  }

  cancelOrder(orderId) {
    const order = this.orders.get(orderId);
    if (!order) throw new Error('Order not found');

    if (order.status === 'delivered' || order.status === 'cancelled') {
      throw new Error(`Cannot cancel order with status: ${order.status}`);
    }

    for (const item of order.items) {
      const product = this.products.get(item.productId);
      if (product) product.inventory += item.quantity;
    }

    order.status = 'cancelled';
    this.logAnalytics('order.cancelled', { orderId });
    this.sendNotification(order.userId, `Order ${orderId} has been cancelled`);
    return order;
  }

  // ============= PAYMENT PROCESSING =============
  processPayment(orderId, paymentMethod) {
    this.validateRequired(orderId, 'orderId');
    this.validateRequired(paymentMethod, 'paymentMethod');

    const order = this.orders.get(orderId);
    if (!order) throw new Error('Order not found');

    if (order.paymentStatus === 'paid') throw new Error('Order already paid');

    const validMethods = ['credit_card', 'debit_card', 'paypal', 'bank_transfer'];
    if (!validMethods.includes(paymentMethod)) throw new Error(`Invalid payment method: ${paymentMethod}`);

    const payment = {
      id: this.generateId(),
      orderId,
      amount: order.total,
      method: paymentMethod,
      status: 'completed',
      processedAt: this.getCurrentTimestamp(),
      transactionId: this.generateId(),
    };

    this.payments.set(payment.id, payment);
    order.paymentStatus = 'paid';
    this.logAnalytics('payment.processed', { orderId, amount: order.total, method: paymentMethod });
    this.sendNotification(order.userId, `Payment of $${order.total.toFixed(2)} confirmed`);
    return payment;
  }

  refundPayment(orderId) {
    this.validateRequired(orderId, 'orderId');

    const payment = Array.from(this.payments.values()).find(p => p.orderId === orderId);
    if (!payment) throw new Error('Payment not found');

    const order = this.orders.get(orderId);
    if (!order) throw new Error('Order not found');

    payment.status = 'refunded';
    order.paymentStatus = 'refunded';
    this.logAnalytics('payment.refunded', { orderId, amount: payment.amount });
    this.sendNotification(order.userId, `Refund of $${payment.amount.toFixed(2)} processed`);
    return payment;
  }

  // ============= NOTIFICATION SYSTEM =============
  sendNotification(userId, message) {
    this.validateRequired(userId, 'userId');
    this.validateRequired(message, 'message');

    const user = this.users.get(userId);
    if (!user) throw new Error('User not found');

    const notification = {
      id: this.generateId(),
      userId,
      message: this.sanitizeString(message),
      sentAt: this.getCurrentTimestamp(),
      read: false,
    };

    this.notifications.set(notification.id, notification);
    this.logAnalytics('notification.sent', { userId });
    return notification;
  }

  getUserNotifications(userId) {
    this.validateRequired(userId, 'userId');

    return Array.from(this.notifications.values())
      .filter(n => n.userId === userId)
      .map(n => this.deepClone(n));
  }

  markNotificationRead(notificationId) {
    const notification = this.notifications.get(notificationId);
    if (!notification) throw new Error('Notification not found');

    notification.read = true;
    return notification;
  }

  // ============= ANALYTICS =============
  logAnalytics(event, data) {
    const entry = {
      id: this.generateId(),
      event,
      data: this.deepClone(data),
      timestamp: this.getCurrentTimestamp(),
    };

    this.analytics.set(entry.id, entry);
  }

  getAnalytics(eventFilter = null) {
    let events = Array.from(this.analytics.values());

    if (eventFilter) {
      events = events.filter(e => e.event === eventFilter);
    }

    return events.map(e => this.deepClone(e));
  }

  getAnalyticsSummary() {
    const summary = {
      totalEvents: this.analytics.size,
      totalUsers: this.users.size,
      totalProducts: this.products.size,
      totalOrders: this.orders.size,
      totalPayments: this.payments.size,
      totalNotifications: this.notifications.size,
      eventTypes: {},
    };

    for (const event of this.analytics.values()) {
      summary.eventTypes[event.event] = (summary.eventTypes[event.event] || 0) + 1;
    }

    return summary;
  }

  // ============= CACHING =============
  setCacheValue(key, value, ttl = 3600) {
    this.validateRequired(key, 'key');

    this.cache.set(key, {
      value: this.deepClone(value),
      expiresAt: Date.now() + (ttl * 1000),
    });
  }

  getCacheValue(key) {
    const cached = this.cache.get(key);
    if (!cached) return null;

    if (cached.expiresAt < Date.now()) {
      this.cache.delete(key);
      return null;
    }

    return this.deepClone(cached.value);
  }

  clearCache() {
    this.cache.clear();
  }

  // ============= REPORTING =============
  generateSalesReport(startDate, endDate) {
    const orders = Array.from(this.orders.values()).filter(o => {
      const createdAt = new Date(o.createdAt);
      return createdAt >= new Date(startDate) && createdAt <= new Date(endDate);
    });

    return {
      period: { startDate, endDate },
      totalOrders: orders.length,
      totalRevenue: orders.reduce((sum, o) => sum + o.total, 0),
      averageOrderValue: orders.length > 0 ? orders.reduce((sum, o) => sum + o.total, 0) / orders.length : 0,
      ordersCount: {
        pending: orders.filter(o => o.status === 'pending').length,
        confirmed: orders.filter(o => o.status === 'confirmed').length,
        shipped: orders.filter(o => o.status === 'shipped').length,
        delivered: orders.filter(o => o.status === 'delivered').length,
        cancelled: orders.filter(o => o.status === 'cancelled').length,
      },
    };
  }

  generateUserReport() {
    return {
      totalUsers: this.users.size,
      activeUsers: Array.from(this.users.values()).filter(u => u.isActive).length,
      usersByRegisterDate: this.groupByDate(Array.from(this.users.values()), 'createdAt'),
    };
  }

  groupByDate(items, dateField) {
    const grouped = {};
    for (const item of items) {
      const date = new Date(item[dateField]).toDateString();
      grouped[date] = (grouped[date] || 0) + 1;
    }
    return grouped;
  }
}

module.exports = MonolithicSystem;
