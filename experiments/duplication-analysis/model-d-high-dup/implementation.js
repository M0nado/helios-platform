/**
 * MODEL D: Very High Duplication (Aggressive Parallel)
 * 16 independent micro-modules with duplicated utilities in each
 * No sharing, maximum modularity, 30-40% duplication
 */

// Each micro-module has its own utility functions (DUPLICATION!)

// ============ MICRO-MODULE 1: User Auth ============
class UserAuthModule {
  constructor() {
    this.users = new Map();
  }

  validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  sanitizeString(str) { return str.trim().toLowerCase(); }
  generateId() { return Math.random().toString(36).substr(2, 9); }
  getCurrentTimestamp() { return new Date().toISOString(); }
  deepClone(obj) { return JSON.parse(JSON.stringify(obj)); }
  hashPassword(password) { let hash = 0; for (let i = 0; i < password.length; i++) { hash = ((hash << 5) - hash) + password.charCodeAt(i); } return Math.abs(hash).toString(36); }

  registerUser(email, password, profile) {
    if (!email || !password) throw new Error('Email and password required');
    if (!this.validateEmail(email)) throw new Error('Invalid email');
    const user = { id: this.generateId(), email: this.sanitizeString(email), password: this.hashPassword(password), profile: this.deepClone(profile), createdAt: this.getCurrentTimestamp(), lastLogin: null, isActive: true };
    this.users.set(user.id, user);
    return user;
  }

  loginUser(email, password) {
    const user = Array.from(this.users.values()).find(u => this.sanitizeString(u.email) === this.sanitizeString(email));
    if (!user || user.password !== this.hashPassword(password)) throw new Error('Invalid credentials');
    user.lastLogin = this.getCurrentTimestamp();
    return { userId: user.id, token: this.generateId() };
  }

  getUser(userId) {
    const user = this.users.get(userId);
    if (!user) throw new Error('User not found');
    return this.deepClone(user);
  }
}

// ============ MICRO-MODULE 2: User Profile ============
class UserProfileModule {
  constructor() { this.profiles = new Map(); }
  sanitizeString(str) { return str.trim().toLowerCase(); }
  generateId() { return Math.random().toString(36).substr(2, 9); }
  deepClone(obj) { return JSON.parse(JSON.stringify(obj)); }

  updateProfile(userId, updates) {
    if (!userId) throw new Error('UserId required');
    if (!this.profiles.has(userId)) { this.profiles.set(userId, { id: userId }); }
    Object.assign(this.profiles.get(userId), this.deepClone(updates));
    return this.profiles.get(userId);
  }

  getProfile(userId) {
    const profile = this.profiles.get(userId);
    if (!profile) throw new Error('Profile not found');
    return this.deepClone(profile);
  }
}

// ============ MICRO-MODULE 3: Product Catalog ============
class ProductCatalogModule {
  constructor() { this.products = new Map(); }
  sanitizeString(str) { return str.trim().toLowerCase(); }
  generateId() { return Math.random().toString(36).substr(2, 9); }
  getCurrentTimestamp() { return new Date().toISOString(); }
  deepClone(obj) { return JSON.parse(JSON.stringify(obj)); }

  createProduct(name, price, inventory) {
    if (!name || price < 0 || inventory < 0) throw new Error('Invalid product data');
    const product = { id: this.generateId(), name: this.sanitizeString(name), price: parseFloat(price), inventory: parseInt(inventory), createdAt: this.getCurrentTimestamp(), reviews: [], rating: 0 };
    this.products.set(product.id, product);
    return product;
  }

  getProduct(productId) {
    const product = this.products.get(productId);
    if (!product) throw new Error('Product not found');
    return this.deepClone(product);
  }

  listProducts(filters = {}) {
    let products = Array.from(this.products.values());
    if (filters.minPrice) products = products.filter(p => p.price >= filters.minPrice);
    if (filters.maxPrice) products = products.filter(p => p.price <= filters.maxPrice);
    return products;
  }
}

// ============ MICRO-MODULE 4: Product Reviews ============
class ProductReviewModule {
  constructor(productModule) { this.productModule = productModule; }
  generateId() { return Math.random().toString(36).substr(2, 9); }
  getCurrentTimestamp() { return new Date().toISOString(); }
  sanitizeString(str) { return str.trim().toLowerCase(); }

