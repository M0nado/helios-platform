## HELIOS v4.0 - DEPLOYMENT GUIDE

Complete deployment instructions for production environments.

### Prerequisites

- Node.js 14+ or 16+
- Docker (for containerized deployment)
- Kubernetes 1.18+ (for K8s deployment)
- npm or yarn
- Optional: Prometheus, Jaeger, Vault, AlertManager

### Docker Deployment

```dockerfile
FROM node:18-alpine

WORKDIR /app

# Copy package files
COPY package*.json ./
RUN npm ci --only=production

# Copy application
COPY . .

# Set environment
ENV NODE_ENV=production
ENV PORT=3000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD node -e "require('./src/index.js')" || exit 1

# Expose port
EXPOSE 3000

# Start application
CMD ["node", "app.js"]
```

**Build and run:**
```bash
docker build -t helios-v4:4.0.0 .
docker run -p 3000:3000 -e NODE_ENV=production helios-v4:4.0.0
```

### Kubernetes Deployment

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: helios-config
data:
  NODE_ENV: production
  LOG_LEVEL: info

---
apiVersion: v1
kind: Secret
metadata:
  name: helios-secrets
type: Opaque
stringData:
  VAULT_TOKEN: "s.xxxxxxxxxxxxxxxx"
  DATABASE_URL: "postgresql://..."

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: helios-api
  labels:
    app: helios
    version: v4
spec:
  replicas: 3
  selector:
    matchLabels:
      app: helios
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  template:
    metadata:
      labels:
        app: helios
        version: v4
    spec:
      containers:
      - name: api
        image: helios-v4:4.0.0
        imagePullPolicy: Always
        
        ports:
        - name: http
          containerPort: 3000
          protocol: TCP
        
        env:
        - name: NODE_ENV
          valueFrom:
            configMapKeyRef:
              name: helios-config
              key: NODE_ENV
        - name: PORT
          value: "3000"
        - name: VAULT_TOKEN
          valueFrom:
            secretKeyRef:
              name: helios-secrets
              key: VAULT_TOKEN
        
        # Readiness probe (app ready to accept traffic)
        readinessProbe:
          httpGet:
            path: /health/readiness
            port: 3000
            scheme: HTTP
          initialDelaySeconds: 10
          periodSeconds: 10
          timeoutSeconds: 5
          successThreshold: 1
          failureThreshold: 3
        
        # Liveness probe (app should be restarted)
        livenessProbe:
          httpGet:
            path: /health/liveness
            port: 3000
            scheme: HTTP
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        
        # Startup probe (for slow-starting apps)
        startupProbe:
          httpGet:
            path: /health/startup
            port: 3000
            scheme: HTTP
          initialDelaySeconds: 0
          periodSeconds: 10
          failureThreshold: 30  # 5 minutes
        
        # Resource requests and limits
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "1000m"
        
        # Security context
        securityContext:
          runAsNonRoot: true
          runAsUser: 1000
          readOnlyRootFilesystem: true
          capabilities:
            drop:
            - ALL
        
        # Volume mounts
        volumeMounts:
        - name: tmp
          mountPath: /tmp
      
      # Service account
      serviceAccountName: helios-api
      
      # Volumes
      volumes:
      - name: tmp
        emptyDir: {}
      
      # Pod disruption budget
      terminationGracePeriodSeconds: 30

---
apiVersion: v1
kind: Service
metadata:
  name: helios-api
  labels:
    app: helios
spec:
  type: ClusterIP
  ports:
  - name: http
    port: 80
    targetPort: 3000
    protocol: TCP
  selector:
    app: helios

---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: helios-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: helios-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 50
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
      - type: Percent
        value: 100
        periodSeconds: 15

---
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: helios-pdb
spec:
  minAvailable: 1
  selector:
    matchLabels:
      app: helios
```

**Deploy:**
```bash
kubectl apply -f deployment.yaml
kubectl rollout status deployment/helios-api
```

### Environment Variables

```bash
# Core
NODE_ENV=production
PORT=3000
LOG_LEVEL=info

# Database
DATABASE_URL=postgresql://user:pass@host:5432/db
DATABASE_POOL_SIZE=20

# Caching
REDIS_URL=redis://localhost:6379
CACHE_TTL=600

# External Services
VAULT_ADDR=http://vault:8200
VAULT_TOKEN=s.xxxxx
PROMETHEUS_URL=http://prometheus:9090
JAEGER_ENDPOINT=http://jaeger:6831
SLACK_WEBHOOK_URL=https://hooks.slack.com/...

# AI/ML
ML_MODEL_PATH=/models/cache-predictor
ANOMALY_SENSITIVITY=6

# Monitoring
METRICS_ENABLED=true
TRACING_ENABLED=true
ALERT_ENABLED=true
```

### Health Checks

The service exposes Kubernetes probes:

```bash
# Readiness (ready to accept traffic)
curl http://localhost:3000/health/readiness

# Liveness (service is alive)
curl http://localhost:3000/health/liveness

# Startup (application started)
curl http://localhost:3000/health/startup

# Full health
curl http://localhost:3000/health
```

### Scaling

**Horizontal Scaling:**
```yaml
minReplicas: 2    # Minimum pods
maxReplicas: 10   # Maximum pods
targetCPU: 70%    # Scale up at 70% CPU
targetMemory: 80% # Scale up at 80% memory
```

**Vertical Scaling:**
Increase resource requests/limits in pod spec.

### Monitoring Integration

**Prometheus scrape config:**
```yaml
scrape_configs:
- job_name: 'helios'
  static_configs:
  - targets: ['localhost:3000']
  metrics_path: '/metrics'
```

**Jaeger agent:**
```
jaeger:
  samplers:
    type: probabilistic
    param: 0.1
  reporters:
    - logSpans: true
```

### Backup & Recovery

```bash
# Backup database
pg_dump postgresql://user:pass@host/db > backup.sql

# Backup secrets
kubectl get secrets -o yaml > secrets-backup.yaml

# Restore
psql postgresql://user:pass@host/db < backup.sql
kubectl apply -f secrets-backup.yaml
```

### Rolling Updates

```bash
# Update deployment
kubectl set image deployment/helios-api \
  api=helios-v4:4.0.1 \
  --record

# Check rollout status
kubectl rollout status deployment/helios-api

# Rollback if needed
kubectl rollout undo deployment/helios-api
```

### Performance Tuning

1. **CPU/Memory:** Adjust resource requests/limits
2. **Replicas:** Increase minReplicas for baseline load
3. **Cache:** Adjust TTL values for your data
4. **Database:** Tune connection pool size
5. **Compression:** Enable gzip for responses >1KB

### Security Best Practices

1. Use read-only root filesystem
2. Run as non-root user (uid: 1000)
3. Drop all Linux capabilities
4. Use secrets for sensitive data
5. Enable Pod security policies
6. Use network policies for isolation
7. Regular security scanning

### Troubleshooting

```bash
# Check logs
kubectl logs -f deployment/helios-api

# Describe pod
kubectl describe pod <pod-name>

# Port forward
kubectl port-forward svc/helios-api 3000:3000

# Execute shell
kubectl exec -it <pod-name> -- sh

# Check metrics
kubectl top nodes
kubectl top pods
```

### References

- [Kubernetes Deployment](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/)
- [Pod Disruption Budgets](https://kubernetes.io/docs/tasks/run-application/configure-pdb/)
- [Horizontal Pod Autoscaling](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/)
- [Health Checks](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)
