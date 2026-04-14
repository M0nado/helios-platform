/**
 * HELIOS v4.0 Authentication & Authorization - Usage Examples
 * Real-world scenarios demonstrating all authentication mechanisms
 * @module feat-auth/examples
 */

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
} = require('./index');

/**
 * Example 1: OAuth2 Login Flow
 * Demonstrates complete OAuth2 Authorization Code flow
 */
async function exampleOAuth2Login() {
  console.log('\n=== Example 1: OAuth2 Login Flow ===\n');

  const oauth2 = new OAuth2Provider({
    clientId: 'app-client-id-12345',
    clientSecret: 'app-client-secret-abcdef',
    redirectUri: 'https://app.example.com/oauth/callback',
    tokenEndpoint: 'https://auth.example.com/token',
    authorizeEndpoint: 'https://auth.example.com/authorize'
  });

  // Step 1: Generate authorization URL for user redirect
  const state = Math.random().toString(36).substr(2, 9);
  const authUrl = oauth2.getAuthorizationUrl(state, ['openid', 'profile', 'email']);
  console.log('1. Redirect user to:', authUrl);

  // Step 2: User authenticates and grants access, receives authorization code
  const authorizationCode = 'auth-code-from-oauth-provider';
  console.log('2. Received authorization code:', authorizationCode);

  try {
    // Step 3: Exchange code for tokens (simulated - would call actual OAuth provider)
    console.log('3. Exchanging code for tokens...');
    const tokens = {
      accessToken: 'access-token-xyz',
      refreshToken: 'refresh-token-abc',
      expiresIn: 3600,
      tokenType: 'Bearer'
    };
    console.log('   Access token obtained:', tokens.accessToken.substring(0, 20) + '...');

    // Step 4: Validate token
    console.log('4. Validating token...');
    const isValid = await oauth2.validateToken(tokens.accessToken);
    console.log('   Token valid:', isValid);

    // Step 5: Refresh when near expiry
    console.log('5. Refreshing token...');
    const newTokens = {
      accessToken: 'new-access-token-123',
      expiresIn: 3600
    };
    console.log('   New token obtained:', newTokens.accessToken);
  } catch (error) {
    console.error('OAuth2 error:', error.message);
  }
}

/**
 * Example 2: SAML Authentication
 * Demonstrates SAML 2.0 SP-initiated authentication flow
 */
async function exampleSAMLAuth() {
  console.log('\n=== Example 2: SAML Authentication ===\n');

  const saml = new SAMLProvider({
    entityId: 'urn:example:sp:helios',
    assertionConsumerServiceUrl: 'https://app.example.com/saml/acs',
    identityProviderUrl: 'https://idp.example.com/sso',
    certificate: '-----BEGIN CERTIFICATE-----\nMIICmTCCAYGgAwIBAgIBADANBgkqhkiG9w0BAQQFADBAMQswCQYDVQQGEwJ1czEW...'
  });

  // Step 1: Generate SAML AuthnRequest
  const samlRequest = saml.generateAuthnRequest('relay-state-123');
  console.log('1. Generated SAML AuthnRequest:');
  console.log('   ' + samlRequest.substring(0, 60) + '...');

  // Step 2: User submits form to IdP, authenticates, and receives SAML response
  const samlResponse = 'PHNhbWxwOlJlc3BvbnNl...'; // Base64 encoded SAML response
  console.log('2. Received SAML response from IdP');

  try {
    // Step 3: Parse and validate assertion
    console.log('3. Validating SAML assertion...');
    const assertion = await saml.validateAssertion(samlResponse, 'relay-state-123');
    console.log('   Subject:', assertion.subject);
    console.log('   Attributes:', assertion.attributes);
    console.log('   Session Index:', assertion.sessionIndex);

    // Step 4: Check cached assertion
    const cached = saml.getCachedAssertion(assertion.subject);
    console.log('4. Assertion cached:', !!cached);
  } catch (error) {
    console.error('SAML error:', error.message);
  }
}

/**
 * Example 3: JWT Token Lifecycle
 * Demonstrates JWT creation, verification, refresh, and revocation
 */