  addReview(productId, userId, rating, comment) {
    if (!productId || !userId || rating < 1 || rating > 5) throw new Error('Invalid review data');
    const product = this.productModule.getProduct(productId);
    const review = { id: this.generateId(), userId, rating: parseInt(rating), comment: this.sanitizeString(comment), createdAt: this.getCurrentTimestamp() };
    product.reviews.push(review);
    product.rating = product.reviews.reduce((sum, r) => sum + r.rating, 0) / product.reviews.length;
    return review;
  }
}

// ============ MICRO-MODULE 5: Inventory Management ============
class InventoryModule {
  constructor() { this.inventory = new Map(); }
  generateId() { return Math.random().toString(36).substr(2, 9); }
  getCurrentTimestamp() { return new Date().toISOString(); }

  updateInventory(productId, quantity) {
    const inventoryRecord = this.inventory.get(productId) || { id: this.generateId(), productId, quantity: 0, lastUpdated: this.getCurrentTimestamp() };
    inventoryRecord.quantity = quantity;
    inventoryRecord.lastUpdated = this.getCurrentTimestamp();
    this.inventory.set(productId, inventoryRecord);
    return inventoryRecord;
  }

  getInventory(productId) {
    return this.inventory.get(productId);
  }
}

// ============ MICRO-MODULE 6: Order Creation ============
class OrderCreationModule {
  constructor() { this.orders = new Map(); }
  generateId() { return Math.random().toString(36).substr(2, 9); }
  getCurrentTimestamp() { return new Date().toISOString(); }
  deepClone(obj) { return JSON.parse(JSON.stringify(obj)); }

  createOrder(userId, items, productModule) {
    if (!userId || !items) throw new Error('UserId and items required');
    let total = 0;
    const orderItems = [];
    for (const item of items) {
      const product = productModule.getProduct(item.productId);
      if (product.inventory < item.quantity) throw new Error('Insufficient inventory');
      const itemTotal = product.price * item.quantity;
      total += itemTotal;
      orderItems.push({ productId: product.id, productName: product.name, quantity: item.quantity, unitPrice: product.price, itemTotal });
      product.inventory -= item.quantity;
    }
    const order = { id: this.generateId(), userId, items: orderItems, total, status: 'pending', createdAt: this.getCurrentTimestamp(), paymentStatus: 'unpaid' };
    this.orders.set(order.id, order);
    return order;
  }

  getOrder(orderId) {
    const order = this.orders.get(orderId);
    if (!order) throw new Error('Order not found');
    return this.deepClone(order);
  }
}

// ============ MICRO-MODULE 7: Order Status ============
class OrderStatusModule {
  constructor() { this.orders = new Map(); }

  updateStatus(orderId, status, orderModule) {
    const validStatuses = ['pending', 'confirmed', 'shipped', 'delivered', 'cancelled'];
    if (!validStatuses.includes(status)) throw new Error('Invalid status');
    const order = orderModule.getOrder(orderId);
    order.status = status;
    return order;
  }

  cancelOrder(orderId, orderModule, productModule) {
    const order = orderModule.getOrder(orderId);
    if (['delivered', 'cancelled'].includes(order.status)) throw new Error('Cannot cancel');
    for (const item of order.items) {
      const product = productModule.getProduct(item.productId);
      product.inventory += item.quantity;
    }
    order.status = 'cancelled';
    return order;
  }
}

// ============ MICRO-MODULE 8: Payment Processing ============
class PaymentModule {
  constructor() { this.payments = new Map(); }
  generateId() { return Math.random().toString(36).substr(2, 9); }
  getCurrentTimestamp() { return new Date().toISOString(); }

  processPayment(orderId, method, orderModule) {
    const validMethods = ['credit_card', 'debit_card', 'paypal', 'bank_transfer'];
    if (!validMethods.includes(method)) throw new Error('Invalid method');
    const order = orderModule.getOrder(orderId);
    if (order.paymentStatus === 'paid') throw new Error('Already paid');
    const payment = { id: this.generateId(), orderId, amount: order.total, method, status: 'completed', processedAt: this.getCurrentTimestamp(), transactionId: this.generateId() };
    this.payments.set(payment.id, payment);
    order.paymentStatus = 'paid';
    return payment;
  }

  refundPayment(orderId, orderModule) {
    const payment = Array.from(this.payments.values()).find(p => p.orderId === orderId);
    if (!payment) throw new Error('Payment not found');
    const order = orderModule.getOrder(orderId);
    payment.status = 'refunded';
    order.paymentStatus = 'refunded';
    return payment;
  }
}

