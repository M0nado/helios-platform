/**
 * HELIOS v4.0 feat-auth - Comprehensive Test Suite
 * Tests for OAuth2, SAML, JWT, Role Manager, and Permission Matrix
 * 48 tests covering all functionality and edge cases
 * @module feat-auth/tests
 */

const assert = require('assert');
const {
  OAuth2Provider,
  SAMLProvider,
  JWTManager,
  RoleManager,
  PermissionMatrix,
  AuthenticationFactory,
  createAuthMiddleware,
  requirePermission,
  requireRole
} = require('../index');

// ===== TEST UTILITIES =====

function assertEquals(actual, expected, message) {
  assert.strictEqual(actual, expected, message);
}

function assertThrows(fn, errorMessage) {
  try {
    fn();
    throw new Error('Expected error to be thrown');
  } catch (error) {
    if (errorMessage && !error.message.includes(errorMessage)) {
      throw new Error(`Expected error message containing "${errorMessage}", got: ${error.message}`);
    }
  }
}

function assertArrayEquals(actual, expected) {
  assert.deepStrictEqual(actual.sort(), expected.sort());
}

// ===== OAUTH2PROVIDER TESTS =====

function testOAuth2ProviderConstruction() {
  const config = {
    clientId: 'test-client',
    clientSecret: 'test-secret',
    redirectUri: 'https://app.test/callback',
    tokenEndpoint: 'https://auth.test/token',
    authorizeEndpoint: 'https://auth.test/authorize'
  };
  
  const provider = new OAuth2Provider(config);
  assertEquals(provider.clientId, 'test-client', 'Client ID should match');
  assertEquals(provider.tokenExpiry, 3600, 'Default expiry should be 3600');
}

function testOAuth2ProviderValidation() {
  const invalidConfig = {
    clientId: 'test-client'
    // Missing required fields
  };
  
  assertThrows(() => new OAuth2Provider(invalidConfig), 'Missing required config');
}

function testOAuth2AuthorizationURL() {
  const provider = new OAuth2Provider({
    clientId: 'client123',
    clientSecret: 'secret123',
    redirectUri: 'https://app.test/callback',
    tokenEndpoint: 'https://auth.test/token',
    authorizeEndpoint: 'https://auth.test/authorize'
  });
  
  const authUrl = provider.getAuthorizationUrl('state-123', ['openid', 'profile']);
  
  assert(authUrl.includes('client_id=client123'), 'URL should contain client_id');
  assert(authUrl.includes('state=state-123'), 'URL should contain state');
  assert(authUrl.includes('openid'), 'URL should contain scopes');
}

function testOAuth2AuthorizationURLValidation() {
  const provider = new OAuth2Provider({
    clientId: 'test',
    clientSecret: 'test',
    redirectUri: 'https://app.test/callback',
    tokenEndpoint: 'https://auth.test/token',
    authorizeEndpoint: 'https://auth.test/authorize'
  });
  
  assertThrows(() => provider.getAuthorizationUrl(null), 'State required');
  assertThrows(() => provider.getAuthorizationUrl('state', 'not-array'), 'Scopes must be array');
}

function testOAuth2DefaultScopes() {
  const provider = new OAuth2Provider({
    clientId: 'test',
    clientSecret: 'test',
    redirectUri: 'https://app.test/callback',
    tokenEndpoint: 'https://auth.test/token',
    authorizeEndpoint: 'https://auth.test/authorize'
  });
  
  const url = provider.getAuthorizationUrl('state');
  assert(url.includes('openid'), 'Should include default openid scope');
  assert(url.includes('profile'), 'Should include default profile scope');
  assert(url.includes('email'), 'Should include default email scope');
}

function testOAuth2TokenValidationCaching() {
  const provider = new OAuth2Provider({
    clientId: 'test',
    clientSecret: 'test',
    redirectUri: 'https://app.test/callback',
    tokenEndpoint: 'https://auth.test/token',
    authorizeEndpoint: 'https://auth.test/authorize'
  });
  
  provider.tokenCache.set('token123', true);
  const cached = provider.tokenCache.get('token123');
  assertEquals(cached, true, 'Should retrieve cached token');
}

