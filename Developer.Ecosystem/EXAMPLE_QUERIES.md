## MONADO BLADE Developer Ecosystem - Example Queries & Responses

Complete examples of typical usage scenarios with expected responses from both Hermes and Copilot.

---

### Query Category 1: Code Generation

#### Query 1.1: Generate C# Async Method
```
Input Query:
"Generate a C# async method that fetches user data from an API and returns it as JSON"

Expected Hermes Response (150-300ms):
✅ Provider: Hermes 7B
Latency: 245ms

public async Task<User> FetchUserDataAsync(int userId)
{
    using (var client = new HttpClient())
    {
        var response = await client.GetAsync($"https://api.example.com/users/{userId}");
        response.EnsureSuccessStatusCode();
        
        var jsonContent = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<User>(jsonContent);
        
        return user;
    }
}

Expected Copilot Response (Fallback, 500-2000ms):
✅ Provider: GitHub Copilot (Fallback)
Latency: 1250ms
IsFallback: true

Same response with additional documentation and error handling
```

---

#### Query 1.2: Generate Python Data Processing Script
```
Input Query:
"Create a Python script that reads a CSV file and performs data analysis"

Expected Hermes Response (150-300ms):
✅ Provider: Hermes 7B
Latency: 280ms

import pandas as pd
import numpy as np

def analyze_csv(filepath):
    df = pd.read_csv(filepath)
    
    # Basic statistics
    print("Data Shape:", df.shape)
    print("\nColumn Statistics:")
    print(df.describe())
    
    # Missing values
    print("\nMissing Values:")
    print(df.isnull().sum())
    
    return df

if __name__ == "__main__":
    data = analyze_csv('data.csv')
```

---

### Query Category 2: Problem Solving

#### Query 2.1: Debug Connection Issue
```
Input Query:
"Why is my Docker container unable to connect to the host database on localhost:5432?"

Expected Hermes Response (150-300ms):
✅ Provider: Hermes 7B
Latency: 210ms

Docker containers have isolated networking by default. "localhost" or "127.0.0.1" 
inside the container refers to the container itself, not the host. Solutions:

1. Use host network mode:
   docker run --network host -it myimage

2. Use host.docker.internal (Docker Desktop):
   connection_string = "postgresql://host.docker.internal:5432/mydb"

3. Use the container network:
   docker inspect mycontainer | grep IPAddress
   # Then connect to that IP from other containers

4. Port mapping:
   docker run -p 5432:5432 -it myimage
```

---

#### Query 2.2: Optimize Database Query
```
Input Query:
"My SQL query is slow. How can I optimize: SELECT * FROM users WHERE status='active'"

Expected Hermes Response (150-300ms):
✅ Provider: Hermes 7B
Latency: 195ms

Performance optimization strategies:

1. Add an index on the status column:
   CREATE INDEX idx_users_status ON users(status);

2. Select only needed columns:
   SELECT id, name, email FROM users WHERE status='active';

3. Check query plan:
   EXPLAIN ANALYZE SELECT * FROM users WHERE status='active';

4. Consider partitioning:
   -- Partition by status or date for large tables

5. Add WHERE conditions:
   SELECT * FROM users WHERE status='active' AND created_at > NOW() - INTERVAL 1 MONTH;

Expected improvement: 5-100x faster depending on table size
```

---

### Query Category 3: Architecture & Design

#### Query 3.1: Microservices Architecture
```
Input Query:
"Design a microservices architecture for an e-commerce platform"

Expected Hermes Response (150-300ms):
✅ Provider: Hermes 7B
Latency: 290ms

E-Commerce Microservices Architecture:

Core Services:
- User Service: Authentication, profiles, preferences
- Product Service: Catalog, inventory management
- Order Service: Order processing, history
- Payment Service: Payment processing, billing
- Shipping Service: Logistics, tracking
- Notification Service: Email, SMS, push notifications

Technical Stack:
- API Gateway: Kong/Nginx for routing
- Message Queue: RabbitMQ/Kafka for async events
- Database: PostgreSQL (users), MongoDB (products)
- Cache: Redis for performance
- Service Mesh: Istio for inter-service communication

Communication Patterns:
- Synchronous: REST/gRPC for real-time operations
- Asynchronous: Event-driven for eventual consistency
- Example: Order → Triggers Payment → Triggers Shipment

Deployment:
- Docker containers
- Kubernetes orchestration
- CI/CD with GitHub Actions
```

---

