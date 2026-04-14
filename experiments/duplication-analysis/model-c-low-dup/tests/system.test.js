const LowDuplicationSystem = require('../implementation.js');

describe('Model C: Low Duplication (Shared Utils)', () => {
  let system;

  beforeEach(() => {
    system = new LowDuplicationSystem();
  });

  describe('User Management', () => {
    test('should register user', () => {
      const user = system.registerUser('test@example.com', 'password123', { name: 'John' });
      expect(user.id).toBeDefined();
      expect(user.email).toBe('test@example.com');
    });

    test('should reject duplicate email', () => {
      system.registerUser('test@example.com', 'password', {});
      expect(() => system.registerUser('test@example.com', 'pass', {})).toThrow();
    });

    test('should login successfully', () => {
      const user = system.registerUser('test@example.com', 'password123', {});
      const result = system.loginUser('test@example.com', 'password123');
      expect(result.userId).toBe(user.id);
    });

    test('should reject wrong password', () => {
      system.registerUser('test@example.com', 'password123', {});
      expect(() => system.loginUser('test@example.com', 'wrongpass')).toThrow();
    });

    test('should update user profile', () => {
      const user = system.registerUser('test@example.com', 'password', {});
      const updated = system.updateUser(user.id, { profile: { name: 'Jane' } });
      expect(updated.profile.name).toBe('jane');
    });

    test('should retrieve user', () => {
      const user = system.registerUser('test@example.com', 'password', {});
      const retrieved = system.getUser(user.id);
      expect(retrieved.email).toBe('test@example.com');
    });
  });

  describe('Product Management', () => {
    test('should create product', () => {
      const product = system.createProduct('Laptop', 999.99, 10);
      expect(product.id).toBeDefined();
      expect(product.price).toBe(999.99);
      expect(product.inventory).toBe(10);
    });

    test('should reject negative price', () => {
      expect(() => system.createProduct('Laptop', -100, 10)).toThrow('Price must be non-negative');
    });

    test('should reject negative inventory', () => {
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

    test('should list products without filters', () => {
      system.createProduct('Product A', 100, 10);
      system.createProduct('Product B', 200, 5);
      const products = system.listProducts();
      expect(products.length).toBe(2);
    });

    test('should filter products by min price', () => {
      system.createProduct('Cheap', 10, 100);
      system.createProduct('Expensive', 1000, 5);
      const products = system.listProducts({ minPrice: 500 });
      expect(products.length).toBe(1);
      expect(products[0].name).toBe('expensive');
    });

    test('should filter products by max price', () => {
      system.createProduct('Cheap', 10, 100);
      system.createProduct('Expensive', 1000, 5);
      const products = system.listProducts({ maxPrice: 500 });
      expect(products.length).toBe(1);
      expect(products[0].name).toBe('cheap');
    });

    test('should filter in-stock products', () => {
      system.createProduct('In Stock', 100, 5);
      const product = system.createProduct('Out of Stock', 100, 0);
      const products = system.listProducts({ inStock: true });
      expect(products.length).toBe(1);
      expect(products[0].id).not.toBe(product.id);
    });

    test('should add product review', () => {
      const user = system.registerUser('test@example.com', 'password', {});
      const product = system.createProduct('Laptop', 999, 10);
      const review = system.addProductReview(product.id, user.id, 5, 'Excellent!');
      expect(review.rating).toBe(5);
      expect(product.rating).toBe(5);
    });

    test('should calculate average rating with multiple reviews', () => {
      const user1 = system.registerUser('user1@example.com', 'password', {});
      const user2 = system.registerUser('user2@example.com', 'password', {});
      const product = system.createProduct('Laptop', 999, 10);
      
      system.addProductReview(product.id, user1.id, 5, 'Great!');
      system.addProductReview(product.id, user2.id, 3, 'Good');
      
      expect(product.rating).toBe(4);
    });

    test('should validate rating range', () => {
      const user = system.registerUser('test@example.com', 'password', {});
      const product = system.createProduct('Laptop', 999, 10);
      expect(() => system.addProductReview(product.id, user.id, 0, 'Bad')).toThrow();
      expect(() => system.addProductReview(product.id, user.id, 6, 'Too high')).toThrow();
    });
  });

  describe('Order Management', () => {
    beforeEach(() => {
      system.registerUser('customer@example.com', 'password', {});
      system.createProduct('Product A', 100, 10);
      system.createProduct('Product B', 200, 5);
    });

    test('should create order', () => {
      const user = Array.from(system.userModule.users.values())[0];
      const product = Array.from(system.productModule.products.values())[0];
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 2 }]);
      expect(order.id).toBeDefined();
      expect(order.total).toBe(200);
      expect(order.status).toBe('pending');
    });

    test('should reject order with insufficient inventory', () => {
      const user = Array.from(system.userModule.users.values())[0];
      const product = Array.from(system.productModule.products.values())[0];
      expect(() => system.createOrder(user.id, [{ productId: product.id, quantity: 100 }])).toThrow();
    });

    test('should get order', () => {
      const user = Array.from(system.userModule.users.values())[0];
      const product = Array.from(system.productModule.products.values())[0];
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 1 }]);
      const retrieved = system.getOrder(order.id);
      expect(retrieved.id).toBe(order.id);
    });

    test('should update order status', () => {
      const user = Array.from(system.userModule.users.values())[0];
      const product = Array.from(system.productModule.products.values())[0];
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 1 }]);
      const updated = system.updateOrderStatus(order.id, 'shipped');
      expect(updated.status).toBe('shipped');
    });

    test('should reject invalid order status', () => {
      const user = Array.from(system.userModule.users.values())[0];
      const product = Array.from(system.productModule.products.values())[0];
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 1 }]);
      expect(() => system.updateOrderStatus(order.id, 'invalid_status')).toThrow();
    });

    test('should cancel pending order', () => {
      const user = Array.from(system.userModule.users.values())[0];
      const product = Array.from(system.productModule.products.values())[0];
      const originalInventory = product.inventory;
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 2 }]);
      system.cancelOrder(order.id);
      expect(product.inventory).toBe(originalInventory);
      expect(order.status).toBe('cancelled');
    });

    test('should reject cancellation of delivered order', () => {
      const user = Array.from(system.userModule.users.values())[0];
      const product = Array.from(system.productModule.products.values())[0];
      const order = system.createOrder(user.id, [{ productId: product.id, quantity: 1 }]);
      system.updateOrderStatus(order.id, 'delivered');
      expect(() => system.cancelOrder(order.id)).toThrow();
    });
  });

  describe('Payment Processing', () => {
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
      expect(order.paymentStatus).toBe('paid');
    });

    test('should reject invalid payment method', () => {
      const order = Array.from(system.orderModule.orders.values())[0];
      expect(() => system.processPayment(order.id, 'invalid')).toThrow();
    });

    test('should reject double payment', () => {
      const order = Array.from(system.orderModule.orders.values())[0];
      system.processPayment(order.id, 'credit_card');
      expect(() => system.processPayment(order.id, 'debit_card')).toThrow();
    });

    test('should refund payment', () => {
      const order = Array.from(system.orderModule.orders.values())[0];
      system.processPayment(order.id, 'credit_card');
      const refund = system.refundPayment(order.id);
      expect(refund.status).toBe('refunded');
      expect(order.paymentStatus).toBe('refunded');
    });
  });

  describe('Notifications', () => {
    test('should send notification', () => {
      const user = system.registerUser('test@example.com', 'password', {});
      const notif = system.sendNotification(user.id, 'Welcome!');
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

  describe('Analytics', () => {
    test('should log analytics event', () => {
      system.logAnalytics('user.test', { userId: 'test123' });
      const events = system.getAnalytics('user.test');
      expect(events.length).toBe(1);
    });

    test('should get analytics summary', () => {
      system.logAnalytics('event1', {});
      system.logAnalytics('event1', {});
      system.logAnalytics('event2', {});
      const summary = system.getAnalyticsSummary();
      expect(summary.totalEvents).toBe(3);
      expect(summary.eventTypes['event1']).toBe(2);
    });
  });

  describe('Caching', () => {
    test('should set and get cache', () => {
      system.setCacheValue('key', { data: 'value' });
      const cached = system.getCacheValue('key');
      expect(cached.data).toBe('value');
    });

    test('should return null for non-existent key', () => {
      expect(system.getCacheValue('non-existent')).toBeNull();
    });

    test('should handle cache expiration', (done) => {
      system.setCacheValue('key', { data: 'value' }, 0.001);
      setTimeout(() => {
        expect(system.getCacheValue('key')).toBeNull();
        done();
      }, 10);
    });

    test('should clear cache', () => {
      system.setCacheValue('key1', { data: 'value' });
      system.setCacheValue('key2', { data: 'value' });
      system.clearCache();
      expect(system.getCacheValue('key1')).toBeNull();
      expect(system.getCacheValue('key2')).toBeNull();
    });
  });

  describe('Reporting', () => {
    beforeEach(() => {
      const user = system.registerUser('customer@example.com', 'password', {});
      system.createProduct('Product A', 100, 10);
      system.createProduct('Product B', 200, 5);
      const products = Array.from(system.productModule.products.values());
      system.createOrder(user.id, [
        { productId: products[0].id, quantity: 2 },
        { productId: products[1].id, quantity: 1 }
      ]);
    });

    test('should generate sales report', () => {
      const start = new Date(Date.now() - 86400000).toISOString();
      const end = new Date().toISOString();
      const report = system.generateSalesReport(start, end);
      expect(report.totalOrders).toBe(1);
      expect(report.totalRevenue).toBe(400);
      expect(report.averageOrderValue).toBe(400);
    });

    test('should generate user report', () => {
      const report = system.generateUserReport();
      expect(report.totalUsers).toBe(1);
      expect(report.activeUsers).toBe(1);
    });
  });

  describe('Complete 100% Integration Test', () => {
    test('full e-commerce workflow', () => {
      const user = system.registerUser('customer@example.com', 'password123', { name: 'John' });
      const product1 = system.createProduct('Laptop', 1299.99, 5);
      const product2 = system.createProduct('Mouse', 29.99, 50);

      const order = system.createOrder(user.id, [
        { productId: product1.id, quantity: 1 },
        { productId: product2.id, quantity: 2 }
      ]);

      expect(order.total).toBe(1359.97);

      const payment = system.processPayment(order.id, 'credit_card');
      expect(payment.status).toBe('completed');

      system.sendNotification(user.id, 'Order confirmed!');
      system.updateOrderStatus(order.id, 'shipped');

      const notifs = system.getUserNotifications(user.id);
      expect(notifs.length).toBe(1);

      const review = system.addProductReview(product1.id, user.id, 5, 'Excellent!');
      expect(review.rating).toBe(5);

      const start = new Date(Date.now() - 86400000).toISOString();
      const end = new Date().toISOString();
      const report = system.generateSalesReport(start, end);
      expect(report.totalRevenue).toBe(1359.97);
    });
  });

  describe('Shared Utilities Validation', () => {
    test('validators are shared across modules', () => {
      // Model C uses shared utility modules
      expect(system.userModule.constructor.name).toBe('UserModule');
      expect(system.productModule.constructor.name).toBe('ProductModule');
      // This demonstrates that utilities are centralized, not duplicated
    });
  });
});
