# HELIOS v4.0 feat-auth Module

## Overview

`feat-auth` provides enterprise-grade authentication and authorization for HELIOS v4.0 applications. It implements OAuth2, SAML 2.0, JWT, API key authentication, and fine-grained role-based access control (RBAC).

**Module Size:** ~75 KB | **Test Coverage:** 48 tests

### Key Features

- **OAuth2 Provider** - Authorization Code, Client Credentials, Refresh Token flows
- **SAML 2.0** - SP-initiated and IdP-initiated authentication
- **JWT Manager** - Create, verify, refresh, revoke tokens (HS256, RS256, ES256)
- **Role Manager** - Role definition, assignment, and hierarchical inheritance
- **Permission Matrix** - Resource-level access control with ACLs
- **Express Middleware** - Ready-to-use authentication middleware

### Performance Characteristics

| Operation | Latency | Notes |
|-----------|---------|-------|
| JWT verification | <10ms | Cached signatures |
| Token validation | <50ms | 5-minute cache TTL |
| Role/permission check | <1ms | Cached results |
| SAML assertion parsing | <100ms | Full XML validation |
| OAuth2 token exchange | 200-500ms | Network dependent |

## Installation

```bash
npm install @helios/feat-auth
```

## Quick Start

### Basic JWT Authentication

```javascript
const { JWTManager } = require('@helios/feat-auth');

const jwt = new JWTManager({
  secret: process.env.JWT_SECRET,
  expiresIn: 3600,
  issuer: 'helios-api'
});

// Create token
const token = jwt.createToken({
  sub: 'user123',
  email: 'user@example.com',
  roles: ['editor']
});

// Verify token
const payload = jwt.verifyToken(token);
console.log(payload.sub); // 'user123'

// Refresh token
const newToken = jwt.refreshToken(token);

// Revoke token
jwt.revokeToken(token);
```

### Role-Based Access Control

```javascript
const { AuthenticationFactory } = require('@helios/feat-auth');

const { roleManager, permissionMatrix } = 
  AuthenticationFactory.createRBACSystem();

// Define roles
permissionMatrix.grantPermission('editor', 'create:articles');
permissionMatrix.grantPermission('editor', 'edit:articles');
permissionMatrix.grantPermission('viewer', 'read:articles');

// Assign roles
roleManager.assignRole('user123', 'editor');

// Check permissions
if (permissionMatrix.hasPermission('editor', 'create:articles')) {
  // Allow action
}

// Resource-level access
permissionMatrix.grantResourceAccess('user123', 'article-456', ['read', 'write']);
if (permissionMatrix.hasResourceAccess('user123', 'article-456', 'write')) {
  // User can edit article-456
}
```

### Express Integration

```javascript
const express = require('express');
const { createAuthMiddleware, requirePermission, requireRole } = require('@helios/feat-auth');

const app = express();

// Setup authentication
const authMiddleware = createAuthMiddleware({
  jwtManager: jwt,
  roleManager: roleManager,
  permissionMatrix: permissionMatrix
});

// Protect routes
app.use(authMiddleware);

// Require specific permission
app.delete('/articles/:id', requirePermission('delete:articles'), (req, res) => {
  res.json({ deleted: true });
});

// Require specific role
app.post('/admin/users', requireRole('admin'), (req, res) => {
  res.json({ created: true });
});

// Multiple role requirement
app.get('/moderation', requireRole(['admin', 'moderator']), (req, res) => {
  res.json({ queue: [] });
});
```

### OAuth2 Authentication

```javascript
const { OAuth2Provider } = require('@helios/feat-auth');

const oauth2 = new OAuth2Provider({
  clientId: 'your-client-id',
  clientSecret: 'your-client-secret',
  redirectUri: 'https://app.example.com/callback',
  tokenEndpoint: 'https://auth.provider.com/token',
  authorizeEndpoint: 'https://auth.provider.com/authorize',
  tokenExpiry: 3600
});

// Generate authorization URL
const state = crypto.randomBytes(16).toString('hex');
const authUrl = oauth2.getAuthorizationUrl(state, ['openid', 'profile', 'email']);

// Exchange code for token (in callback handler)
const tokens = await oauth2.exchangeCodeForToken(authorizationCode);

// Validate and refresh
if (await oauth2.validateToken(tokens.accessToken)) {
  // Token is valid
}

const refreshed = await oauth2.refreshAccessToken(tokens.refreshToken);
```

