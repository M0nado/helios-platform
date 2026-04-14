/**
 * MODEL C: Low Duplication (Shared Utils)
 * 8 feature modules + 3 shared utility modules
 * Minimal duplication through shared utilities
 */

// ============ SHARED UTILITY MODULES (Non-duplicated) ============

class ValidatorUtils {
  static validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  static validateRequired(value, fieldName) {
    if (value === null || value === undefined || value === '') {
      throw new Error(`${fieldName} is required`);
    }
    return true;
  }

  static validateRating(rating) {
    if (rating < 1 || rating > 5) throw new Error('Rating must be between 1 and 5');
    return true;
  }

  static validatePrice(price) {
    if (price < 0) throw new Error('Price must be non-negative');
    return true;
  }

  static validateInventory(inventory) {
    if (inventory < 0) throw new Error('Inventory must be non-negative');
    return true;
  }

  static validatePaymentMethod(method) {
    const validMethods = ['credit_card', 'debit_card', 'paypal', 'bank_transfer'];
    if (!validMethods.includes(method)) throw new Error(`Invalid payment method: ${method}`);
    return true;
  }

  static validateOrderStatus(status) {
    const validStatuses = ['pending', 'confirmed', 'shipped', 'delivered', 'cancelled'];
    if (!validStatuses.includes(status)) throw new Error(`Invalid status: ${status}`);
    return true;
  }
}

class DataUtils {
  static generateId() {
    return Math.random().toString(36).substr(2, 9);
  }

  static getCurrentTimestamp() {
    return new Date().toISOString();
  }

  static deepClone(obj) {
    return JSON.parse(JSON.stringify(obj));
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

  static groupByDate(items, dateField) {
    const grouped = {};
    for (const item of items) {
      const date = new Date(item[dateField]).toDateString();
      grouped[date] = (grouped[date] || 0) + 1;
    }
    return grouped;
  }

  static calculateAverage(numbers) {
    return numbers.length > 0 ? numbers.reduce((a, b) => a + b, 0) / numbers.length : 0;
  }
}

class StorageUtils {
  static findById(collection, id) {
    return collection.get(id);
  }

  static findByCondition(collection, condition) {
    return Array.from(collection.values()).find(condition);
  }

  static findAllByCondition(collection, condition) {
    return Array.from(collection.values()).filter(condition);
  }

  static save(collection, id, item) {
    collection.set(id, item);
    return item;
  }

  static delete(collection, id) {
    collection.delete(id);
  }

  static getAll(collection) {
    return Array.from(collection.values());
  }
}

// ============ FEATURE MODULE 1: USER MANAGEMENT ============
class UserModule {
  constructor() {
    this.users = new Map();
  }

  registerUser(email, password, profile) {
    ValidatorUtils.validateRequired(email, 'email');
    ValidatorUtils.validateRequired(password, 'password');
    ValidatorUtils.validateEmail(email);

    const sanitizedEmail = DataUtils.sanitizeString(email);
    if (StorageUtils.findByCondition(this.users, u => DataUtils.sanitizeString(u.email) === sanitizedEmail)) {
      throw new Error('Email already registered');
    }

    const user = {
      id: DataUtils.generateId(),
      email: sanitizedEmail,
      password: DataUtils.hashPassword(password),
      profile: DataUtils.deepClone(profile),
      createdAt: DataUtils.getCurrentTimestamp(),
      lastLogin: null,
      isActive: true,
    };

    return StorageUtils.save(this.users, user.id, user);
  }

  loginUser(email, password) {
    ValidatorUtils.validateRequired(email, 'email');
    ValidatorUtils.validateRequired(password, 'password');

    const sanitizedEmail = DataUtils.sanitizeString(email);
    const user = StorageUtils.findByCondition(this.users, u => DataUtils.sanitizeString(u.email) === sanitizedEmail);
    if (!user) throw new Error('User not found');
    if (user.password !== DataUtils.hashPassword(password)) throw new Error('Invalid password');

    user.lastLogin = DataUtils.getCurrentTimestamp();
    return { userId: user.id, token: DataUtils.generateId() };
  }

  updateUser(userId, updates) {
    ValidatorUtils.validateRequired(userId, 'userId');
    const user = StorageUtils.findById(this.users, userId);
    if (!user) throw new Error('User not found');
    return Object.assign(user, DataUtils.deepClone(updates));
  }