// ============ MICRO-MODULE 9: Notifications ============
class NotificationModule {
  constructor() { this.notifications = new Map(); }
  generateId() { return Math.random().toString(36).substr(2, 9); }
  getCurrentTimestamp() { return new Date().toISOString(); }
  sanitizeString(str) { return str.trim().toLowerCase(); }

  sendNotification(userId, message) {
    if (!userId || !message) throw new Error('UserId and message required');
    const notification = { id: this.generateId(), userId, message: this.sanitizeString(message), sentAt: this.getCurrentTimestamp(), read: false };
    this.notifications.set(notification.id, notification);
    return notification;
  }

  getUserNotifications(userId) {
    return Array.from(this.notifications.values()).filter(n => n.userId === userId);
  }

  markRead(notificationId) {
    const notification = this.notifications.get(notificationId);
    if (!notification) throw new Error('Not found');
    notification.read = true;
    return notification;
  }
}

// ============ MICRO-MODULE 10: Events & Analytics ============
class EventsModule {
  constructor() { this.events = new Map(); }
  generateId() { return Math.random().toString(36).substr(2, 9); }
  getCurrentTimestamp() { return new Date().toISOString(); }
  deepClone(obj) { return JSON.parse(JSON.stringify(obj)); }

  logEvent(eventType, data) {
    const event = { id: this.generateId(), eventType, data: this.deepClone(data), timestamp: this.getCurrentTimestamp() };
    this.events.set(event.id, event);
    return event;
  }

  getEvents(filter = null) {
    let events = Array.from(this.events.values());
    if (filter) events = events.filter(e => e.eventType === filter);
    return events;
  }
}

// ============ MICRO-MODULE 11: Caching ============
class CacheModule {
  constructor() { this.cache = new Map(); }

  set(key, value, ttl = 3600) {
    this.cache.set(key, { value, expiresAt: Date.now() + (ttl * 1000) });
  }

  get(key) {
    const cached = this.cache.get(key);
    if (!cached) return null;
    if (cached.expiresAt < Date.now()) { this.cache.delete(key); return null; }
    return cached.value;
  }

  clear() { this.cache.clear(); }
}

// ============ MICRO-MODULE 12: Reports - Sales ============
class SalesReportModule {
  calculateAverage(nums) { return nums.length > 0 ? nums.reduce((a, b) => a + b, 0) / nums.length : 0; }

  generateSalesReport(startDate, endDate, orderModule) {
    const orders = Array.from(orderModule.orders.values()).filter(o => {
      const createdAt = new Date(o.createdAt);
      return createdAt >= new Date(startDate) && createdAt <= new Date(endDate);
    });
    return {
      period: { startDate, endDate },
      totalOrders: orders.length,
      totalRevenue: orders.reduce((sum, o) => sum + o.total, 0),
      averageOrderValue: this.calculateAverage(orders.map(o => o.total)),
      ordersCount: { pending: orders.filter(o => o.status === 'pending').length, confirmed: orders.filter(o => o.status === 'confirmed').length, shipped: orders.filter(o => o.status === 'shipped').length, delivered: orders.filter(o => o.status === 'delivered').length, cancelled: orders.filter(o => o.status === 'cancelled').length }
    };
  }
}

// ============ MICRO-MODULE 13: Reports - Users ============
class UserReportModule {
  groupByDate(items, dateField) {
    const grouped = {};
    for (const item of items) {
      const date = new Date(item[dateField]).toDateString();
      grouped[date] = (grouped[date] || 0) + 1;
    }
    return grouped;
  }

  generateUserReport(userModule) {
    const users = Array.from(userModule.users.values());
    return { totalUsers: users.length, activeUsers: users.filter(u => u.isActive).length, usersByRegisterDate: this.groupByDate(users, 'createdAt') };
  }
}

// ============ MICRO-MODULE 14: Audit Logging ============
class AuditModule {
  constructor() { this.logs = new Map(); }
  generateId() { return Math.random().toString(36).substr(2, 9); }
  getCurrentTimestamp() { return new Date().toISOString(); }

  log(action, userId, target) {
    const logEntry = { id: this.generateId(), action, userId, target, timestamp: this.getCurrentTimestamp() };
    this.logs.set(logEntry.id, logEntry);
  }