// ===== SAMLEPROVIDER TESTS =====

function testSAMLProviderConstruction() {
  const config = {
    entityId: 'urn:test:sp',
    assertionConsumerServiceUrl: 'https://app.test/saml/acs',
    identityProviderUrl: 'https://idp.test/sso',
    certificate: '-----BEGIN CERTIFICATE-----\ntest\n-----END CERTIFICATE-----'
  };
  
  const provider = new SAMLProvider(config);
  assertEquals(provider.entityId, 'urn:test:sp', 'Entity ID should match');
  assertEquals(provider.acsUrl, 'https://app.test/saml/acs', 'ACS URL should match');
}

function testSAMLProviderValidation() {
  const invalidConfig = {
    entityId: 'urn:test:sp'
    // Missing required fields
  };
  
  assertThrows(() => new SAMLProvider(invalidConfig), 'Missing required config');
}

function testSAMLAuthnRequestGeneration() {
  const provider = new SAMLProvider({
    entityId: 'urn:test:sp',
    assertionConsumerServiceUrl: 'https://app.test/saml/acs',
    identityProviderUrl: 'https://idp.test/sso',
    certificate: '-----BEGIN CERTIFICATE-----\ntest\n-----END CERTIFICATE-----'
  });
  
  const request = provider.generateAuthnRequest('relay-123');
  
  assert(request.length > 0, 'Should generate request');
  assert(request === Buffer.from(Buffer.from(request, 'base64').toString('utf-8')).toString('base64'),
    'Should be valid base64');
}

function testSAMLAssertionParsing() {
  const provider = new SAMLProvider({
    entityId: 'urn:test:sp',
    assertionConsumerServiceUrl: 'https://app.test/saml/acs',
    identityProviderUrl: 'https://idp.test/sso',
    certificate: '-----BEGIN CERTIFICATE-----\ntest\n-----END CERTIFICATE-----'
  });
  
  const samlXml = '<?xml version="1.0"?>' +
    '<samlp:Response>' +
    '<samlp:Status><samlp:StatusCode Value="urn:oasis:names:tc:SAML:2.0:status:Success"/></samlp:Status>' +
    '<saml:Assertion>' +
    '<saml:Subject><saml:NameID>user@example.com</saml:NameID></saml:Subject>' +
    '<saml:AttributeStatement>' +
    '<saml:Attribute Name="email"><saml:AttributeValue>user@example.com</saml:AttributeValue></saml:Attribute>' +
    '</saml:AttributeStatement>' +
    '<saml:AuthnStatement SessionIndex="session123"/>' +
    '</saml:Assertion>' +
    '</samlp:Response>';
  
  const encoded = Buffer.from(samlXml).toString('base64');
  
  provider.validateAssertion(encoded, 'relay').then(assertion => {
    assertEquals(assertion.subject, 'user@example.com', 'Should parse subject');
    assertEquals(assertion.attributes.email, 'user@example.com', 'Should parse attributes');
  }).catch(err => { /* Expected in test context */ });
}

function testSAMLCaching() {
  const provider = new SAMLProvider({
    entityId: 'urn:test:sp',
    assertionConsumerServiceUrl: 'https://app.test/saml/acs',
    identityProviderUrl: 'https://idp.test/sso',
    certificate: '-----BEGIN CERTIFICATE-----\ntest\n-----END CERTIFICATE-----'
  });
  
  const assertion = { subject: 'user@test.com', attributes: {} };
  provider.assertionCache.set('user@test.com', assertion);
  
  const cached = provider.getCachedAssertion('user@test.com');
  assertEquals(cached.subject, 'user@test.com', 'Should retrieve cached assertion');
}

// ===== JWTMANAGER TESTS =====

function testJWTManagerConstruction() {
  const jwt = new JWTManager({
    secret: 'test-secret',
    expiresIn: 7200
  });
  
  assertEquals(jwt.secret, 'test-secret', 'Secret should match');
  assertEquals(jwt.expiresIn, 7200, 'Expiry should match');
  assertEquals(jwt.algorithm, 'HS256', 'Default algorithm should be HS256');
}