  getUser(userId) {
    const user = StorageUtils.findById(this.users, userId);
    if (!user) throw new Error('User not found');
    return DataUtils.deepClone(user);
  }
}

// ============ FEATURE MODULE 2: PRODUCT MANAGEMENT ============
class ProductModule {
  constructor() {
    this.products = new Map();
  }

  createProduct(name, price, inventory) {
    ValidatorUtils.validateRequired(name, 'name');
    ValidatorUtils.validateRequired(price, 'price');
    ValidatorUtils.validateRequired(inventory, 'inventory');
    ValidatorUtils.validatePrice(price);
    ValidatorUtils.validateInventory(inventory);

    const product = {
      id: DataUtils.generateId(),
      name: DataUtils.sanitizeString(name),
      price: parseFloat(price),
      inventory: parseInt(inventory),
      createdAt: DataUtils.getCurrentTimestamp(),
      isActive: true,
      reviews: [],
      rating: 0,
    };

    return StorageUtils.save(this.products, product.id, product);
  }

  updateProduct(productId, updates) {
    ValidatorUtils.validateRequired(productId, 'productId');
    const product = StorageUtils.findById(this.products, productId);
    if (!product) throw new Error('Product not found');
    return Object.assign(product, DataUtils.deepClone(updates));
  }

  getProduct(productId) {
    const product = StorageUtils.findById(this.products, productId);
    if (!product) throw new Error('Product not found');
    return DataUtils.deepClone(product);
  }

  listProducts(filters = {}) {
    let products = StorageUtils.getAll(this.products);
    if (filters.minPrice !== undefined) products = products.filter(p => p.price >= filters.minPrice);
    if (filters.maxPrice !== undefined) products = products.filter(p => p.price <= filters.maxPrice);
    if (filters.inStock === true) products = products.filter(p => p.inventory > 0);
    return products.map(p => DataUtils.deepClone(p));
  }

  addProductReview(productId, userId, rating, comment) {
    ValidatorUtils.validateRequired(productId, 'productId');
    ValidatorUtils.validateRequired(userId, 'userId');
    ValidatorUtils.validateRequired(rating, 'rating');
    ValidatorUtils.validateRating(rating);

    const product = StorageUtils.findById(this.products, productId);
    if (!product) throw new Error('Product not found');

    const review = {
      id: DataUtils.generateId(),
      userId,
      rating: parseInt(rating),
      comment: DataUtils.sanitizeString(comment),
      createdAt: DataUtils.getCurrentTimestamp(),
    };

    product.reviews.push(review);
    product.rating = DataUtils.calculateAverage(product.reviews.map(r => r.rating));
    return review;
  }
}

// ============ FEATURE MODULE 3: ORDER MANAGEMENT ============
class OrderModule {
  constructor() {
    this.orders = new Map();
  }

  createOrder(userId, items, productModule) {
    ValidatorUtils.validateRequired(userId, 'userId');
    ValidatorUtils.validateRequired(items, 'items');

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
      id: DataUtils.generateId(),
      userId,
      items: orderItems,
      total,
      status: 'pending',
      createdAt: DataUtils.getCurrentTimestamp(),
      paymentStatus: 'unpaid',
    };

    return StorageUtils.save(this.orders, order.id, order);
  }

  getOrder(orderId) {
    const order = StorageUtils.findById(this.orders, orderId);
    if (!order) throw new Error('Order not found');
    return DataUtils.deepClone(order);
  }

  updateOrderStatus(orderId, status) {
    ValidatorUtils.validateRequired(orderId, 'orderId');
    ValidatorUtils.validateRequired(status, 'status');
    ValidatorUtils.validateOrderStatus(status);

    const order = StorageUtils.findById(this.orders, orderId);
    if (!order) throw new Error('Order not found');
    order.status = status;
    return order;
  }