function exampleJWTTokenLifecycle() {
  console.log('\n=== Example 3: JWT Token Lifecycle ===\n');

  const jwtManager = new JWTManager({
    secret: 'super-secret-key-change-in-production',
    algorithm: 'HS256',
    expiresIn: 3600,
    issuer: 'helios-api',
    audience: 'helios-app'
  });

  // Step 1: Create token
  console.log('1. Creating JWT token...');
  const token = jwtManager.createToken({
    sub: 'user-12345',
    email: 'user@example.com',
    roles: ['editor', 'moderator'],
    metadata: { department: 'Engineering' }
  });
  console.log('   Token created:', token.substring(0, 30) + '...');

  // Step 2: Verify token
  console.log('2. Verifying token...');
  try {
    const payload = jwtManager.verifyToken(token);
    console.log('   User:', payload.sub);
    console.log('   Email:', payload.email);
    console.log('   Roles:', payload.roles);
    console.log('   Issued at:', new Date(payload.iat * 1000).toISOString());
    console.log('   Expires at:', new Date(payload.exp * 1000).toISOString());
  } catch (error) {
    console.error('   Verification failed:', error.message);
  }

  // Step 3: Refresh token
  console.log('3. Refreshing token...');
  try {
    const newToken = jwtManager.refreshToken(token);
    console.log('   New token created:', newToken.substring(0, 30) + '...');
  } catch (error) {
    console.error('   Refresh failed:', error.message);
  }

  // Step 4: Revoke token
  console.log('4. Revoking token...');
  jwtManager.revokeToken(token);
  console.log('   Token revoked');

  // Step 5: Verify revoked token
  console.log('5. Verifying revoked token...');
  try {
    jwtManager.verifyToken(token);
  } catch (error) {
    console.log('   Error (expected):', error.message);
  }
}

/**
 * Example 4: Role-Based Access Control
 * Demonstrates role assignment, hierarchy, and permission management
 */
function exampleRBACSystem() {
  console.log('\n=== Example 4: Role-Based Access Control ===\n');

  const { roleManager, permissionMatrix } = AuthenticationFactory.createRBACSystem({
    roles: {
      admin: { description: 'System Administrator' },
      editor: { description: 'Content Editor' },
      viewer: { description: 'Read-only Viewer' },
      moderator: { description: 'Content Moderator' }
    }
  });

  // Step 1: Define roles with permissions
  console.log('1. Defining roles and permissions...');
  permissionMatrix.grantPermission('admin', 'delete:users');
  permissionMatrix.grantPermission('admin', 'create:users');
  permissionMatrix.grantPermission('admin', '*:*');
  
  permissionMatrix.grantPermission('editor', 'create:articles');
  permissionMatrix.grantPermission('editor', 'edit:articles');
  permissionMatrix.grantPermission('editor', 'delete:own-articles');
  
  permissionMatrix.grantPermission('viewer', 'read:articles');
  
  permissionMatrix.grantPermission('moderator', 'edit:articles');
  permissionMatrix.grantPermission('moderator', 'delete:comments');
  console.log('   Permissions assigned');

  // Step 2: Set role hierarchy
  console.log('2. Setting role hierarchy...');
  roleManager.setHierarchy('moderator', ['editor']);
  console.log('   Moderator inherits from Editor');

  // Step 3: Assign roles to users
  console.log('3. Assigning roles to users...');
  roleManager.assignRole('user-001', 'admin');
  roleManager.assignRole('user-002', 'editor');
  roleManager.assignRole('user-003', 'moderator');
  roleManager.assignRole('user-004', 'viewer');
  console.log('   Roles assigned');

  // Step 4: Check user permissions
  console.log('4. Checking user permissions...');
  
  console.log('\n   Admin (user-001):');
  console.log('   - Can delete users:', permissionMatrix.hasPermission('admin', 'delete:users'));
  console.log('   - Can write anything:', permissionMatrix.hasPermission('admin', 'write:anything'));
  
  console.log('\n   Editor (user-002):');
  console.log('   - Can create articles:', permissionMatrix.hasPermission('editor', 'create:articles'));
  console.log('   - Can delete users:', permissionMatrix.hasPermission('editor', 'delete:users'));
  
  console.log('\n   Moderator (user-003):');
  console.log('   - Can edit articles:', permissionMatrix.hasPermission('moderator', 'edit:articles'));
  console.log('   - Can delete comments:', permissionMatrix.hasPermission('moderator', 'delete:comments'));
  console.log('   - Inherits editor permissions:', permissionMatrix.hasPermission('moderator', 'create:articles'));
  
  console.log('\n   Viewer (user-004):');
  console.log('   - Can read articles:', permissionMatrix.hasPermission('viewer', 'read:articles'));
  console.log('   - Can create articles:', permissionMatrix.hasPermission('viewer', 'create:articles'));

  // Step 5: Resource-level access control
  console.log('\n5. Resource-level access control...');
  permissionMatrix.grantResourceAccess('user-002', 'article-123', ['read', 'write', 'delete']);
  permissionMatrix.grantResourceAccess('user-004', 'article-123', ['read']);
  
  console.log('   Editor on article-123: read=', 
    permissionMatrix.hasResourceAccess('user-002', 'article-123', 'read'),
    'write=',
    permissionMatrix.hasResourceAccess('user-002', 'article-123', 'write'));
  
  console.log('   Viewer on article-123: read=',
    permissionMatrix.hasResourceAccess('user-004', 'article-123', 'read'),
    'write=',
    permissionMatrix.hasResourceAccess('user-004', 'article-123', 'write'));
}

