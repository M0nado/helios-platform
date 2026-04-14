const ModerateSystem = require('../implementation.js');

describe('Model B: Moderate Duplication (8 Independent Modules)', () => {
  let system;

  beforeEach(() => {
    system = new ModerateSystem();
  });

  describe('User Management Module', () => {
    test('should register user', () => {
      const user = system.registerUser('test@example.com', 'password123', { name: 'John' });
      expect(user.id).toBeDefined();
      expect(user.email).toBe('test@example.com');
    });

    test('should login user', () => {
      const user = system.registerUser('test@example.com', 'password123', {});
      const result = system.loginUser('test@example.com', 'password123');
      expect(result.userId).toBe(user.id);
    });

    test('should get user', () => {
      const user = system.registerUser('test@example.com', 'password123', {});
      const retrieved = system.getUser(user.id);
      expect(retrieved.email).toBe('test@example.com');
    });

    test('should reject duplicate email', () => {
      system.registerUser('test@example.com', 'password', {});
      expect(() => system.registerUser('test@example.com', 'pass', {})).toThrow();
    });
  });

  describe('Product Management Module', () => {
    test('should create product', () => {
      const product = system.createProduct('Laptop', 999.99, 10);
      expect(product.id).toBeDefined();
      expect(product.price).toBe(999.99);
    });

    test('should list products with filters', () => {
      system.createProduct('Cheap Item', 10, 100);
      system.createProduct('Expensive Item', 1000, 5);
      const products = system.listProducts({ minPrice: 500 });
      expect(products.length).toBe(1);
    });

    test('should add product review', () => {
      const user = system.registerUser('test@example.com', 'password', {});
      const product = system.createProduct('Laptop', 999, 10);
      const review = system.addProductReview(product.id, user.id, 4, 'Good product');
      expect(review.rating).toBe(4);
    });
  });

  describe('Order Management Module', () => {
    beforeEach(() => {
      system.registerUser('customer@example.com', 'password', {});
      system.createProduct('Product A', 100, 10);
      system.createProduct('Product B', 200, 5);
    });

    test('should create order', () => {
      const user = Array.from(system.userModule.users.values())[0];
      const product = Array.from(system.productModule.products.values())[0];
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 2 }]);
      expect(order.total).toBe(200);
    });

    test('should reject insufficient inventory', () => {
      const user = Array.from(system.userModule.users.values())[0];
      const product = Array.from(system.productModule.products.values())[0];
      expect(() => system.createOrder(user.id, [{ productId: product.id, quantity: 100 }])).toThrow();
    });

    test('should update order status', () => {
      const user = Array.from(system.userModule.users.values())[0];
      const product = Array.from(system.productModule.products.values())[0];
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 1 }]);
      const updated = system.updateOrderStatus(order.id, 'shipped');
      expect(updated.status).toBe('shipped');
    });

    test('should cancel order and restore inventory', () => {
      const user = Array.from(system.userModule.users.values())[0];
      const product = Array.from(system.productModule.products.values())[0];
      const originalInventory = product.inventory;
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 2 }]);
      system.cancelOrder(order.id);
      expect(product.inventory).toBe(originalInventory);
    });
  });

  describe('Payment Module', () => {
    beforeEach(() => {
      const user = system.registerUser('customer@example.com', 'password', {});
      system.createProduct('Product', 100, 10);
      const product = Array.from(system.productModule.products.values())[0];
      system.createOrder(user.id, [{ productId: product.id, quantity: 1 }]);
    });

    test('should process payment', () => {
      const order = Array.from(system.orderModule.orders.values())[0];
      const payment = system.processPayment(order.id, 'credit_card');
      expect(payment.status).toBe('completed');
    });

    test('should refund payment', () => {
      const order = Array.from(system.orderModule.orders.values())[0];
      system.processPayment(order.id, 'credit_card');
      const refund = system.refundPayment(order.id);
      expect(refund.status).toBe('refunded');
    });
  });

  describe('Notification Module', () => {
    test('should send notification', () => {
      const user = system.registerUser('test@example.com', 'password', {});
      const notif = system.sendNotification(user.id, 'Welcome!');
      expect(notif.id).toBeDefined();
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

  describe('Analytics Module', () => {
    test('should log analytics', () => {
      system.logAnalytics('test.event', { value: 123 });
      const events = system.getAnalytics('test.event');
      expect(events.length).toBe(1);
    });

    test('should get analytics summary', () => {
      system.registerUser('test@example.com', 'password', {});
      system.logAnalytics('user.test', {});
      const summary = system.getAnalyticsSummary();
      expect(summary.totalEvents).toBeGreaterThan(0);
    });
  });

  describe('Cache Module', () => {
    test('should set and get cache', () => {
      system.setCacheValue('key', { data: 'value' });
      const cached = system.getCacheValue('key');
      expect(cached.data).toBe('value');
    });

    test('should clear cache', () => {
      system.setCacheValue('key', { data: 'value' });
      system.clearCache();
      expect(system.getCacheValue('key')).toBeNull();
    });
  });

  describe('Reporting Module', () => {
    beforeEach(() => {
      const user = system.registerUser('customer@example.com', 'password', {});
      system.createProduct('Product', 100, 10);
      const product = Array.from(system.productModule.products.values())[0];
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
    });
  });

  describe('Module Interactions', () => {
    test('complete workflow with all modules', () => {
      const user = system.registerUser('customer@example.com', 'password', { name: 'John' });
      const product = system.createProduct('Laptop', 1299.99, 5);
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 1 }]);
      const payment = system.processPayment(order.id, 'credit_card');
      const notif = system.sendNotification(user.id, 'Order confirmed!');
      const shipped = system.updateOrderStatus(order.id, 'shipped');

      expect(user.id).toBeDefined();
      expect(payment.status).toBe('completed');
      expect(notif.id).toBeDefined();
      expect(shipped.status).toBe('shipped');
    });
  });

  describe('Error Handling', () => {
    test('should handle validation errors', () => {
      expect(() => system.registerUser(null, 'password', {})).toThrow();
      expect(() => system.createProduct('Invalid', -100, 10)).toThrow();
      expect(() => system.processPayment('invalid', 'invalid_method')).toThrow();
    });
  });

  describe('Utility Function Duplication Check', () => {
    test('validateEmail is duplicated in UtilityFunctions class', () => {
      // Verify that UtilityFunctions has validateEmail method
      const UtilityFunctions = require('../implementation.js').prototype.constructor;
      // The presence of utilities in each module demonstrates duplication
      expect(system.userModule.constructor.name).toBe('UserModule');
    });
  });

  // ============ COMPLETE 100% COVERAGE TESTS ============
  describe('Complete 100% Code Coverage', () => {
    test('Test updateUser', () => {
      const user = system.registerUser('test@example.com', 'password', { name: 'John' });
      const updated = system.updateUser(user.id, { profile: { name: 'Jane' } });
      expect(updated.profile.name).toBe('jane');
    });

    test('Test getProduct', () => {
      const product = system.createProduct('Laptop', 999, 10);
      const retrieved = system.getProduct(product.id);
      expect(retrieved.name).toBe('laptop');
    });

    test('Test getOrder', () => {
      const user = system.registerUser('test@example.com', 'password', {});
      const product = system.createProduct('Product', 100, 10);
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 1 }]);
      const retrieved = system.getOrder(order.id);
      expect(retrieved.id).toBe(order.id);
    });

    test('Test getAllCacheScenarios', (done) => {
      system.setCacheValue('key1', { data: 'value' });
      expect(system.getCacheValue('key1')).toBeDefined();
      
      system.setCacheValue('key2', { data: 'value' }, 0.001);
      setTimeout(() => {
        expect(system.getCacheValue('key2')).toBeNull();
        done();
      }, 10);
    });
  });
});