#### Query 3.2: Fallback Strategy Design
```
Input Query:
"How should I design a fallback strategy for my AI queries?"

Expected Hermes Response (150-300ms):
✅ Provider: Hermes 7B
Latency: 215ms

Fallback Strategy Best Practices:

1. Primary + Fallback Architecture:
   - Primary: Hermes (fast, local, offline)
   - Fallback: Copilot (slower, network-based, best quality)

2. Timeout Strategy:
   - Primary timeout: 300ms
   - Fallback timeout: 2000ms
   - Total max wait: 2.3 seconds

3. Seamless Switching:
   - No user-visible switching
   - Log which provider was used
   - Cache responses

4. Error Handling:
   - If both fail: Return cached response or error message
   - Log failures for monitoring
   - Alert on repeated failures

5. Monitoring:
   - Track fallback frequency
   - Monitor latency for both
   - Alert if primary unavailable >5 mins
```

---

### Query Category 4: DevOps & Deployment

#### Query 4.1: Docker Compose Setup
```
Input Query:
"Create a Docker Compose file for a multi-service application with database"

Expected Hermes Response (150-300ms):
✅ Provider: Hermes 7B
Latency: 260ms

version: '3.8'

services:
  app:
    build: .
    ports:
      - "5000:5000"
    environment:
      - DATABASE_URL=postgresql://user:password@db:5432/appdb
    depends_on:
      - db
      - redis
    networks:
      - app-network

  db:
    image: postgres:15
    environment:
      - POSTGRES_USER=user
      - POSTGRES_PASSWORD=password
      - POSTGRES_DB=appdb
    volumes:
      - db_data:/var/lib/postgresql/data
    networks:
      - app-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U user"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    networks:
      - app-network

volumes:
  db_data:

networks:
  app-network:
    driver: bridge
```

---

### Query Category 5: WSL2 & Development Environment

#### Query 5.1: WSL2 Configuration
```
Input Query:
"How do I optimize my WSL2 environment for maximum performance?"

Expected Hermes Response (150-300ms):
✅ Provider: Hermes 7B
Latency: 220ms

WSL2 Performance Optimization:

1. Configure .wslconfig:
[wsl2]
memory=8GB           # Allocate 8GB to WSL2
processors=4         # Use 4 CPU cores
swap=2GB             # Swap space
localhostForwarding=true

[interop]
guiApplications=true # Enable GUI apps
appendWindowsPath=true

2. Storage Optimization:
- Use WSL2 filesystem (not Windows filesystem)
- Mount on /home or project directories
- Avoid C:\Windows\System32

3. DevDrive Integration:
- Store projects on E: (ReFS) for 40% speed boost
- Use E:\DevProjects for development

4. Docker Integration:
- Enable Docker Desktop WSL2 integration
- Run docker from WSL2 CLI

5. Performance Monitoring:
wsl -l -v                    # Check version
wsl -e free -h              # Memory usage
wsl -e top                  # Process usage
```

---

#### Query 5.2: Mount External Filesystem in WSL2
```
Input Query:
"How do I mount my E: drive (DevDrive) in WSL2?"

Expected Hermes Response (150-300ms):
✅ Provider: Hermes 7B
Latency: 185ms

WSL2 DevDrive Mounting:

1. Access from WSL2:
   # Mount is automatic if using latest Windows/WSL2
   ls /mnt/e/
   cd /mnt/e/

2. Create persistent mount in ~/.bashrc:
   sudo mkdir -p /dev/drive
   sudo mount -t drvfs E: /dev/drive

3. Add to /etc/fstab for auto-mount:
   E: /mnt/e drvfs defaults 0 0

4. Symlink for convenience:
   ln -s /mnt/e ~/e-drive
   cd ~/e-drive

5. Performance tips:
   - Keep large files on E: (ReFS)
   - Use /home for source code (faster)
   - Mirror important files between filesystems
```

---

### Query Category 6: Performance Benchmarking

#### Query 6.1: Benchmark Results
```
Expected Hermes Response:
✅ Provider: Hermes 7B
Latency: 245ms

MONADO BLADE Ecosystem Performance Benchmarks:

AI Response Latencies:
- Hermes 7B (GPU):     150-300ms
- Hermes 13B (GPU):    300-600ms
- Hermes 70B (GPU):    1-3 seconds
- Copilot API:         500-2000ms

Fallback Success Rates:
- Hermes only:         85%
- With Copilot fallback: 99.2%
- Average fallback rate: 15% (network/timeout)

System Performance:
- Query throughput:    10-50 queries/minute
- Memory usage:        2-8GB (7B model)
- GPU utilization:     60-90% during inference
- Disk I/O:            Minimal (models cached)

DevDrive Performance vs NTFS:
- File operations:     40% faster
- Build times:         35% faster
- Git operations:      30% faster

WSL2 Overhead:
- Docker exec:         <10ms
- Filesystem ops:      2-5% slower than native
- GPU pass-through:    <5% overhead
```