### SAML Authentication

```javascript
const { SAMLProvider } = require('@helios/feat-auth');
const fs = require('fs');

const saml = new SAMLProvider({
  entityId: 'urn:myapp:sp',
  assertionConsumerServiceUrl: 'https://app.example.com/saml/acs',
  identityProviderUrl: 'https://idp.example.com/sso',
  certificate: fs.readFileSync('idp-certificate.pem', 'utf-8')
});

// Generate AuthnRequest
const samlRequest = saml.generateAuthnRequest(relayState);

// Validate response assertion
const assertion = await saml.validateAssertion(samlResponse, relayState);
console.log(assertion.subject); // User email/ID
console.log(assertion.attributes); // SAML attributes
```

## API Reference

### OAuth2Provider

#### Constructor
```javascript
new OAuth2Provider({
  clientId: string,           // OAuth2 client ID
  clientSecret: string,       // OAuth2 client secret
  redirectUri: string,        // Callback URI
  tokenEndpoint: string,      // Token endpoint URL
  authorizeEndpoint: string,  // Authorization endpoint URL
  tokenExpiry: number         // Token TTL in seconds (default: 3600)
})
```

#### Methods

- **getAuthorizationUrl(state, scopes)** → string  
  Generate authorization URL for user redirect

- **exchangeCodeForToken(code)** → Promise<{accessToken, refreshToken, expiresIn, tokenType}>  
  Exchange authorization code for tokens

- **refreshAccessToken(refreshToken)** → Promise<{accessToken, expiresIn}>  
  Refresh expired access token

- **validateToken(token)** → Promise<boolean>  
  Validate OAuth2 access token

### SAMLProvider

#### Constructor
```javascript
new SAMLProvider({
  entityId: string,                        // Service Provider entity ID
  assertionConsumerServiceUrl: string,    // ACS URL
  identityProviderUrl: string,            // IdP SSO URL
  certificate: string,                   // X.509 certificate
  privateKey: string                    // Optional private key
})
```

#### Methods

- **generateAuthnRequest(relayState)** → string  
  Generate Base64-encoded SAML AuthnRequest

- **validateAssertion(samlResponse, relayState)** → Promise<{subject, attributes, sessionIndex}>  
  Parse and validate SAML response

- **getCachedAssertion(subject)** → Object|null  
  Retrieve cached assertion

### JWTManager

#### Constructor
```javascript
new JWTManager({
  secret: string,           // HMAC secret
  algorithm: string,        // HS256|RS256|ES256 (default: HS256)
  expiresIn: number,       // TTL in seconds (default: 3600)
  issuer: string,          // JWT iss claim
  audience: string         // JWT aud claim
})
```

#### Methods

- **createToken(payload)** → string  
  Create signed JWT token

- **verifyToken(token, options)** → Object  
  Verify and decode JWT token

- **refreshToken(token)** → string  
  Create new token from existing token

- **revokeToken(token)** → boolean  
  Add token to blacklist

### RoleManager

#### Constructor
```javascript
new RoleManager({
  roles: Object,          // Predefined roles
  hierarchy: Object       // Role inheritance
})
```

#### Methods

- **defineRole(name, description, permissions)** → void  
  Create new role

- **assignRole(userId, roleName)** → void  
  Assign role to user

- **removeRole(userId, roleName)** → boolean  
  Remove role from user

- **getUserRoles(userId)** → string[]  
  Get user's roles

- **hasRole(userId, roleName)** → boolean  
  Check user has role (with inheritance)

- **setHierarchy(roleName, parentRoles)** → void  
  Define role inheritance

- **getUserPermissions(userId)** → Set<string>  
  Get all user permissions from roles

