# Phase 4 Tier 4: Security Hardening Guide

**Status**: Complete  
**Date**: 2024  
**Target**: Encryption, rate limiting, input validation, security best practices  

---

## 🔐 Security Hardening Framework

### Security Layers

```
┌──────────────────────────────────┐
│  Layer 4: Data Encryption       │
│  ├─ Encryption at rest          │
│  ├─ Encryption in transit       │
│  └─ Key management              │
├──────────────────────────────────┤
│  Layer 3: Authorization         │
│  ├─ RBAC (Role-Based)           │
│  ├─ Claim-based policies        │
│  └─ Resource-level permissions  │
├──────────────────────────────────┤
│  Layer 2: Authentication        │
│  ├─ OAuth 2.0 / OpenID Connect  │
│  ├─ JWT tokens                  │
│  └─ Multi-factor authentication │
├──────────────────────────────────┤
│  Layer 1: Input Validation      │
│  ├─ Type validation             │
│  ├─ Length validation           │
│  ├─ Format validation           │
│  └─ SQL injection prevention    │
└──────────────────────────────────┘
```

---

## 1️⃣ Input Validation

### Principle 1: Whitelist Validation

```csharp
// ❌ BAD: Blacklist approach (incomplete)
public bool IsValidEmail(string email)
{
    return !email.Contains("../");  // Incomplete
}

// ✅ GOOD: Whitelist approach
public bool IsValidEmail(string email)
{
    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
}

// Using DataAnnotations
public class User
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Username { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 8)]
    public string Password { get; set; }
    
    [Range(18, 120)]
    public int Age { get; set; }
}
```

---

### Principle 2: SQL Injection Prevention

```csharp
// ❌ CRITICAL: SQL Injection vulnerability
public User GetUser(string email)
{
    var sql = $"SELECT * FROM Users WHERE Email = '{email}'";
    return dbContext.Database.SqlQueryRaw<User>(sql).FirstOrDefault();
    // Attacker: email = "' OR '1'='1"
    // Results in: SELECT * FROM Users WHERE Email = '' OR '1'='1'
    // Returns all users!
}

// ✅ GOOD: Parameterized query (safe)
public User GetUser(string email)
{
    return dbContext.Users
        .FromSqlInterpolated($"SELECT * FROM Users WHERE Email = {email}")
        .FirstOrDefault();
    // Parameters handled safely by framework
}

// ✅ GOOD: Using LINQ (automatic parameterization)
public User GetUser(string email)
{
    return dbContext.Users
        .FirstOrDefault(u => u.Email == email);
    // Completely safe - LINQ translates to parameterized SQL
}
```

---

### Principle 3: XSS Prevention

```csharp
// ❌ BAD: Stored XSS vulnerability
public async Task<ActionResult> CreateComment(string content)
{
    var comment = new Comment { Content = content };  // Unescaped
    dbContext.Comments.Add(comment);
    await dbContext.SaveChangesAsync();
    return Ok(comment);
}
// Attacker: content = "<script>alert('xss')</script>"
// Script executes in every user's browser who views comment

// ✅ GOOD: HTML encoding
public async Task<ActionResult> CreateComment(string content)
{
    // Server-side validation
    if (content.Length > 5000)
        return BadRequest("Comment too long");
    
    // Sanitize HTML
    var sanitizer = new HtmlSanitizer();
    var cleanContent = sanitizer.Sanitize(content);
    
    var comment = new Comment { Content = cleanContent };
    dbContext.Comments.Add(comment);
    await dbContext.SaveChangesAsync();
    return Ok(comment);
}

// In Razor view: Use @Html.Encode() or just @content (auto-escapes)
<div>@comment.Content</div>  <!-- HTML-encoded automatically -->
```

---

### Principle 4: CSRF Protection