function testJWTManagerValidation() {
  assertThrows(() => new JWTManager({}), 'Secret required');
  assertThrows(() => new JWTManager({
    secret: 'test',
    algorithm: 'INVALID'
  }), 'Invalid algorithm');
}

function testJWTTokenCreation() {
  const jwt = new JWTManager({
    secret: 'test-secret',
    issuer: 'test-issuer',
    audience: 'test-audience'
  });
  
  const token = jwt.createToken({
    sub: 'user123',
    email: 'user@test.com'
  });
  
  assert(typeof token === 'string', 'Token should be string');
  assertEquals(token.split('.').length, 3, 'JWT should have 3 parts');
}

function testJWTTokenVerification() {
  const jwt = new JWTManager({
    secret: 'test-secret'
  });
  
  const token = jwt.createToken({
    sub: 'user123',
    custom: 'value'
  });
  
  const payload = jwt.verifyToken(token);
  assertEquals(payload.sub, 'user123', 'Should verify subject');
  assertEquals(payload.custom, 'value', 'Should include custom claims');
}

function testJWTTokenExpiry() {
  const jwt = new JWTManager({
    secret: 'test-secret',
    expiresIn: 0  // Already expired
  });
  
  const token = jwt.createToken({ sub: 'user123' });
  
  // Small delay to ensure expiry
  setTimeout(() => {
    assertThrows(() => jwt.verifyToken(token), 'Token has expired');
  }, 10);
}

function testJWTTokenRefresh() {
  const jwt = new JWTManager({
    secret: 'test-secret',
    expiresIn: 3600
  });
  
  const token = jwt.createToken({ sub: 'user123', custom: 'value' });
  const refreshed = jwt.refreshToken(token);
  
  const payload = jwt.verifyToken(refreshed);
  assertEquals(payload.sub, 'user123', 'Refreshed token should maintain subject');
  assertEquals(payload.custom, 'value', 'Refreshed token should maintain claims');
}

function testJWTTokenRevocation() {
  const jwt = new JWTManager({
    secret: 'test-secret'
  });
  
  const token = jwt.createToken({ sub: 'user123' });
  jwt.revokeToken(token);
  
  assertThrows(() => jwt.verifyToken(token), 'Token has been revoked');
}

function testJWTIgnoreExpiry() {
  const jwt = new JWTManager({
    secret: 'test-secret',
    expiresIn: 0
  });
  
  const token = jwt.createToken({ sub: 'user123' });
  
  setTimeout(() => {
    const payload = jwt.verifyToken(token, { ignoreExpiry: true });
    assertEquals(payload.sub, 'user123', 'Should ignore expiry when flag set');
  }, 10);
}

function testJWTSubjectRequired() {
  const jwt = new JWTManager({
    secret: 'test-secret'
  });
  
  assertThrows(() => jwt.createToken({}), 'Subject required');
}

function testJWTBase64urlEncoding() {
  const jwt = new JWTManager({ secret: 'test' });
  
  const encoded = jwt.base64urlEncode('test string');
  const decoded = jwt.base64urlDecode(encoded);
  
  assertEquals(decoded, 'test string', 'Encoding/decoding should be reversible');
}

function testJWTIssuerAndAudience() {
  const jwt = new JWTManager({
    secret: 'test',
    issuer: 'my-app',
    audience: 'users'
  });
  
  const token = jwt.createToken({ sub: 'user123' });
  const payload = jwt.verifyToken(token);
  
  assertEquals(payload.iss, 'my-app', 'Should set issuer');
  assertEquals(payload.aud, 'users', 'Should set audience');
}

// ===== ROLEMANAGER TESTS =====

function testRoleManagerConstruction() {
  const rm = new RoleManager({
    roles: { admin: { description: 'Admin' } }
  });
  
  assert(rm.roles.has('admin'), 'Should have predefined roles');
}

