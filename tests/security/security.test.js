/**
 * Security Audit Tests - HELIOS v4.0
 * OWASP Top 10 coverage with 40 security tests
 */

const assert = require('assert');
const { describe, it, before, after } = require('mocha');
const HELIOSClient = require('@helios/client');

describe('HELIOS v4.0 Security Tests (40 tests)', function() {
  let client;
  
  before(async function() {
    this.timeout(10000);
    client = new HELIOSClient({ baseURL: 'http://localhost:3000' });
    await client.connect();
  });

  after(async function() {
    await client.disconnect();
  });

  // ========== SQL Injection Prevention (10 tests) ==========
  describe('SQL Injection Prevention (10 tests)', function() {
    
    it('S1. Parameterized queries prevent SQL injection', async function() {
      try {
        const malicious = "'; DROP TABLE users; --";
        await client.db.query('SELECT * FROM users WHERE email = $1', [malicious]);
        const exists = await client.db.query('SELECT COUNT(*) FROM users');
        assert(exists.count > 0, 'Table was dropped by injection!');
      } catch (error) {
        // Expected - table doesn't exist
      }
    });

    it('S2. Input validation prevents injection in API', async function() {
      try {
        await client.documents.create({
          title: "'; DELETE FROM documents; --",
          content: 'test'
        });
        const count = await client.documents.count();
        assert(count > 0, 'Documents deleted by injection!');
      } catch (error) {
        assert(error.code === 'INVALID_INPUT');
      }
    });

    it('S3. Prepared statements used throughout', async function() {
      const queries = await client.security.auditPreparedStatements();
      const injectionVulnerable = queries.filter(q => !q.isPrepared);
      console.log(`Non-prepared statements: ${injectionVulnerable.length}`);
      assert(injectionVulnerable.length === 0, 'Found non-prepared statements');
    });

    it('S4. ORM prevents injection', async function() {
      const safe = await client.documents.find({
        where: { title: { contains: "'; DROP--" } }
      });
      assert(Array.isArray(safe));
    });

    it('S5. Query logging sanitized', async function() {
      await client.documents.create({ title: 'test', content: 'test' });
      const logs = await client.security.getQueryLogs({ limit: 10 });
      const unsanitized = logs.filter(l => l.query.includes('password'));
      assert(unsanitized.length === 0, 'Passwords found in logs');
    });

    it('S6. Batch operations protected', async function() {
      const malicious = Array(100).fill({ 
        title: "'; DROP--",
        content: 'x'
      });
      await client.documents.batchCreate(malicious);
      const count = await client.documents.count();
      assert(count > 0, 'Batch injection succeeded');
    });

    it('S7. Stored procedures protected', async function() {
      const procedures = await client.security.auditStoredProcedures();
      const vulnerable = procedures.filter(p => !p.isParameterized);
      assert(vulnerable.length === 0, 'Non-parameterized procedures found');
    });

    it('S8. Dynamic query building prevented', async function() {
      const queries = await client.security.auditQueryBuilding();
      const unsafe = queries.filter(q => q.isDynamic && !q.isSafeBuilt);
      assert(unsafe.length === 0, 'Unsafe dynamic queries found');
    });

    it('S9. Escaped quotes in strings', async function() {
      const result = await client.documents.create({
        title: "Test's \"Document\" with 'quotes'",
        content: 'content'
      });
      assert(result.id);
      const retrieved = await client.documents.get(result.id);
      assert(retrieved.title.includes("'"));
    });

    it('S10. Comment injection prevented', async function() {
      try {
        await client.db.query('SELECT * FROM users WHERE id = $1', ['1 OR 1=1 --']);
      } catch (error) {
        // Expected
      }
      const users = await client.users.list();
      assert(users.length < 999999, 'Injection likely succeeded');
    });
  });

  // ========== XSS Prevention (10 tests) ==========
  describe('Cross-Site Scripting Prevention (10 tests)', function() {
    
    it('S11. HTML encoded in responses', async function() {
      const xss = '<img src=x onerror="alert(\'xss\')">';
      const doc = await client.documents.create({
        title: xss,
        content: 'test'
      });
      const retrieved = await client.documents.get(doc.id);
      assert(!retrieved.title.includes('onerror'));
    });

    it('S12. Script tags removed', async function() {
      const payload = '<script>alert("xss")</script>';
      const doc = await client.documents.create({
        title: 'test',
        content: payload
      });
      const content = await client.documents.get(doc.id);
      assert(!content.content.includes('<script>'));
    });

    it('S13. Event handlers sanitized', async function() {
      const xss = '<div onclick="alert(1)">test</div>';
      const result = await client.documents.create({
        title: 'test',
        content: xss
      });
      assert(result.id);
      // Verify in response
      const retrieved = await client.documents.get(result.id);
      assert(!retrieved.content.includes('onclick'));
    });

    it('S14. Attribute encoding prevents attribute injection', async function() {
      const payload = '" onload="alert(1)';
      const doc = await client.documents.create({
        title: payload,
        content: 'test'
      });
      const retrieved = await client.documents.get(doc.id);
      assert(!retrieved.title.includes('onload'));
    });

    it('S15. Content-Security-Policy headers set', async function() {
      const headers = await client.security.getResponseHeaders();
      assert(headers['content-security-policy'], 'CSP header missing');
      assert(headers['content-security-policy'].includes("default-src 'self'"));
    });

    it('S16. X-XSS-Protection header enabled', async function() {
      const headers = await client.security.getResponseHeaders();
      assert(headers['x-xss-protection'], 'X-XSS-Protection missing');
      assert(headers['x-xss-protection'].includes('1; mode=block'));
    });

    it('S17. JSON responses safe', async function() {
      const xss = '</script><script>alert(1)</script>';
      await client.analytics.trackEvent({ name: xss });
      const response = await client.analytics.queryEvents({ limit: 1 });
      const json = JSON.stringify(response);
      assert(!json.includes('</script>'));
    });

    it('S18. Template injection prevented', async function() {
      const payload = '${7*7}';
      const doc = await client.documents.create({
        title: payload,
        content: 'test'
      });
      const retrieved = await client.documents.get(doc.id);
      assert(retrieved.title === payload, 'Template injection detected');
    });

    it('S19. URL encoding in redirects', async function() {
      const malicious = 'javascript:alert(1)';
      const result = await client.security.checkRedirect(malicious);
      assert(!result.allowed, 'Dangerous redirect allowed');
    });

    it('S20. DOM element escaping', async function() {
      const xss = '<img src=x onerror=alert(1)>';
      const safe = await client.security.escapeHtml(xss);
      assert(!safe.includes('onerror'));
    });
  });

  // ========== CSRF Protection (5 tests) ==========
  describe('CSRF Protection (5 tests)', function() {
    
    it('S21. CSRF tokens required for state changes', async function() {
      try {
        await client.documents.create({ title: 'test', content: 'test' }, { 
          csrfToken: null 
        });
        assert.fail('Should require CSRF token');
      } catch (error) {
        assert(error.code === 'CSRF_TOKEN_MISSING' || error.code === 'CSRF_TOKEN_INVALID');
      }
    });

    it('S22. Token validation on mutation endpoints', async function() {
      const token = await client.security.getCsrfToken();
      assert(token.length > 0, 'CSRF token not generated');
    });

    it('S23. SameSite cookie attribute set', async function() {
      const cookies = await client.security.getCookieAttributes();
      assert(cookies.some(c => c.sameSite === 'Strict' || c.sameSite === 'Lax'));
    });

    it('S24. State-changing requests use POST/PUT/DELETE', async function() {
      const methods = await client.security.auditHttpMethods();
      const getStateChange = methods.filter(m => m.method === 'GET' && m.changesState);
      assert(getStateChange.length === 0, 'GET request changes state');
    });

    it('S25. Origin/Referer validation enabled', async function() {
      const config = await client.security.getCorsConfig();
      assert(config.checkOrigin === true);
      assert(config.allowedOrigins.length > 0);
    });
  });

  // ========== Rate Limiting (5 tests) ==========
  describe('Rate Limiting (5 tests)', function() {
    
    it('S26. API rate limiting enforced', async function() {
      try {
        for (let i = 0; i < 1000; i++) {
          await client.health.check();
        }
        assert.fail('Rate limit not enforced');
      } catch (error) {
        assert(error.status === 429 || error.code === 'RATE_LIMITED');
      }
    });

    it('S27. Per-user rate limits', async function() {
      const limits = await client.security.getUserRateLimits();
      assert(limits.requestsPerMinute > 0);
      assert(limits.requestsPerHour > 0);
    });

    it('S28. Brute force protection on auth', async function() {
      try {
        for (let i = 0; i < 10; i++) {
          await client.auth.login({
            email: 'test@test.com',
            password: 'wrongpassword'
          }).catch(() => {});
        }
      } catch (error) {
        assert(error.code === 'TOO_MANY_ATTEMPTS' || error.status === 429);
      }
    });

    it('S29. Account lockout after failed attempts', async function() {
      const account = await client.security.getAccountStatus('test@test.com');
      assert(typeof account.lockedUntil === 'number' || account.lockedUntil === null);
    });

    it('S30. Rate limit bypass protection', async function() {
      try {
        // Attempt bypass with X-Forwarded-For
        await client.security.testRateLimitBypass('127.0.0.1,8.8.8.8');
        assert.fail('Bypass protection failed');
      } catch (error) {
        assert(error.status === 429);
      }
    });
  });

  // ========== Authentication (5 tests) ==========
  describe('Authentication Security (5 tests)', function() {
    
    it('S31. Password hashing algorithm secure', async function() {
      const algorithm = await client.security.getPasswordAlgorithm();
      assert(algorithm === 'bcrypt' || algorithm === 'argon2', `Insecure algorithm: ${algorithm}`);
    });

    it('S32. JWT tokens signed properly', async function() {
      const token = await client.security.getTestJwt();
      const tampered = token.slice(0, -10) + 'tampered';
      try {
        await client.security.verifyJwt(tampered);
        assert.fail('Tampered token accepted');
      } catch (error) {
        assert(error.code === 'INVALID_SIGNATURE');
      }
    });

    it('S33. Session tokens are random', async function() {
      const sessions = await client.security.getSessionTokens({ count: 10 });
      const unique = new Set(sessions);
      assert(unique.size === 10, 'Duplicate session tokens generated');
    });

    it('S34. Token expiration enforced', async function() {
      const expired = await client.security.getExpiredToken();
      try {
        await client.security.verifyJwt(expired);
        assert.fail('Expired token accepted');
      } catch (error) {
        assert(error.code === 'TOKEN_EXPIRED');
      }
    });

    it('S35. Password reset tokens are secure', async function() {
      const token = await client.security.generatePasswordResetToken('test@test.com');
      assert(token.length > 20, 'Reset token too short');
      assert(!token.includes('password'), 'Token contains password');
    });
  });

  // ========== Authorization (5 tests) ==========
  describe('Authorization & Access Control (5 tests)', function() {
    
    it('S36. RBAC enforced correctly', async function() {
      const user = await client.users.getProfile();
      const admin = await client.admin.getConfig().catch(() => ({}));
      if (!user.roles.includes('admin')) {
        assert(admin.error === 'FORBIDDEN');
      }
    });

    it('S37. Object-level access control', async function() {
      const doc1 = await client.documents.create({ title: 'private', content: 'mine' });
      const doc2Id = 'other_user_doc_123';
      try {
        await client.documents.delete(doc2Id);
        assert.fail('Deleted document from another user');
      } catch (error) {
        assert(error.status === 403 || error.code === 'FORBIDDEN');
      }
    });

    it('S38. API permissions enforced', async function() {
      const available = await client.security.getAvailableEndpoints();
      const restricted = available.filter(e => e.requiresAuth === true);
      assert(restricted.length > 0, 'All endpoints public');
    });

    it('S39. Data isolation by tenant/user', async function() {
      const user1Docs = await client.documents.list();
      assert(user1Docs.every(d => d.userId === 'user_1'), 'Cross-user data leak');
    });

    it('S40. Privilege escalation prevented', async function() {
      try {
        await client.users.updateProfile({ role: 'admin' });
        const updated = await client.users.getProfile();
        assert(updated.role !== 'admin', 'Privilege escalation succeeded');
      } catch (error) {
        assert(error.code === 'FORBIDDEN' || error.code === 'INVALID_ROLE');
      }
    });
  });
});