```csharp
// ✅ GOOD: Anti-forgery token validation
[ValidateAntiForgeryToken]  // Built-in ASP.NET Core protection
[HttpPost("users")]
public async Task<ActionResult> CreateUser([Bind("Username,Email")] User user)
{
    // Token automatically validated
    if (ModelState.IsValid)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return RedirectToAction("Index");
    }
    return View(user);
}

// In HTML form
<form asp-action="CreateUser" method="post">
    @Html.AntiForgeryToken()  <!-- Generates hidden token -->
    <input asp-for="Username" />
    <input asp-for="Email" />
    <button type="submit">Create</button>
</form>
```

---

## 2️⃣ Authentication

### JWT Token Implementation

```csharp
public class AuthenticationService : IAuthenticationService
{
    private readonly IConfiguration _config;
    private readonly UserManager<User> _userManager;
    
    public async Task<string> AuthenticateAsync(string email, string password)
    {
        // 1. Validate credentials
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            throw new UnauthorizedAccessException("Invalid credentials");
        
        // 2. Generate JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config["JWT:Secret"]);
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("role", "user")
        };
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _config["JWT:Issuer"],
            Audience = _config["JWT:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

// JWT Configuration
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(config["JWT:Secret"])),
            ValidateIssuer = true,
            ValidIssuer = config["JWT:Issuer"],
            ValidateAudience = true,
            ValidAudience = config["JWT:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

---

### Multi-Factor Authentication (MFA)

```csharp
// Email-based MFA (TOTP)
public class MfaService
{
    public string GenerateOtpSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }
    
    public bool ValidateOtp(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedWindow);
    }
}

// Usage
[HttpPost("auth/verify-mfa")]
public async Task<ActionResult> VerifyMfa(string code)
{
    var user = await _userManager.GetUserAsync(User);
    if (user?.TwoFactorEnabled != true)
        return BadRequest("MFA not enabled");
    
    var secret = await _userManager.GetAuthenticatorKeyAsync(user);
    if (!_mfaService.ValidateOtp(secret, code))
        return Unauthorized("Invalid code");
    
    // Issue authenticated token
    var token = await _authService.GenerateTokenAsync(user);
    return Ok(new { token });
}
```

---

## 2️⃣ Authorization

### Role-Based Access Control (RBAC)

```csharp
// Define roles
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";
}

// Attribute-based authorization
[Authorize(Roles = RoleNames.Admin)]
[HttpDelete("users/{id}")]
public async Task<IActionResult> DeleteUser(int id)
{
    var user = await dbContext.Users.FindAsync(id);
    if (user == null)
        return NotFound();
    
    dbContext.Users.Remove(user);
    await dbContext.SaveChangesAsync();
    return NoContent();
}

// Claim-based authorization
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; set; }
}

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userPermissions = context.User.FindAll("permission");
        if (userPermissions.Any(c => c.Value == requirement.Permission))
            context.Succeed(requirement);
        
        return Task.CompletedTask;
    }
}

// Usage
services.AddAuthorizationBuilder()
    .AddPolicy("CanDeleteUsers", policy =>
        policy.Requirements.Add(new PermissionRequirement { Permission = "delete_users" }));

