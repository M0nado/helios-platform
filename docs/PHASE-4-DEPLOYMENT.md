# HELIOS Phase 4 - Deployment & Operations Guide

**Version:** 4.0.0  
**Status:** Production Ready  
**Date:** April 14, 2026

---

## 🚀 Deployment Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Load Balancer                          │
│              (Azure Load Balancer / AWS ALB)                │
└─────────────────────────────────────────────────────────────┘
                              │
                 ┌────────────┼────────────┐
                 ▼            ▼            ▼
        ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
        │  K8s Node 1  │ │  K8s Node 2  │ │  K8s Node 3  │
        │   Backend    │ │   Backend    │ │   Backend    │
        │    Pod 1     │ │    Pod 2     │ │    Pod 3     │
        └──────────────┘ └──────────────┘ └──────────────┘
                 │            │            │
                 └────────────┼────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
    ┌────────────┐      ┌─────────────┐      ┌──────────┐
    │ PostgreSQL │      │    Redis    │      │  S3/Blob │
    │  Database  │      │   Cache     │      │  Storage │
    │  (Primary  │      │  (Cluster)  │      │          │
    │  + Replicas)       └─────────────┘      └──────────┘
    └────────────┘
```

---

## 📦 Docker Deployment

### Dockerfile (Production)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Add non-root user for security
RUN useradd -m -u 1001 appuser && chown -R appuser:appuser /app

COPY --from=builder /app/publish .
USER appuser

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/api/v1/health || exit 1

ENTRYPOINT ["dotnet", "HELIOS.Platform.dll"]
```

### Building & Pushing

```bash
# Build image
docker build -t helios-backend:4.0.0 -t helios-backend:latest .

# Tag for registry
docker tag helios-backend:latest myregistry.azurecr.io/helios-backend:4.0.0
docker tag helios-backend:latest myregistry.azurecr.io/helios-backend:latest

# Push to Azure Container Registry
docker push myregistry.azurecr.io/helios-backend:4.0.0
docker push myregistry.azurecr.io/helios-backend:latest

# Verify
docker push myregistry.azurecr.io/helios-backend:latest && echo "✅ Push successful"
```

---

## 🐳 Kubernetes Deployment

### Secrets & ConfigMaps

```yaml
# secrets.yaml - Create before deploying
apiVersion: v1
kind: Secret
metadata:
  name: helios-backend-secrets
type: Opaque
stringData:
  jwt-secret: "your-super-secret-jwt-key-min-32-chars"
  db-connection: "Server=postgres.default.svc.cluster.local;User Id=sa;Password=YourPassword;Database=helios"
  redis-connection: "redis.default.svc.cluster.local:6379"
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: helios-backend-config
data:
  jwt-issuer: "https://helios.example.com"
  jwt-audience: "helios-api"
  jwt-expiration-minutes: "60"
  log-level: "Information"
```

### Deployment

```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: helios-backend
  labels:
    app: helios-backend
    version: v4.0.0
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: helios-backend
  template:
    metadata:
      labels:
        app: helios-backend
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "5000"
    spec:
      serviceAccountName: helios-backend-sa
      securityContext:
        runAsNonRoot: true
        runAsUser: 1001
      containers:
      - name: backend
        image: myregistry.azurecr.io/helios-backend:4.0.0
        imagePullPolicy: Always
        ports:
        - containerPort: 5000
          name: http
          protocol: TCP
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Production
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: helios-backend-secrets
              key: jwt-secret
        - name: DB_CONNECTION
          valueFrom:
            secretKeyRef:
              name: helios-backend-secrets
              key: db-connection
        - name: REDIS_CONNECTION
          valueFrom:
            secretKeyRef:
              name: helios-backend-secrets
              key: redis-connection
        - name: JWT_ISSUER
          valueFrom:
            configMapKeyRef:
              name: helios-backend-config
              key: jwt-issuer
        - name: JWT_AUDIENCE
          valueFrom:
            configMapKeyRef:
              name: helios-backend-config
              key: jwt-audience
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /api/v1/health/live
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /api/v1/health/ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 2
        volumeMounts:
        - name: logs
          mountPath: /app/logs
      volumes:
      - name: logs
        emptyDir: {}
---
apiVersion: v1
kind: Service
metadata:
  name: helios-backend-service
  labels:
    app: helios-backend
spec:
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 5000
    protocol: TCP
    name: http
  selector:
    app: helios-backend
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: helios-backend-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: helios-backend
  minReplicas: 3
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
```

### Deployment Steps

```bash
# 1. Create namespace
kubectl create namespace helios-prod

# 2. Create secrets
kubectl apply -f secrets.yaml -n helios-prod

# 3. Create config maps
kubectl apply -f configmaps.yaml -n helios-prod

# 4. Deploy application
kubectl apply -f deployment.yaml -n helios-prod

# 5. Verify deployment
kubectl get pods -n helios-prod
kubectl logs deployment/helios-backend -n helios-prod -f

# 6. Verify service
kubectl get service helios-backend-service -n helios-prod

# 7. Test endpoint
kubectl port-forward service/helios-backend-service 8080:80 -n helios-prod
curl http://localhost:8080/api/v1/health
```

---

## 🔄 GitOps Deployment (ArgoCD)

### ArgoCD Application

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: helios-backend
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/M0nado/helios-platform
    path: k8s/
    targetRevision: main
  destination:
    server: https://kubernetes.default.svc
    namespace: helios-prod
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
    - CreateNamespace=true
```

### Deployment with ArgoCD

```bash
# Install ArgoCD (if not already installed)
kubectl create namespace argocd
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml

