/**
 * MODEL B: Moderate Duplication
 * 8 independent feature modules, each with duplicated utilities (~20% overhead)
 * Each module: ~60KB with self-contained utilities
 */

// ============ SHARED UTILITIES (DUPLICATED IN EACH MODULE) ============

class UtilityFunctions {
  static validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  static generateId() {
    return Math.random().toString(36).substr(2, 9);
  }

  static getCurrentTimestamp() {
    return new Date().toISOString();
  }

  static deepClone(obj) {
    return JSON.parse(JSON.stringify(obj));
  }

  static validateRequired(value, fieldName) {
    if (value === null || value === undefined || value === '') {
      throw new Error(`${fieldName} is required`);
    }
    return true;
  }

  static sanitizeString(str) {
    return str.trim().toLowerCase();
  }

  static hashPassword(password) {
    let hash = 0;
    for (let i = 0; i < password.length; i++) {
      hash = ((hash << 5) - hash) + password.charCodeAt(i);
      hash = hash & hash;
    }
    return Math.abs(hash).toString(36);
  }
}

// ============ MODULE 1: USER MANAGEMENT ============
class UserModule {
  constructor() {
    this.users = new Map();
  }

  registerUser(email, password, profile) {
    UtilityFunctions.validateRequired(email, 'email');
    UtilityFunctions.validateRequired(password, 'password');
    
    if (!UtilityFunctions.validateEmail(email)) {
      throw new Error('Invalid email format');
    }

    const sanitizedEmail = UtilityFunctions.sanitizeString(email);
    if (Array.from(this.users.values()).some(u => UtilityFunctions.sanitizeString(u.email) === sanitizedEmail)) {
      throw new Error('Email already registered');
    }

    const user = {
      id: UtilityFunctions.generateId(),
      email: sanitizedEmail,
      password: UtilityFunctions.hashPassword(password),
      profile: UtilityFunctions.deepClone(profile),
      createdAt: UtilityFunctions.getCurrentTimestamp(),
      lastLogin: null,
      isActive: true,
    };

    this.users.set(user.id, user);
    return user;
  }

  loginUser(email, password) {
    UtilityFunctions.validateRequired(email, 'email');
    UtilityFunctions.validateRequired(password, 'password');

    const sanitizedEmail = UtilityFunctions.sanitizeString(email);
    const user = Array.from(this.users.values()).find(u => UtilityFunctions.sanitizeString(u.email) === sanitizedEmail);

    if (!user) throw new Error('User not found');
    if (user.password !== UtilityFunctions.hashPassword(password)) throw new Error('Invalid password');

    user.lastLogin = UtilityFunctions.getCurrentTimestamp();
    return { userId: user.id, token: UtilityFunctions.generateId() };
  }

  updateUser(userId, updates) {
    UtilityFunctions.validateRequired(userId, 'userId');
    const user = this.users.get(userId);
    if (!user) throw new Error('User not found');

    Object.assign(user, UtilityFunctions.deepClone(updates));
    return user;
  }

  getUser(userId) {
    const user = this.users.get(userId);
    if (!user) throw new Error('User not found');
    return UtilityFunctions.deepClone(user);
  }
}

// ============ MODULE 2: PRODUCT MANAGEMENT ============
class ProductModule {
  constructor() {
    this.products = new Map();
  }

  createProduct(name, price, inventory) {
    UtilityFunctions.validateRequired(name, 'name');
    UtilityFunctions.validateRequired(price, 'price');
    UtilityFunctions.validateRequired(inventory, 'inventory');

    if (price < 0) throw new Error('Price must be non-negative');
    if (inventory < 0) throw new Error('Inventory must be non-negative');

    const product = {
      id: UtilityFunctions.generateId(),
      name: UtilityFunctions.sanitizeString(name),
      price: parseFloat(price),
      inventory: parseInt(inventory),
      createdAt: UtilityFunctions.getCurrentTimestamp(),
      isActive: true,
      reviews: [],
      rating: 0,
    };

    this.products.set(product.id, product);
    return product;
  }

  updateProduct(productId, updates) {
    UtilityFunctions.validateRequired(productId, 'productId');
    const product = this.products.get(productId);
    if (!product) throw new Error('Product not found');

    Object.assign(product, UtilityFunctions.deepClone(updates));
    return product;
  }

  getProduct(productId) {
    const product = this.products.get(productId);
    if (!product) throw new Error('Product not found');
    return UtilityFunctions.deepClone(product);
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

    return products.map(p => UtilityFunctions.deepClone(p));
  }