  getLogs(userId = null) {
    let logs = Array.from(this.logs.values());
    if (userId) logs = logs.filter(l => l.userId === userId);
    return logs;
  }
}

// ============ MICRO-MODULE 15: Config & Settings ============
class ConfigModule {
  constructor() {
    this.settings = new Map();
    this.settings.set('maxOrderItems', 100);
    this.settings.set('paymentMethods', ['credit_card', 'debit_card', 'paypal', 'bank_transfer']);
    this.settings.set('orderStatuses', ['pending', 'confirmed', 'shipped', 'delivered', 'cancelled']);
  }

  getSetting(key) {
    return this.settings.get(key);
  }

  setSetting(key, value) {
    this.settings.set(key, value);
  }
}

// ============ MICRO-MODULE 16: Health Check ============
class HealthModule {
  getCurrentTimestamp() { return new Date().toISOString(); }

  getSystemHealth(system) {
    return {
      timestamp: this.getCurrentTimestamp(),
      modules: {
        users: system.userAuthModule.users.size,
        products: system.productModule.products.size,
        orders: system.orderModule.orders.size,
        payments: system.paymentModule.payments.size,
        notifications: system.notificationModule.notifications.size,
        cache: system.cacheModule.cache.size,
      },
      status: 'healthy'
    };
  }
}

// ============ SYSTEM ORCHESTRATOR ============
class HighDuplicationSystem {
  constructor() {
    this.userAuthModule = new UserAuthModule();
    this.userProfileModule = new UserProfileModule();
    this.productModule = new ProductCatalogModule();
    this.reviewModule = new ProductReviewModule(this.productModule);
    this.inventoryModule = new InventoryModule();
    this.orderModule = new OrderCreationModule();
    this.orderStatusModule = new OrderStatusModule();
    this.paymentModule = new PaymentModule();
    this.notificationModule = new NotificationModule();
    this.eventsModule = new EventsModule();
    this.cacheModule = new CacheModule();
    this.salesReportModule = new SalesReportModule();
    this.userReportModule = new UserReportModule();
    this.auditModule = new AuditModule();
    this.configModule = new ConfigModule();
    this.healthModule = new HealthModule();
  }

  registerUser(...args) { return this.userAuthModule.registerUser(...args); }
  loginUser(...args) { return this.userAuthModule.loginUser(...args); }
  getUser(...args) { return this.userAuthModule.getUser(...args); }
  updateProfile(...args) { return this.userProfileModule.updateProfile(...args); }

  createProduct(...args) { return this.productModule.createProduct(...args); }
  getProduct(...args) { return this.productModule.getProduct(...args); }
  listProducts(...args) { return this.productModule.listProducts(...args); }
  addReview(...args) { return this.reviewModule.addReview(...args); }

  createOrder(userId, items) { return this.orderModule.createOrder(userId, items, this.productModule); }
  getOrder(...args) { return this.orderModule.getOrder(...args); }
  updateOrderStatus(orderId, status) { return this.orderStatusModule.updateStatus(orderId, status, this.orderModule); }
  cancelOrder(orderId) { return this.orderStatusModule.cancelOrder(orderId, this.orderModule, this.productModule); }

  processPayment(orderId, method) { return this.paymentModule.processPayment(orderId, method, this.orderModule); }
  refundPayment(orderId) { return this.paymentModule.refundPayment(orderId, this.orderModule); }

  sendNotification(...args) { return this.notificationModule.sendNotification(...args); }
  getUserNotifications(...args) { return this.notificationModule.getUserNotifications(...args); }
  markNotificationRead(...args) { return this.notificationModule.markRead(...args); }

  logEvent(...args) { return this.eventsModule.logEvent(...args); }
  getEvents(...args) { return this.eventsModule.getEvents(...args); }

  setCacheValue(k, v, ttl) { return this.cacheModule.set(k, v, ttl); }
  getCacheValue(k) { return this.cacheModule.get(k); }
  clearCache() { return this.cacheModule.clear(); }

  generateSalesReport(start, end) { return this.salesReportModule.generateSalesReport(start, end, this.orderModule); }
  generateUserReport() { return this.userReportModule.generateUserReport(this.userAuthModule); }

  auditLog(...args) { return this.auditModule.log(...args); }
  getAuditLogs(...args) { return this.auditModule.getLogs(...args); }

  getSystemHealth() { return this.healthModule.getSystemHealth(this); }
}

module.exports = HighDuplicationSystem;