### PermissionMatrix

#### Constructor
```javascript
new PermissionMatrix({
  permissions: Map<string, Set<string>>  // Initial permissions
})
```

#### Methods

- **grantPermission(roleName, permission)** → void  
  Grant permission to role

- **revokePermission(roleName, permission)** → boolean  
  Revoke permission from role

- **hasPermission(roleName, permission)** → boolean  
  Check if role has permission (supports wildcards)

- **grantResourceAccess(userId, resourceId, actions)** → void  
  Grant resource-level access

- **hasResourceAccess(userId, resourceId, action)** → boolean  
  Check resource access

- **getRolePermissions(roleName)** → string[]  
  Get all permissions for role

### Middleware & Utilities

#### createAuthMiddleware(config)
Express middleware for JWT authentication
```javascript
app.use(createAuthMiddleware({
  jwtManager: JWTManager,
  roleManager: RoleManager,
  permissionMatrix: PermissionMatrix
}));
// req.user = { id, roles, permissions, hasPermission(), canAccess() }
```

#### requirePermission(permission)
Middleware to require specific permission
```javascript
app.delete('/articles/:id', 
  requirePermission('delete:articles'),
  handler
);
```

#### requireRole(roles)
Middleware to require specific role(s)
```javascript
app.post('/admin/config',
  requireRole(['admin', 'superuser']),
  handler
);
```

## Error Handling

All classes throw descriptive errors with context:

```javascript
try {
  jwt.verifyToken(malformedToken);
} catch (error) {
  console.error(error.message);
  // "JWTManager: Token verification failed - Signature verification failed"
}

try {
  roleManager.assignRole('user123', 'nonexistent');
} catch (error) {
  console.error(error.message);
  // "RoleManager: Role 'nonexistent' does not exist"
}
```

## Security Best Practices

1. **Secrets Management**
   ```javascript
   const jwt = new JWTManager({
     secret: process.env.JWT_SECRET  // Never hardcode
   });
   ```

2. **Token Expiry**
   ```javascript
   // Short-lived access tokens, long-lived refresh tokens
   const accessToken = jwt.createToken(payload); // 1 hour
   const refreshToken = jwt.createToken(payload); // 7 days
   ```

3. **HTTPS Only**
   ```javascript
   // Always use HTTPS in production
   // Bearer token must only be sent over secure connections
   ```

4. **Token Storage**
   ```javascript
   // Client: Store in secure httpOnly cookies, not localStorage
   // Server: Use token blacklist for revocation
   ```

5. **Role Hierarchy**
   ```javascript
   // Prevent privilege escalation
   roleManager.setHierarchy('user', []);        // User cannot inherit others
   roleManager.setHierarchy('admin', ['user']); // Admin inherits user
   ```

## Testing

Run comprehensive test suite:

```bash
npm test
```

Test categories:
- OAuth2 Provider (12 tests)
- SAML Provider (10 tests)
- JWT Manager (12 tests)
- Role Manager (9 tests)
- Permission Matrix (5 tests)

## Examples

Run included examples:

```bash
node examples.js
```

Covers:
1. OAuth2 login flow
2. SAML authentication
3. JWT token lifecycle
4. Role-based access control
5. Express middleware integration
6. Multi-provider authentication

## Troubleshooting

### "Token has expired"
```javascript
// Re-authenticate or refresh token
const newToken = jwt.refreshToken(expiredToken);
```

### "Signature verification failed"
- JWT secret mismatch between signer and verifier
- Check `process.env.JWT_SECRET` is consistent

### "Token has been revoked"
```javascript
// Token was blacklisted with revokeToken()
// User must re-authenticate
```

### "Insufficient permissions"
```javascript
// Grant permission explicitly
permissionMatrix.grantPermission('editor', 'delete:articles');
// Or check permission before action
if (!permissionMatrix.hasPermission(role, 'delete:articles')) {
  throw new Error('Permission denied');
}
```

## License

HELIOS v4.0 - Proprietary

## Support

For issues and feature requests: support@helios.example.com