# Deploy application
kubectl apply -f argocd-application.yaml

# Monitor deployment
argocd app get helios-backend
argocd app wait helios-backend

# Trigger sync (if needed)
argocd app sync helios-backend
```

---

## 📊 Monitoring & Observability

### Prometheus Metrics

```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: helios-backend
spec:
  selector:
    matchLabels:
      app: helios-backend
  endpoints:
  - port: http
    interval: 30s
    path: /metrics
```

### Prometheus Alerts

```yaml
apiVersion: monitoring.coreos.com/v1
kind: PrometheusRule
metadata:
  name: helios-backend-alerts
spec:
  groups:
  - name: helios-backend
    interval: 30s
    rules:
    - alert: HighErrorRate
      expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
      for: 5m
      labels:
        severity: critical
      annotations:
        summary: "High error rate detected"
        description: "Error rate > 5% for 5 minutes"
    
    - alert: SlowResponse
      expr: histogram_quantile(0.95, http_request_duration_seconds) > 2
      for: 5m
      labels:
        severity: warning
      annotations:
        summary: "Slow API response times"
        description: "P95 latency > 2 seconds"
    
    - alert: HighMemoryUsage
      expr: container_memory_usage_bytes > 450000000
      for: 5m
      labels:
        severity: warning
      annotations:
        summary: "High memory usage"
        description: "Pod memory usage > 450MB"
```

---

## 🔐 Security Practices

### HTTPS/TLS

```yaml
apiVersion: cert-manager.io/v1
kind: Certificate
metadata:
  name: helios-backend-cert
spec:
  secretName: helios-backend-tls
  issuerRef:
    name: letsencrypt-prod
    kind: ClusterIssuer
  dnsNames:
  - api.helios.example.com
```

### Network Policy

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: helios-backend-netpol
spec:
  podSelector:
    matchLabels:
      app: helios-backend
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 5000
  egress:
  - to:
    - podSelector:
        matchLabels:
          app: postgres
    ports:
    - protocol: TCP
      port: 5432
  - to:
    - podSelector:
        matchLabels:
          app: redis
    ports:
    - protocol: TCP
      port: 6379
  - to:  # Allow DNS
    - namespaceSelector: {}
      podSelector:
        matchLabels:
          k8s-app: kube-dns
    ports:
    - protocol: UDP
      port: 53
```

---

## 🧪 Load Testing

### Using Apache Bench

```bash
# Warm up
ab -n 1000 -c 10 https://api.helios.example.com/api/v1/health

# Load test
ab -n 100000 -c 500 https://api.helios.example.com/api/v1/health
```

### Using k6

```javascript
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  stages: [
    { duration: '30s', target: 100 },
    { duration: '1m30s', target: 1000 },
    { duration: '30s', target: 0 },
  ],
};

export default function () {
  let response = http.get('https://api.helios.example.com/api/v1/health');
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 200ms': (r) => r.timings.duration < 200,
  });
}
```

Run: `k6 run load-test.js`

---

## 📋 Operational Runbooks

### Scaling Up

```bash
# Manual scale
kubectl scale deployment helios-backend --replicas=5 -n helios-prod

# Verify
kubectl get pods -n helios-prod

# Check HPA status
kubectl get hpa helios-backend-hpa -n helios-prod -w
```

### Rolling Update

```bash
# Update image
kubectl set image deployment/helios-backend \
  backend=myregistry.azurecr.io/helios-backend:4.0.1 \
  -n helios-prod

# Monitor rollout
kubectl rollout status deployment/helios-backend -n helios-prod
```

### Rollback

```bash
# Automatic rollback on failure
kubectl rollout undo deployment/helios-backend -n helios-prod

# Check status
kubectl rollout history deployment/helios-backend -n helios-prod
```

### Restart Pods

```bash
# Graceful restart
kubectl rollout restart deployment/helios-backend -n helios-prod

# Force restart (if necessary)
kubectl delete pods -l app=helios-backend -n helios-prod
```

### Logs & Debugging

```bash
# View logs
kubectl logs deployment/helios-backend -n helios-prod -f

# View logs from specific pod
kubectl logs pod-name -n helios-prod -f

# Execute commands in pod
kubectl exec -it pod-name -n helios-prod -- bash

# Port forward for local debugging
kubectl port-forward pod/pod-name 5000:5000 -n helios-prod
```

---

## 🚨 Incident Response

### API Not Responding

```bash
# 1. Check pod status
kubectl get pods -n helios-prod

# 2. Check recent logs
kubectl logs deployment/helios-backend -n helios-prod --tail=100

# 3. Check resource usage
kubectl top pods -n helios-prod

# 4. Check events
kubectl get events -n helios-prod --sort-by='.lastTimestamp'

# 5. Restart if necessary
kubectl rollout restart deployment/helios-backend -n helios-prod
```

### Database Connection Issues

```bash
# Check database connectivity
kubectl exec -it pod/helios-backend-xxxxx -n helios-prod -- \
  dotnet /app/DbCheck.dll

# Verify secrets
kubectl get secret helios-backend-secrets -n helios-prod -o yaml
```

---

## 📈 Performance Tuning

### Database Connection Pooling

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=postgres;Max Pool Size=50;Min Pool Size=5;..."
  }
}
```

### Redis Optimization

```json
{
  "Redis": {
    "Connection": "redis:6379,allowAdmin=true,connectTimeout=5000",
    "ConfigurationOptions": {
      "AllowAdmin": true,
      "ConnectTimeout": 5000,
      "SyncTimeout": 5000
    }
  }
}
```

---

**Deployment & Operations Guide Complete** ✅