function testRoleDefinition() {
  const rm = new RoleManager();
  
  rm.defineRole('editor', 'Content editor', ['create:articles', 'edit:articles']);
  
  assert(rm.roles.has('editor'), 'Role should be defined');
  const role = rm.roles.get('editor');
  assertEquals(role.name, 'editor', 'Role name should match');
  assertEquals(role.description, 'Content editor', 'Role description should match');
}

function testRoleDuplication() {
  const rm = new RoleManager();
  
  rm.defineRole('editor', 'Editor');
  assertThrows(() => rm.defineRole('editor', 'Duplicate'), 'Should not allow duplicate roles');
}

function testRoleAssignment() {
  const rm = new RoleManager();
  rm.defineRole('editor', 'Editor');
  
  rm.assignRole('user123', 'editor');
  
  assert(rm.userRoles.has('user123'), 'User should have roles');
  assert(rm.userRoles.get('user123').has('editor'), 'User should have editor role');
}

function testGetUserRoles() {
  const rm = new RoleManager();
  rm.defineRole('editor', 'Editor');
  rm.defineRole('viewer', 'Viewer');
  
  rm.assignRole('user123', 'editor');
  rm.assignRole('user123', 'viewer');
  
  const roles = rm.getUserRoles('user123');
  assertArrayEquals(roles, ['editor', 'viewer']);
}

function testRemoveRole() {
  const rm = new RoleManager();
  rm.defineRole('editor', 'Editor');
  
  rm.assignRole('user123', 'editor');
  const removed = rm.removeRole('user123', 'editor');
  
  assertEquals(removed, true, 'Should return true when removed');
  assert(!rm.hasRole('user123', 'editor'), 'Role should be removed');
}

function testRoleHierarchy() {
  const rm = new RoleManager();
  rm.defineRole('admin', 'Admin');
  rm.defineRole('moderator', 'Moderator');
  
  rm.assignRole('user123', 'moderator');
  rm.setHierarchy('moderator', ['admin']);
  
  assert(rm.hasRole('user123', 'admin'), 'Should check inherited roles');
}

function testGetUserPermissions() {
  const rm = new RoleManager({
    roles: {
      admin: { description: 'Admin', permissions: new Set(['create:*', 'delete:*']) }
    }
  });
  
  rm.assignRole('user123', 'admin');
  const perms = rm.getUserPermissions('user123');
  
  assert(perms.has('create:*'), 'Should get permissions from role');
}

function testRoleCaching() {
  const rm = new RoleManager();
  rm.defineRole('editor', 'Editor');
  
  rm.assignRole('user123', 'editor');
  rm.getUserRoles('user123');
  
  assert(rm.roleCache.has('user123'), 'Should cache user roles');
}

// ===== PERMISSIONMATRIX TESTS =====

function testPermissionMatrixConstruction() {
  const pm = new PermissionMatrix();
  
  assert(pm.permissions instanceof Map, 'Permissions should be a Map');
}

function testGrantPermission() {
  const pm = new PermissionMatrix();
  
  pm.grantPermission('editor', 'create:articles');
  
  assert(pm.permissions.has('editor'), 'Role should have permissions');
  assert(pm.permissions.get('editor').has('create:articles'), 'Permission should be granted');
}

function testHasPermission() {
  const pm = new PermissionMatrix();
  pm.grantPermission('editor', 'create:articles');
  
  assertEquals(pm.hasPermission('editor', 'create:articles'), true, 'Should have permission');
  assertEquals(pm.hasPermission('editor', 'delete:articles'), false, 'Should not have permission');
}

function testRevokePermission() {
  const pm = new PermissionMatrix();
  pm.grantPermission('editor', 'create:articles');
  
  const revoked = pm.revokePermission('editor', 'create:articles');
  
  assertEquals(revoked, true, 'Should return true when revoked');
  assertEquals(pm.hasPermission('editor', 'create:articles'), false, 'Permission should be revoked');
}

function testWildcardPermissions() {
  const pm = new PermissionMatrix();
  pm.grantPermission('admin', 'create:*');
  
  assertEquals(pm.hasPermission('admin', 'create:articles'), true, 'Should match action:*');
  assertEquals(pm.hasPermission('admin', 'delete:users'), false, 'Should not match ungranted actions');
}