  cancelOrder(orderId, productModule) {
    const order = StorageUtils.findById(this.orders, orderId);
    if (!order) throw new Error('Order not found');

    if (['delivered', 'cancelled'].includes(order.status)) {
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

// ============ FEATURE MODULE 4: PAYMENT PROCESSING ============
class PaymentModule {
  constructor() {
    this.payments = new Map();
  }

  processPayment(orderId, paymentMethod, orderModule) {
    ValidatorUtils.validateRequired(orderId, 'orderId');
    ValidatorUtils.validateRequired(paymentMethod, 'paymentMethod');
    ValidatorUtils.validatePaymentMethod(paymentMethod);

    const order = orderModule.getOrder(orderId);
    if (order.paymentStatus === 'paid') throw new Error('Order already paid');

    const payment = {
      id: DataUtils.generateId(),
      orderId,
      amount: order.total,
      method: paymentMethod,
      status: 'completed',
      processedAt: DataUtils.getCurrentTimestamp(),
      transactionId: DataUtils.generateId(),
    };

    StorageUtils.save(this.payments, payment.id, payment);
    order.paymentStatus = 'paid';
    return payment;
  }

  refundPayment(orderId, orderModule) {
    ValidatorUtils.validateRequired(orderId, 'orderId');

    const payment = StorageUtils.findByCondition(this.payments, p => p.orderId === orderId);
    if (!payment) throw new Error('Payment not found');

    const order = orderModule.getOrder(orderId);
    payment.status = 'refunded';
    order.paymentStatus = 'refunded';
    return payment;
  }
}

// ============ FEATURE MODULE 5: NOTIFICATION SYSTEM ============
class NotificationModule {
  constructor() {
    this.notifications = new Map();
  }

  sendNotification(userId, message) {
    ValidatorUtils.validateRequired(userId, 'userId');
    ValidatorUtils.validateRequired(message, 'message');

    const notification = {
      id: DataUtils.generateId(),
      userId,
      message: DataUtils.sanitizeString(message),
      sentAt: DataUtils.getCurrentTimestamp(),
      read: false,
    };

    return StorageUtils.save(this.notifications, notification.id, notification);
  }

  getUserNotifications(userId) {
    ValidatorUtils.validateRequired(userId, 'userId');
    return StorageUtils.findAllByCondition(this.notifications, n => n.userId === userId)
      .map(n => DataUtils.deepClone(n));
  }

  markNotificationRead(notificationId) {
    const notification = StorageUtils.findById(this.notifications, notificationId);
    if (!notification) throw new Error('Notification not found');
    notification.read = true;
    return notification;
  }
}

// ============ FEATURE MODULE 6: ANALYTICS ============
class AnalyticsModule {
  constructor() {
    this.analytics = new Map();
  }

  logAnalytics(event, data) {
    const entry = {
      id: DataUtils.generateId(),
      event,
      data: DataUtils.deepClone(data),
      timestamp: DataUtils.getCurrentTimestamp(),
    };
    return StorageUtils.save(this.analytics, entry.id, entry);
  }

  getAnalytics(eventFilter = null) {
    let events = StorageUtils.getAll(this.analytics);
    if (eventFilter) events = events.filter(e => e.event === eventFilter);
    return events.map(e => DataUtils.deepClone(e));
  }

  getAnalyticsSummary() {
    const events = StorageUtils.getAll(this.analytics);
    return {
      totalEvents: events.length,
      eventTypes: events.reduce((acc, e) => {
        acc[e.event] = (acc[e.event] || 0) + 1;
        return acc;
      }, {}),
    };
  }
}

// ============ FEATURE MODULE 7: CACHING ============
class CacheModule {
  constructor() {
    this.cache = new Map();
  }

  setCacheValue(key, value, ttl = 3600) {
    ValidatorUtils.validateRequired(key, 'key');
    this.cache.set(key, {
      value: DataUtils.deepClone(value),
      expiresAt: Date.now() + (ttl * 1000),
    });
  }

  getCacheValue(key) {
    const cached = StorageUtils.findById(this.cache, key);
    if (!cached) return null;
    if (cached.expiresAt < Date.now()) {
      StorageUtils.delete(this.cache, key);
      return null;
    }
    return DataUtils.deepClone(cached.value);
  }

  clearCache() {
    this.cache.clear();
  }
}

// ============ FEATURE MODULE 8: REPORTING ============
class ReportingModule {
  generateSalesReport(startDate, endDate, orderModule) {
    const orders = StorageUtils.getAll(orderModule.orders).filter(o => {
      const createdAt = new Date(o.createdAt);
      return createdAt >= new Date(startDate) && createdAt <= new Date(endDate);
    });

    return {
      period: { startDate, endDate },
      totalOrders: orders.length,
      totalRevenue: orders.reduce((sum, o) => sum + o.total, 0),
      averageOrderValue: DataUtils.calculateAverage(orders.map(o => o.total)),
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
    const users = StorageUtils.getAll(userModule.users);
    return {
      totalUsers: users.length,
      activeUsers: users.filter(u => u.isActive).length,
      usersByRegisterDate: DataUtils.groupByDate(users, 'createdAt'),
    };
  }
}

// ============ SYSTEM ORCHESTRATOR ============
class LowDuplicationSystem {
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

module.exports = LowDuplicationSystem;
