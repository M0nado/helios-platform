const MonolithicSystem = require('../implementation.js');

describe('Model A: Monolithic System - Complete Test Suite', () => {
  let system;

  beforeEach(() => {
    system = new MonolithicSystem();
  });

  // ============ USER MANAGEMENT TESTS ============
  describe('User Management', () => {
    test('should register user with valid data', () => {
      const user = system.registerUser('test@example.com', 'password123', { name: 'John' });
      expect(user.id).toBeDefined();
      expect(user.email).toBe('test@example.com');
      expect(user.isActive).toBe(true);
    });

    test('should reject duplicate email', () => {
      system.registerUser('test@example.com', 'password123', {});
      expect(() => system.registerUser('test@example.com', 'password456', {})).toThrow('Email already registered');
    });

    test('should login with correct credentials', () => {
      const user = system.registerUser('test@example.com', 'password123', {});
      const result = system.loginUser('test@example.com', 'password123');
      expect(result.userId).toBe(user.id);
      expect(result.token).toBeDefined();
    });

    test('should reject login with wrong password', () => {
      system.registerUser('test@example.com', 'password123', {});
      expect(() => system.loginUser('test@example.com', 'wrongpassword')).toThrow('Invalid password');
    });

    test('should update user', () => {
      const user = system.registerUser('test@example.com', 'password123', {});
      const updated = system.updateUser(user.id, { profile: { name: 'Jane' } });
      expect(updated.profile.name).toBe('jane');
    });

    test('should get user', () => {
      const user = system.registerUser('test@example.com', 'password123', { name: 'John' });
      const retrieved = system.getUser(user.id);
      expect(retrieved.email).toBe('test@example.com');
    });
  });

  // ============ PRODUCT MANAGEMENT TESTS ============
  describe('Product Management', () => {
    test('should create product', () => {
      const product = system.createProduct('Laptop', 999.99, 10);
      expect(product.id).toBeDefined();
      expect(product.name).toBe('laptop');
      expect(product.price).toBe(999.99);
    });

    test('should reject invalid price', () => {
      expect(() => system.createProduct('Laptop', -100, 10)).toThrow('Price must be non-negative');
    });

    test('should reject invalid inventory', () => {
      expect(() => system.createProduct('Laptop', 999, -5)).toThrow('Inventory must be non-negative');
    });

    test('should update product', () => {
      const product = system.createProduct('Laptop', 999, 10);
      const updated = system.updateProduct(product.id, { price: 899 });
      expect(updated.price).toBe(899);
    });

    test('should get product', () => {
      const product = system.createProduct('Laptop', 999, 10);
      const retrieved = system.getProduct(product.id);
      expect(retrieved.name).toBe('laptop');
    });

    test('should list products with filters', () => {
      system.createProduct('Cheap Item', 10, 100);
      system.createProduct('Expensive Item', 1000, 5);
      const products = system.listProducts({ minPrice: 500 });
      expect(products.length).toBe(1);
      expect(products[0].name).toBe('expensive item');
    });

    test('should add product review', () => {
      const user = system.registerUser('test@example.com', 'password', {});
      const product = system.createProduct('Laptop', 999, 10);
      const review = system.addProductReview(product.id, user.id, 5, 'Great product!');
      expect(review.rating).toBe(5);
      expect(product.rating).toBe(5);
    });
  });

  // ============ ORDER MANAGEMENT TESTS ============
  describe('Order Management', () => {
    beforeEach(() => {
      system.registerUser('customer@example.com', 'password', {});
      system.createProduct('Product A', 100, 10);
      system.createProduct('Product B', 200, 5);
    });

    test('should create order', () => {
      const user = Array.from(system.users.values())[0];
      const product = Array.from(system.products.values())[0];
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 2 }]);
      expect(order.id).toBeDefined();
      expect(order.total).toBe(200);
      expect(order.status).toBe('pending');
    });

    test('should reject order with insufficient inventory', () => {
      const user = Array.from(system.users.values())[0];
      const product = Array.from(system.products.values())[0];
      expect(() => system.createOrder(user.id, [{ productId: product.id, quantity: 100 }])).toThrow('Insufficient inventory');
    });

    test('should update order status', () => {
      const user = Array.from(system.users.values())[0];
      const product = Array.from(system.products.values())[0];
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 1 }]);
      const updated = system.updateOrderStatus(order.id, 'shipped');
      expect(updated.status).toBe('shipped');
    });

    test('should cancel order', () => {
      const user = Array.from(system.users.values())[0];
      const product = Array.from(system.products.values())[0];
      const originalInventory = product.inventory;
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 2 }]);
      system.cancelOrder(order.id);
      expect(product.inventory).toBe(originalInventory);
      expect(order.status).toBe('cancelled');
    });
  });

  // ============ PAYMENT TESTS ============
  describe('Payment Processing', () => {
    beforeEach(() => {
      const user = system.registerUser('customer@example.com', 'password', {});
      system.createProduct('Product', 100, 10);
      const product = Array.from(system.products.values())[0];
      system.createOrder(user.id, [{ productId: product.id, quantity: 1 }]);
    });

    test('should process payment', () => {
      const order = Array.from(system.orders.values())[0];
      const payment = system.processPayment(order.id, 'credit_card');
      expect(payment.status).toBe('completed');
      expect(order.paymentStatus).toBe('paid');
    });

    test('should reject invalid payment method', () => {
      const order = Array.from(system.orders.values())[0];
      expect(() => system.processPayment(order.id, 'invalid_method')).toThrow('Invalid payment method');
    });

    test('should refund payment', () => {
      const order = Array.from(system.orders.values())[0];
      system.processPayment(order.id, 'credit_card');
      const refund = system.refundPayment(order.id);
      expect(refund.status).toBe('refunded');
    });
  });

  // ============ NOTIFICATION TESTS ============
  describe('Notifications', () => {
    test('should send notification', () => {
      const user = system.registerUser('test@example.com', 'password', {});
      const notif = system.sendNotification(user.id, 'Welcome to our store!');
      expect(notif.id).toBeDefined();
      expect(notif.read).toBe(false);
    });

    test('should get user notifications', () => {
      const user = system.registerUser('test@example.com', 'password', {});
      system.sendNotification(user.id, 'Message 1');
      system.sendNotification(user.id, 'Message 2');
      const notifs = system.getUserNotifications(user.id);
      expect(notifs.length).toBe(2);
    });

    test('should mark notification read', () => {
      const user = system.registerUser('test@example.com', 'password', {});
      const notif = system.sendNotification(user.id, 'Test');
      const marked = system.markNotificationRead(notif.id);
      expect(marked.read).toBe(true);
    });
  });

  // ============ ANALYTICS TESTS ============
  describe('Analytics', () => {
    test('should log analytics event', () => {
      system.logAnalytics('user.registered', { userId: 'test123' });
      const events = system.getAnalytics('user.registered');
      expect(events.length).toBe(1);
      expect(events[0].event).toBe('user.registered');
    });

    test('should get analytics summary', () => {
      system.registerUser('test@example.com', 'password', {});
      const summary = system.getAnalyticsSummary();
      expect(summary.totalUsers).toBe(1);
    });
  });

  // ============ CACHE TESTS ============
  describe('Caching', () => {
    test('should set and get cache value', () => {
      system.setCacheValue('key1', { data: 'value' });
      const cached = system.getCacheValue('key1');
      expect(cached.data).toBe('value');
    });

    test('should return null for expired cache', (done) => {
      system.setCacheValue('key2', { data: 'value' }, 0.001);
      setTimeout(() => {
        const cached = system.getCacheValue('key2');
        expect(cached).toBeNull();
        done();
      }, 10);
    });

    test('should clear cache', () => {
      system.setCacheValue('key1', { data: 'value' });
      system.clearCache();
      const cached = system.getCacheValue('key1');
      expect(cached).toBeNull();
    });
  });

  // ============ REPORTING TESTS ============
  describe('Reporting', () => {
    beforeEach(() => {
      const user = system.registerUser('customer@example.com', 'password', {});
      system.createProduct('Product', 100, 10);
      const product = Array.from(system.products.values())[0];
      system.createOrder(user.id, [{ productId: product.id, quantity: 2 }]);
    });

    test('should generate sales report', () => {
      const start = new Date(Date.now() - 86400000).toISOString();
      const end = new Date().toISOString();
      const report = system.generateSalesReport(start, end);
      expect(report.totalOrders).toBe(1);
      expect(report.totalRevenue).toBe(200);
    });

    test('should generate user report', () => {
      const report = system.generateUserReport();
      expect(report.totalUsers).toBe(1);
      expect(report.activeUsers).toBe(1);
    });
  });

  // ============ INTEGRATION TESTS ============
  describe('Full Integration Workflow', () => {
    test('complete e-commerce flow', () => {
      // Register user
      const user = system.registerUser('customer@example.com', 'password123', { name: 'John Doe' });
      expect(user.id).toBeDefined();

      // Add product
      const product = system.createProduct('Laptop', 1299.99, 5);
      expect(product.id).toBeDefined();

      // Create order
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 1 }]);
      expect(order.total).toBe(1299.99);

      // Process payment
      const payment = system.processPayment(order.id, 'credit_card');
      expect(payment.status).toBe('completed');

      // Send notification
      const notif = system.sendNotification(user.id, 'Order confirmed!');
      expect(notif.id).toBeDefined();

      // Update order status
      const shipped = system.updateOrderStatus(order.id, 'shipped');
      expect(shipped.status).toBe('shipped');

      // Get analytics
      const summary = system.getAnalyticsSummary();
      expect(summary.totalUsers).toBe(1);
    });
  });

  // ============ ERROR HANDLING TESTS ============
  describe('Error Handling', () => {
    test('should handle missing required fields', () => {
      expect(() => system.registerUser(null, 'password', {})).toThrow('email is required');
      expect(() => system.registerUser('test@example.com', null, {})).toThrow('password is required');
    });

    test('should handle invalid email format', () => {
      expect(() => system.registerUser('invalid-email', 'password', {})).toThrow('Invalid email format');
    });

    test('should handle non-existent resources', () => {
      expect(() => system.getUser('non-existent')).toThrow('User not found');
      expect(() => system.getProduct('non-existent')).toThrow('Product not found');
      expect(() => system.getOrder('non-existent')).toThrow('Order not found');
    });
  });
});