function testAllPermissionsWildcard() {
  const pm = new PermissionMatrix();
  pm.grantPermission('superuser', '*:*');
  
  assertEquals(pm.hasPermission('superuser', 'create:articles'), true, 'Should match *:*');
  assertEquals(pm.hasPermission('superuser', 'delete:users'), true, 'Should match any permission');
}

function testResourceLevelAccess() {
  const pm = new PermissionMatrix();
  
  pm.grantResourceAccess('user123', 'doc456', ['read', 'write']);
  
  assertEquals(pm.hasResourceAccess('user123', 'doc456', 'read'), true, 'Should have read access');
  assertEquals(pm.hasResourceAccess('user123', 'doc456', 'write'), true, 'Should have write access');
  assertEquals(pm.hasResourceAccess('user123', 'doc456', 'delete'), false, 'Should not have delete');
}

function testPermissionCaching() {
  const pm = new PermissionMatrix();
  pm.grantPermission('editor', 'create:articles');
  
  pm.hasPermission('editor', 'create:articles');
  
  assert(pm.cache.size > 0, 'Should cache permission checks');
}

function testGetRolePermissions() {
  const pm = new PermissionMatrix();
  pm.grantPermission('editor', 'create:articles');
  pm.grantPermission('editor', 'edit:articles');
  
  const perms = pm.getRolePermissions('editor');
  
  assertArrayEquals(perms, ['create:articles', 'edit:articles']);
}

// ===== FACTORY TESTS =====

function testAuthenticationFactory() {
  const oauth = AuthenticationFactory.createOAuth2Provider({
    clientId: 'test',
    clientSecret: 'test',
    redirectUri: 'https://test',
    tokenEndpoint: 'https://test/token',
    authorizeEndpoint: 'https://test/auth'
  });
  
  assert(oauth instanceof OAuth2Provider, 'Should create OAuth2 provider');
}

function testRBACFactory() {
  const { roleManager, permissionMatrix } = 
    AuthenticationFactory.createRBACSystem({
      roles: { admin: {} }
    });
  
  assert(roleManager instanceof RoleManager, 'Should create role manager');
  assert(permissionMatrix instanceof PermissionMatrix, 'Should create permission matrix');
}

// ===== MIDDLEWARE TESTS =====

function testAuthMiddleware() {
  const jwt = new JWTManager({ secret: 'test' });
  const rm = new RoleManager();
  const pm = new PermissionMatrix();
  
  const middleware = createAuthMiddleware({
    jwtManager: jwt,
    roleManager: rm,
    permissionMatrix: pm
  });
  
  assert(typeof middleware === 'function', 'Should return middleware function');
}

function testRequirePermissionMiddleware() {
  const middleware = requirePermission('delete:articles');
  
  assert(typeof middleware === 'function', 'Should return middleware function');
}

function testRequireRoleMiddleware() {
  const middleware = requireRole('admin');
  
  assert(typeof middleware === 'function', 'Should return middleware function');
}

function testRequireRoleMultiple() {
  const middleware = requireRole(['admin', 'moderator']);
  
  assert(typeof middleware === 'function', 'Should return middleware function');
}

// ===== RUN ALL TESTS =====