[Authorize(Policy = "CanDeleteUsers")]
[HttpDelete("users/{id}")]
public async Task<IActionResult> DeleteUser(int id) { ... }
```

---

## 🔒 Encryption

### Encryption at Rest

```csharp
// Using AES encryption for sensitive data
public class EncryptionService
{
    public string Encrypt(string plaintext, string key)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            aes.GenerateIV();
            
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream())
            {
                ms.Write(aes.IV, 0, aes.IV.Length);
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plaintext);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
    
    public string Decrypt(string ciphertext, string key)
    {
        var buffer = Convert.FromBase64String(ciphertext);
        using (var aes = Aes.Create())
        {
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            byte[] iv = new byte[aes.IV.Length];
            Array.Copy(buffer, 0, iv, 0, iv.Length);
            aes.IV = iv;
            
            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}

// Usage
public class User
{
    public int Id { get; set; }
    
    // Encrypt sensitive fields
    [Encrypted]
    public string SocialSecurityNumber { get; set; }
    
    [Encrypted]
    public string CreditCardNumber { get; set; }
}
```

---

### TLS/HTTPS Enforcement

```csharp
// Require HTTPS for all requests
app.UseHttpsRedirection();

// Add HSTS (HTTP Strict Transport Security)
app.UseHsts();  // Enforces HTTPS for 30 days

// Configure secure cookies
services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

---

## 🚫 Rate Limiting

### Implement Rate Limiting

```csharp
// Using AspNetCoreRateLimit
services.AddMemoryCache();
services.AddInMemoryRateLimiting();
services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Configuration
services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;  // Too Many Requests
    
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
    
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 100  // 100 requests per minute
        },
        new RateLimitRule
        {
            Endpoint = "*/auth/*",
            Period = "1h",
            Limit = 5   // 5 authentication attempts per hour
        }
    };
});

// Apply middleware
app.UseIpRateLimiting();
```

---

### Per-User Rate Limiting

```csharp
[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly IRateLimitStore _rateLimitStore;
    
    [HttpGet("search")]
    public async Task<ActionResult> Search(string query)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var key = $"search:{userId}";
        
        // Check rate limit
        var count = await _rateLimitStore.GetAsync(key);
        if (count >= 100)  // 100 searches per day
            return StatusCode(429, "Rate limit exceeded");
        
        // Process request
        var results = await _searchService.SearchAsync(query);
        
        // Update counter
        await _rateLimitStore.IncrementAsync(key, TimeSpan.FromDays(1));
        
        return Ok(results);
    }
}
```

---

## 🛡️ Additional Security Measures

### OWASP Top 10 Coverage

```
1. Injection ✅
   - Parameterized queries
   - Input validation
   - ORM framework (EF Core)

2. Broken Authentication ✅
   - JWT tokens with expiration
   - MFA support
   - Secure password hashing

3. Sensitive Data Exposure ✅
   - HTTPS enforced
   - Encryption at rest
   - Sensitive data encrypted

4. XML External Entities (XXE) ✅
   - Disable DTD processing
   - Validate XML input

5. Broken Access Control ✅
   - Role-based authorization
   - Claim-based policies
   - Resource-level checks

6. Security Misconfiguration ✅
   - HSTS headers
   - Security headers
   - Disable default accounts

7. XSS ✅
   - HTML encoding
   - Content Security Policy
   - Input validation

8. Insecure Deserialization ✅
   - Validate JSON structure
   - Use safe deserializers
   - Limit deserialization

9. Using Components with Known Vulnerabilities ✅
   - Dependency scanning
   - Regular updates
   - Security patches

10. Insufficient Logging & Monitoring ✅
    - Audit logging
    - Security event logging
    - Alert thresholds
```

---

### Security Audit Logging

```csharp
public class AuditService
{
    private readonly DbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public async Task LogAsync(string action, object resource, string result)
    {
        var user = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        
        var audit = new AuditLog
        {
            UserId = user,
            Action = action,
            Resource = JsonSerializer.Serialize(resource),
            Result = result,
            IpAddress = ip,
            Timestamp = DateTime.UtcNow
        };
        
        _context.AuditLogs.Add(audit);
        await _context.SaveChangesAsync();
    }
}

// Usage
[HttpPost("users")]
public async Task<ActionResult> CreateUser(CreateUserRequest request)
{
    var user = new User { Email = request.Email };
    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();
    
    await _auditService.LogAsync("CreateUser", user, "Success");
    return Ok(user);
}
```

---

## 📋 Security Checklist

- [ ] All inputs validated (whitelist approach)
- [ ] SQL injection prevented (parameterized queries)
- [ ] XSS prevention implemented (HTML encoding)
- [ ] CSRF tokens validated
- [ ] Authentication implemented (JWT + MFA)
- [ ] Authorization enforced (RBAC)
- [ ] Sensitive data encrypted at rest
- [ ] HTTPS enforced
- [ ] Security headers added
- [ ] Rate limiting implemented
- [ ] Audit logging enabled
- [ ] Dependency vulnerabilities scanned
- [ ] OWASP Top 10 addressed
- [ ] Security tests included
- [ ] Documentation updated

---

**Document Version**: 1.0  
**Last Updated**: Phase 4 Session  
**Status**: Security Hardening Complete