---

### Timeout & Fallback Examples

#### Scenario 1: Hermes Timeout → Copilot Fallback
```
User Query: "Complex architectural question"
Timeline:
  0ms    → Query sent to Hermes
  150ms  → Hermes processing...
  300ms  → Hermes timeout reached
  310ms  → Fallback to Copilot
  1500ms → Copilot responds
  
Response:
✅ Provider: GitHub Copilot (Fallback)
IsFallback: true
TotalLatency: 1500ms
Message: "Processing with Copilot due to Hermes timeout"
```

#### Scenario 2: Both Services Offline
```
User Query: "Generate code"
Timeline:
  0ms    → Query sent to Hermes
  300ms  → Hermes timeout (offline)
  310ms  → Fallback to Copilot
  2000ms → Copilot timeout (network down)
  
Response:
❌ Error: Both services unavailable
Please try again when Hermes (offline) or network connection is available
Last cached response: [if available]
```

#### Scenario 3: Hermes Success (No Fallback)
```
User Query: "Quick fix for typo"
Timeline:
  0ms    → Query sent to Hermes
  85ms   → Hermes responds
  
Response:
✅ Provider: Hermes 7B
IsFallback: false
Latency: 85ms
Message: "Fixed instantly with local Hermes"
```

---

### GUI Panel Example Outputs

#### Chat Panel Output
```
User (14:32:45):
"Generate a REST API endpoint in C#"

Hermes (14:32:46) [245ms]:
"public async Task<IActionResult> GetUsers()
{
    var users = await _context.Users.ToListAsync();
    return Ok(users);
}"

Provider: Hermes 7B
```

#### Context Panel Output
```
Language: C#
Project Type: API
Analysis Area: REST Endpoint Implementation

Status: Ready for code generation
```

#### Output Panel Output
```
✅ Query processed successfully
Provider: Hermes 7B
Response Time: 245ms
Token Count: 156

[Full formatted code output with syntax highlighting]
```

#### Settings Panel Output
```
Model: Hermes 13B (Balanced)
Temperature: 0.7
Max Tokens: 2048
Offline Mode: Enabled
GPU Acceleration: NVIDIA CUDA
Fallback Enabled: Yes
```

#### Tools Panel Output
```
🔄 Executing: git commit -am 'Update API'
✅ Git command executed successfully

[Commit message]
Author: dev@localhost
Date: 2024-XX-XX HH:MM:SS
```

#### DevDrive Panel Output
```
📍 Mount: E:
📊 Total: 400.00 GB
💿 Free: 350.00 GB
⚡ Boost: 40%
Status: Optimized
Last Backup: Today 10:30 AM
```

---

### Error Handling Examples

#### Error 1: Model Not Loaded
```
Query: "Generate code"
Response:
⚠️  Model not loaded
Loading Hermes 7B (this may take a moment)...
[Loads model]
Now processing your query...
```

#### Error 2: API Token Invalid
```
Query: "Request Copilot response"
Response:
❌ Copilot API Error: Invalid token
Fallback to Hermes (offline mode)
[Hermes responds]
```

#### Error 3: Low Memory
```
Query: "Generate with 70B model"
Response:
⚠️  Insufficient memory for 70B model
Available: 6GB, Required: 40GB
Options:
  1. Use 13B model instead (8GB)
  2. Close other applications
  3. Enable swap space
Proceed with 13B? (Y/N)
```

---

## Performance Expectations

### Response Time Matrix

| Query Type | Hermes 7B | Hermes 13B | Hermes 70B | Copilot |
|-----------|----------|-----------|-----------|---------|
| Simple code fix | 85ms | 150ms | 500ms | 800ms |
| Code generation | 200ms | 350ms | 1000ms | 1500ms |
| Problem analysis | 250ms | 400ms | 1500ms | 2000ms |
| Architecture design | 300ms | 600ms | 2000ms | 2500ms |

### Typical Usage Pattern

```
Morning (8am): 60 queries → 95% Hermes
Midday (12pm): 45 queries → 90% Hermes (slower due to load)
Afternoon (3pm): 80 queries → 85% Hermes (network issues)
Evening (6pm): 30 queries → 98% Hermes (clean shutdown)

Daily Average: 90% Hermes, 10% Copilot fallback
```

---

Last Updated: 2024
Status: Production Ready ✅