/**
 * Example 5: Express Middleware Integration
 * Demonstrates using authentication in Express applications
 */
function exampleExpressMiddleware() {
  console.log('\n=== Example 5: Express Middleware Integration ===\n');

  const jwtManager = new JWTManager({
    secret: 'app-secret-key',
    expiresIn: 3600,
    issuer: 'helios-api'
  });

  const { roleManager, permissionMatrix } = AuthenticationFactory.createRBACSystem();
  
  // Setup roles and permissions
  permissionMatrix.grantPermission('admin', 'delete:articles');
  permissionMatrix.grantPermission('editor', 'create:articles');
  permissionMatrix.grantPermission('editor', 'edit:articles');
  
  // Create middleware
  const authMiddleware = createAuthMiddleware({
    jwtManager,
    roleManager,
    permissionMatrix
  });

  console.log('1. Authentication middleware created');

  // Simulate request with token
  console.log('2. Creating test token...');
  const token = jwtManager.createToken({
    sub: 'user-123',
    roles: ['editor'],
    email: 'user@example.com'
  });

  // Simulate middleware execution
  console.log('3. Simulating middleware execution...');
  const mockReq = {
    headers: { authorization: `Bearer ${token}` }
  };
  const mockRes = {
    status: (code) => ({
      json: (data) => console.log(`   Response [${code}]:`, data)
    })
  };
  let middlewareExecuted = false;
  const mockNext = () => {
    middlewareExecuted = true;
    console.log('   User authenticated:', mockReq.user.id);
    console.log('   Roles:', mockReq.user.roles);
  };

  authMiddleware(mockReq, mockRes, mockNext);
  
  if (middlewareExecuted && mockReq.user) {
    console.log('4. Testing permission check...');
    console.log('   Has create:articles:', mockReq.user.hasPermission('create:articles'));
    console.log('   Has delete:articles:', mockReq.user.hasPermission('delete:articles'));
  }
}

/**
 * Example 6: Multi-Provider Authentication
 * Demonstrates using multiple authentication providers in one system
 */
async function exampleMultiProviderAuth() {
  console.log('\n=== Example 6: Multi-Provider Authentication ===\n');

  console.log('1. Initializing multiple auth providers...');
  
  const oauth2 = AuthenticationFactory.createOAuth2Provider({
    clientId: 'oauth-client',
    clientSecret: 'oauth-secret',
    redirectUri: 'https://app.example.com/oauth',
    tokenEndpoint: 'https://oauth.example.com/token',
    authorizeEndpoint: 'https://oauth.example.com/authorize'
  });

  const jwt = AuthenticationFactory.createJWTManager({
    secret: 'jwt-secret',
    expiresIn: 3600
  });

  const saml = AuthenticationFactory.createSAMLProvider({
    entityId: 'urn:example:sp',
    assertionConsumerServiceUrl: 'https://app.example.com/saml',
    identityProviderUrl: 'https://saml.example.com/sso',
    certificate: '-----BEGIN CERTIFICATE-----\n...'
  });

  console.log('2. Auth providers ready:');
  console.log('   ✓ OAuth2 Provider');
  console.log('   ✓ JWT Manager');
  console.log('   ✓ SAML Provider');

  console.log('3. Authentication flow decision:');
  console.log('   If enterprise SSO → Use SAML');
  console.log('   If social login → Use OAuth2');
  console.log('   If API access → Use JWT');

  // Create tokens for different scenarios
  const apiToken = jwt.createToken({ sub: 'api-user', type: 'service' });
  console.log('4. Generated API token for service:', apiToken.substring(0, 30) + '...');
}

// Export example functions
module.exports = {
  exampleOAuth2Login,
  exampleSAMLAuth,
  exampleJWTTokenLifecycle,
  exampleRBACSystem,
  exampleExpressMiddleware,
  exampleMultiProviderAuth
};

// Run examples if executed directly
if (require.main === module) {
  (async () => {
    try {
      await exampleOAuth2Login();
      await exampleSAMLAuth();
      exampleJWTTokenLifecycle();
      exampleRBACSystem();
      exampleExpressMiddleware();
      await exampleMultiProviderAuth();
      console.log('\n=== All Examples Completed Successfully ===\n');
    } catch (error) {
      console.error('Error running examples:', error);
    }
  })();
}