  addProductReview(productId, userId, rating, comment) {
    UtilityFunctions.validateRequired(productId, 'productId');
    UtilityFunctions.validateRequired(userId, 'userId');
    UtilityFunctions.validateRequired(rating, 'rating');

    const product = this.products.get(productId);
    if (!product) throw new Error('Product not found');

    if (rating < 1 || rating > 5) throw new Error('Rating must be between 1 and 5');

    const review = {
      id: UtilityFunctions.generateId(),
      userId,
      rating: parseInt(rating),
      comment: UtilityFunctions.sanitizeString(comment),
      createdAt: UtilityFunctions.getCurrentTimestamp(),
    };

    product.reviews.push(review);
    product.rating = product.reviews.reduce((sum, r) => sum + r.rating, 0) / product.reviews.length;
    return review;
  }
}

// ============ MODULE 3: ORDER MANAGEMENT ============
class OrderModule {
  constructor() {
    this.orders = new Map();
  }

  createOrder(userId, items, productModule) {
    UtilityFunctions.validateRequired(userId, 'userId');
    UtilityFunctions.validateRequired(items, 'items');

    let total = 0;
    const orderItems = [];

    for (const item of items) {
      const product = productModule.getProduct(item.productId);
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
      id: UtilityFunctions.generateId(),
      userId,
      items: orderItems,
      total,
      status: 'pending',
      createdAt: UtilityFunctions.getCurrentTimestamp(),
      paymentStatus: 'unpaid',
      shippingAddress: null,
    };

    this.orders.set(order.id, order);
    return order;
  }

  getOrder(orderId) {
    const order = this.orders.get(orderId);
    if (!order) throw new Error('Order not found');
    return UtilityFunctions.deepClone(order);
  }

  updateOrderStatus(orderId, status) {
    UtilityFunctions.validateRequired(orderId, 'orderId');
    UtilityFunctions.validateRequired(status, 'status');

    const validStatuses = ['pending', 'confirmed', 'shipped', 'delivered', 'cancelled'];
    if (!validStatuses.includes(status)) throw new Error(`Invalid status: ${status}`);

    const order = this.orders.get(orderId);
    if (!order) throw new Error('Order not found');

    order.status = status;
    return order;
  }

  cancelOrder(orderId, productModule) {
    const order = this.orders.get(orderId);
    if (!order) throw new Error('Order not found');

    if (order.status === 'delivered' || order.status === 'cancelled') {
      throw new Error(`Cannot cancel order with status: ${order.status}`);
    }

    for (const item of order.items) {
      const product = productModule.getProduct(item.productId);
      product.inventory += item.quantity;
    }

    order.status = 'cancelled';
    return order;
  }
}

// ============ MODULE 4: PAYMENT PROCESSING ============
class PaymentModule {
  constructor() {
    this.payments = new Map();
  }

  processPayment(orderId, paymentMethod, orderModule) {
    UtilityFunctions.validateRequired(orderId, 'orderId');
    UtilityFunctions.validateRequired(paymentMethod, 'paymentMethod');

    const order = orderModule.getOrder(orderId);
    if (order.paymentStatus === 'paid') throw new Error('Order already paid');

    const validMethods = ['credit_card', 'debit_card', 'paypal', 'bank_transfer'];
    if (!validMethods.includes(paymentMethod)) throw new Error(`Invalid payment method: ${paymentMethod}`);

    const payment = {
      id: UtilityFunctions.generateId(),
      orderId,
      amount: order.total,
      method: paymentMethod,
      status: 'completed',
      processedAt: UtilityFunctions.getCurrentTimestamp(),
      transactionId: UtilityFunctions.generateId(),
    };

    this.payments.set(payment.id, payment);
    order.paymentStatus = 'paid';
    return payment;
  }

  refundPayment(orderId, orderModule) {
    UtilityFunctions.validateRequired(orderId, 'orderId');

    const payment = Array.from(this.payments.values()).find(p => p.orderId === orderId);
    if (!payment) throw new Error('Payment not found');

    const order = orderModule.getOrder(orderId);
    payment.status = 'refunded';
    order.paymentStatus = 'refunded';
    return payment;
  }
}

// ============ MODULE 5: NOTIFICATION SYSTEM ============
class NotificationModule {
  constructor() {
    this.notifications = new Map();
  }

  sendNotification(userId, message) {
    UtilityFunctions.validateRequired(userId, 'userId');
    UtilityFunctions.validateRequired(message, 'message');

    const notification = {
      id: UtilityFunctions.generateId(),
      userId,
      message: UtilityFunctions.sanitizeString(message),
      sentAt: UtilityFunctions.getCurrentTimestamp(),
      read: false,
    };

    this.notifications.set(notification.id, notification);
    return notification;
  }

  getUserNotifications(userId) {
    UtilityFunctions.validateRequired(userId, 'userId');

    return Array.from(this.notifications.values())
      .filter(n => n.userId === userId)
      .map(n => UtilityFunctions.deepClone(n));
  }

