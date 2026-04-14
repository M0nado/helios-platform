/**
 * HELIOS v4.0 Authentication & Authorization Module - Public API
 * Complete authentication and authorization solution with OAuth2, SAML, JWT, and RBAC
 * 
 * @module feat-auth
 * @version 1.0.0
 * 
 * @description
 * This module provides comprehensive authentication and authorization capabilities:
 * - OAuth2 Provider (Authorization Code, Client Credentials, Refresh Token flows)
 * - SAML 2.0 Provider (SP-initiated and IdP-initiated authentication)
 * - JWT Manager (Token creation, validation, and refresh with algorithm support)
 * - Role Manager (Role definition, assignment, and hierarchical inheritance)
 * - Permission Matrix (Fine-grained resource-level access control)
 * 
 * Performance Characteristics:
 * - Token validation: <50ms (cached for 5min)
 * - JWT operations: <10ms
 * - Role/permission checks: <1ms (cached)
 * - SAML assertion parsing: <100ms
 * 
 * @example
 * const { OAuth2Provider, JWTManager, RoleManager } = require('@helios/feat-auth');
 * 
 * // Initialize JWT manager
 * const jwt = new JWTManager({
 *   secret: 'your-secret-key',
 *   expiresIn: 3600,
 *   issuer: 'helios-api'
 * });
 * 
 * // Create token
 * const token = jwt.createToken({
 *   sub: 'user123',
 *   roles: ['editor'],
 *   email: 'user@example.com'
 * });
 * 
 * // Verify token
 * const payload = jwt.verifyToken(token);
 * 
 * @requires feat-auth/implementation
 */

const {
  OAuth2Provider,
  SAMLProvider,
  JWTManager,
  RoleManager,
  PermissionMatrix
} = require('./implementation');

/**
 * AuthenticationFactory - Create preconfigured authentication providers
 * @class
 */
class AuthenticationFactory {
  /**
   * Create OAuth2 provider with common configuration
   * @static
   * @param {Object} config - Custom OAuth2 configuration
   * @returns {OAuth2Provider} Configured OAuth2 provider instance
   * @example
   * const oauth = AuthenticationFactory.createOAuth2Provider({
   *   clientId: 'your-client-id',
   *   clientSecret: 'your-secret',
   *   redirectUri: 'https://app.example.com/callback'
   * });
   */
  static createOAuth2Provider(config) {
    return new OAuth2Provider(config);
  }

  /**
   * Create SAML provider with common configuration
   * @static
   * @param {Object} config - Custom SAML configuration
   * @returns {SAMLProvider} Configured SAML provider instance
   * @example
   * const saml = AuthenticationFactory.createSAMLProvider({
   *   entityId: 'urn:example:sp',
   *   assertionConsumerServiceUrl: 'https://app.example.com/saml/acs',
   *   identityProviderUrl: 'https://idp.example.com/sso',
   *   certificate: fs.readFileSync('cert.pem', 'utf-8')
   * });
   */
  static createSAMLProvider(config) {
    return new SAMLProvider(config);
  }

  /**
   * Create JWT manager with common configuration
   * @static
   * @param {Object} config - Custom JWT configuration
   * @returns {JWTManager} Configured JWT manager instance
   * @example
   * const jwt = AuthenticationFactory.createJWTManager({
   *   secret: process.env.JWT_SECRET,
   *   expiresIn: 3600
   * });
   */
  static createJWTManager(config) {
    return new JWTManager(config);
  }

  /**
   * Create RBAC system with roles and permissions
   * @static
   * @param {Object} rolesConfig - Roles and permissions configuration
   * @returns {Object} Object with roleManager and permissionMatrix
   * @example
   * const { roleManager, permissionMatrix } = 
   *   AuthenticationFactory.createRBACSystem({
   *     roles: {
   *       admin: { description: 'Administrator' },
   *       editor: { description: 'Content Editor' },
   *       viewer: { description: 'Viewer' }
   *     }
   *   });
   */
  static createRBACSystem(rolesConfig = {}) {
    const roleManager = new RoleManager(rolesConfig);
    const permissionMatrix = new PermissionMatrix();
    return { roleManager, permissionMatrix };
  }
}

/**
 * Create an integrated authentication middleware
 * @param {Object} config - Authentication configuration
 * @param {JWTManager} config.jwtManager - JWT manager instance
 * @param {RoleManager} config.roleManager - Role manager instance
 * @param {PermissionMatrix} config.permissionMatrix - Permission matrix instance
 * @returns {Function} Express/Connect middleware function
 * @example
 * const authMiddleware = createAuthMiddleware({
 *   jwtManager: jwt,
 *   roleManager: roleManager,
 *   permissionMatrix: permMatrix
 * });
 * 
 * app.use(authMiddleware);
 */
function createAuthMiddleware(config) {
  const { jwtManager, roleManager, permissionMatrix } = config;
  
  return (req, res, next) => {
    const authHeader = req.headers.authorization;
    if (!authHeader) {
      return res.status(401).json({ error: 'No authorization header' });
    }

    try {
      const token = authHeader.replace('Bearer ', '');
      const payload = jwtManager.verifyToken(token);
      
      req.user = {
        id: payload.sub,
        roles: payload.roles || [],
        permissions: new Set()
      };

      if (payload.roles) {
        for (const role of payload.roles) {
          const perms = roleManager.getUserPermissions(payload.sub);
          perms.forEach(p => req.user.permissions.add(p));
        }
      }

      req.user.hasPermission = (permission) => {
        if (!req.user.permissions.size) return false;
        return req.user.permissions.has(permission);
      };

      req.user.canAccess = (resourceId, action) => {
        return permissionMatrix.hasResourceAccess(req.user.id, resourceId, action);
      };

      next();
    } catch (error) {
      res.status(401).json({ error: 'Invalid token' });
    }
  };
}

/**
 * Require specific permission middleware
 * @param {string} permission - Required permission
 * @returns {Function} Express middleware function
 * @example
 * app.delete('/articles/:id', requirePermission('delete:articles'), handler);
 */
function requirePermission(permission) {
  return (req, res, next) => {
    if (!req.user || !req.user.hasPermission(permission)) {
      return res.status(403).json({ error: 'Insufficient permissions' });
    }
    next();
  };
}

/**
 * Require specific role middleware
 * @param {string|string[]} roles - Required role(s)
 * @returns {Function} Express middleware function
 * @example
 * app.post('/admin/users', requireRole('admin'), handler);
 */
function requireRole(roles) {
  const roleArray = Array.isArray(roles) ? roles : [roles];
  
  return (req, res, next) => {
    if (!req.user || !roleArray.some(role => req.user.roles.includes(role))) {
      return res.status(403).json({ error: 'Insufficient role' });
    }
    next();
  };
}

/**
 * Export public API
 */
module.exports = {
  // Core classes
  OAuth2Provider,
  SAMLProvider,
  JWTManager,
  RoleManager,
  PermissionMatrix,
  
  // Factory methods
  AuthenticationFactory,
  
  // Middleware utilities
  createAuthMiddleware,
  requirePermission,
  requireRole,
  
  // Version info
  version: '1.0.0',
  name: '@helios/feat-auth'
};