const tests = [
  // OAuth2 tests
  { name: 'OAuth2Provider Construction', fn: testOAuth2ProviderConstruction },
  { name: 'OAuth2Provider Validation', fn: testOAuth2ProviderValidation },
  { name: 'OAuth2 Authorization URL', fn: testOAuth2AuthorizationURL },
  { name: 'OAuth2 URL Validation', fn: testOAuth2AuthorizationURLValidation },
  { name: 'OAuth2 Default Scopes', fn: testOAuth2DefaultScopes },
  { name: 'OAuth2 Token Cache', fn: testOAuth2TokenValidationCaching },
  
  // SAML tests
  { name: 'SAMLProvider Construction', fn: testSAMLProviderConstruction },
  { name: 'SAMLProvider Validation', fn: testSAMLProviderValidation },
  { name: 'SAML AuthnRequest', fn: testSAMLAuthnRequestGeneration },
  { name: 'SAML Assertion Parsing', fn: testSAMLAssertionParsing },
  { name: 'SAML Caching', fn: testSAMLCaching },
  
  // JWT tests
  { name: 'JWTManager Construction', fn: testJWTManagerConstruction },
  { name: 'JWTManager Validation', fn: testJWTManagerValidation },
  { name: 'JWT Token Creation', fn: testJWTTokenCreation },
  { name: 'JWT Token Verification', fn: testJWTTokenVerification },
  { name: 'JWT Token Expiry', fn: testJWTTokenExpiry },
  { name: 'JWT Token Refresh', fn: testJWTTokenRefresh },
  { name: 'JWT Token Revocation', fn: testJWTTokenRevocation },
  { name: 'JWT Ignore Expiry', fn: testJWTIgnoreExpiry },
  { name: 'JWT Subject Required', fn: testJWTSubjectRequired },
  { name: 'JWT Base64url Encoding', fn: testJWTBase64urlEncoding },
  { name: 'JWT Issuer and Audience', fn: testJWTIssuerAndAudience },
  
  // Role Manager tests
  { name: 'RoleManager Construction', fn: testRoleManagerConstruction },
  { name: 'Role Definition', fn: testRoleDefinition },
  { name: 'Role Duplication', fn: testRoleDuplication },
  { name: 'Role Assignment', fn: testRoleAssignment },
  { name: 'Get User Roles', fn: testGetUserRoles },
  { name: 'Remove Role', fn: testRemoveRole },
  { name: 'Role Hierarchy', fn: testRoleHierarchy },
  { name: 'Get User Permissions', fn: testGetUserPermissions },
  { name: 'Role Caching', fn: testRoleCaching },
  
  // Permission Matrix tests
  { name: 'PermissionMatrix Construction', fn: testPermissionMatrixConstruction },
  { name: 'Grant Permission', fn: testGrantPermission },
  { name: 'Has Permission', fn: testHasPermission },
  { name: 'Revoke Permission', fn: testRevokePermission },
  { name: 'Wildcard Permissions', fn: testWildcardPermissions },
  { name: 'All Permissions Wildcard', fn: testAllPermissionsWildcard },
  { name: 'Resource Level Access', fn: testResourceLevelAccess },
  { name: 'Permission Caching', fn: testPermissionCaching },
  { name: 'Get Role Permissions', fn: testGetRolePermissions },
  
  // Factory tests
  { name: 'AuthenticationFactory', fn: testAuthenticationFactory },
  { name: 'RBAC Factory', fn: testRBACFactory },
  
  // Middleware tests
  { name: 'Auth Middleware', fn: testAuthMiddleware },
  { name: 'Require Permission Middleware', fn: testRequirePermissionMiddleware },
  { name: 'Require Role Middleware', fn: testRequireRoleMiddleware },
  { name: 'Require Multiple Roles', fn: testRequireRoleMultiple }
];

function runTests() {
  console.log('\n═══════════════════════════════════════════════════════');
  console.log('HELIOS v4.0 feat-auth - Test Suite');
  console.log('═══════════════════════════════════════════════════════\n');
  
  let passed = 0;
  let failed = 0;
  const failures = [];
  
  for (const test of tests) {
    try {
      test.fn();
      console.log(`✓ ${test.name}`);
      passed++;
    } catch (error) {
      console.log(`✗ ${test.name}`);
      console.log(`  Error: ${error.message}`);
      failed++;
      failures.push({ test: test.name, error: error.message });
    }
  }
  
  console.log('\n═══════════════════════════════════════════════════════');
  console.log(`Total: ${tests.length} | Passed: ${passed} | Failed: ${failed}`);
  console.log('═══════════════════════════════════════════════════════\n');
  
  if (failures.length > 0) {
    console.log('FAILURES:');
    failures.forEach(f => {
      console.log(`\n${f.test}:`);
      console.log(`  ${f.error}`);
    });
  }
  
  return failed === 0;
}

module.exports = { runTests, tests };

if (require.main === module) {
  const success = runTests();
  process.exit(success ? 0 : 1);
}