  markNotificationRead(notificationId) {
    const notification = this.notifications.get(notificationId);
    if (!notification) throw new Error('Notification not found');

    notification.read = true;
    return notification;
  }
}

// ============ MODULE 6: ANALYTICS ============
class AnalyticsModule {
  constructor() {
    this.analytics = new Map();
  }

  logAnalytics(event, data) {
    const entry = {
      id: UtilityFunctions.generateId(),
      event,
      data: UtilityFunctions.deepClone(data),
      timestamp: UtilityFunctions.getCurrentTimestamp(),
    };

    this.analytics.set(entry.id, entry);
  }

  getAnalytics(eventFilter = null) {
    let events = Array.from(this.analytics.values());

    if (eventFilter) {
      events = events.filter(e => e.event === eventFilter);
    }

    return events.map(e => UtilityFunctions.deepClone(e));
  }

  getAnalyticsSummary() {
    return {
      totalEvents: this.analytics.size,
      eventTypes: Array.from(this.analytics.values()).reduce((acc, e) => {
        acc[e.event] = (acc[e.event] || 0) + 1;
        return acc;
      }, {}),
    };
  }
}

// ============ MODULE 7: CACHING ============
class CacheModule {
  constructor() {
    this.cache = new Map();
  }

  setCacheValue(key, value, ttl = 3600) {
    UtilityFunctions.validateRequired(key, 'key');

    this.cache.set(key, {
      value: UtilityFunctions.deepClone(value),
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

    return UtilityFunctions.deepClone(cached.value);
  }

  clearCache() {
    this.cache.clear();
  }
}

// ============ MODULE 8: REPORTING ============
class ReportingModule {
  constructor() {
    this.reports = new Map();
  }

  generateSalesReport(startDate, endDate, orderModule) {
    const orders = Array.from(orderModule.orders.values()).filter(o => {
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

  generateUserReport(userModule) {
    return {
      totalUsers: userModule.users.size,
      activeUsers: Array.from(userModule.users.values()).filter(u => u.isActive).length,
      usersByRegisterDate: this.groupByDate(Array.from(userModule.users.values()), 'createdAt'),
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

// ============ SYSTEM ORCHESTRATOR ============
class ModerateSystem {
  constructor() {
    this.userModule = new UserModule();
    this.productModule = new ProductModule();
    this.orderModule = new OrderModule();
    this.paymentModule = new PaymentModule();
    this.notificationModule = new NotificationModule();
    this.analyticsModule = new AnalyticsModule();
    this.cacheModule = new CacheModule();
    this.reportingModule = new ReportingModule();
  }

  // Delegate to modules
  registerUser(...args) { return this.userModule.registerUser(...args); }
  loginUser(...args) { return this.userModule.loginUser(...args); }
  updateUser(...args) { return this.userModule.updateUser(...args); }
  getUser(...args) { return this.userModule.getUser(...args); }

  createProduct(...args) { return this.productModule.createProduct(...args); }
  updateProduct(...args) { return this.productModule.updateProduct(...args); }
  getProduct(...args) { return this.productModule.getProduct(...args); }
  listProducts(...args) { return this.productModule.listProducts(...args); }
  addProductReview(...args) { return this.productModule.addProductReview(...args); }

  createOrder(userId, items) { return this.orderModule.createOrder(userId, items, this.productModule); }
  getOrder(...args) { return this.orderModule.getOrder(...args); }
  updateOrderStatus(...args) { return this.orderModule.updateOrderStatus(...args); }
  cancelOrder(orderId) { return this.orderModule.cancelOrder(orderId, this.productModule); }

  processPayment(orderId, method) { return this.paymentModule.processPayment(orderId, method, this.orderModule); }
  refundPayment(orderId) { return this.paymentModule.refundPayment(orderId, this.orderModule); }

  sendNotification(...args) { return this.notificationModule.sendNotification(...args); }
  getUserNotifications(...args) { return this.notificationModule.getUserNotifications(...args); }
  markNotificationRead(...args) { return this.notificationModule.markNotificationRead(...args); }

  logAnalytics(...args) { return this.analyticsModule.logAnalytics(...args); }
  getAnalytics(...args) { return this.analyticsModule.getAnalytics(...args); }
  getAnalyticsSummary(...args) { return this.analyticsModule.getAnalyticsSummary(...args); }

  setCacheValue(...args) { return this.cacheModule.setCacheValue(...args); }
  getCacheValue(...args) { return this.cacheModule.getCacheValue(...args); }
  clearCache(...args) { return this.cacheModule.clearCache(...args); }

  generateSalesReport(start, end) { return this.reportingModule.generateSalesReport(start, end, this.orderModule); }
  generateUserReport() { return this.reportingModule.generateUserReport(this.userModule); }
}

module.exports = ModerateSystem;
