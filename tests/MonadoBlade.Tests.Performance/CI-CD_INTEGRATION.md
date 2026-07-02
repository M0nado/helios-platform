# CI/CD Integration Guide for Performance Benchmarks

## GitHub Actions Integration

### Setup

Add this workflow file to your repository at `.github/workflows/performance-benchmarks.yml`:

```yaml
name: Performance Benchmarks

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]
  schedule:
    - cron: '0 2 * * *'  # Daily at 2 AM UTC

jobs:
  benchmark:
    name: Run Performance Benchmarks
    runs-on: windows-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v3
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Run Performance Benchmarks
        run: |
          cd tests/MonadoBlade.Tests.Performance
          dotnet run -c Release --artifacts BenchmarkResults
        continue-on-error: true

      - name: Check for regressions
        run: |
          if (Select-String -Path "tests/MonadoBlade.Tests.Performance/BenchmarkResults/*/BENCHMARK_SUMMARY.txt" -Pattern "CRITICAL" -ErrorAction SilentlyContinue) {
            Write-Output "CRITICAL performance regression detected!"
            exit 1
          }
        shell: pwsh
        continue-on-error: false

      - name: Upload benchmark results
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: benchmark-results-${{ github.run_id }}
          path: tests/MonadoBlade.Tests.Performance/BenchmarkResults/
          retention-days: 30

      - name: Comment PR with results
        if: github.event_name == 'pull_request'
        uses: actions/github-script@v6
        with:
          script: |
            const fs = require('fs');
            const path = require('path');
            
            const resultsDir = 'tests/MonadoBlade.Tests.Performance/BenchmarkResults';
            const dirs = fs.readdirSync(resultsDir).filter(f => 
              fs.statSync(path.join(resultsDir, f)).isDirectory()
            );
            
            if (dirs.length === 0) {
              console.log('No benchmark results found');
              return;
            }
            
            const latestDir = dirs.sort().reverse()[0];
            const summaryFile = path.join(resultsDir, latestDir, 'BENCHMARK_SUMMARY.txt');
            
            if (!fs.existsSync(summaryFile)) {
              console.log('No summary file found');
              return;
            }
            
            const summary = fs.readFileSync(summaryFile, 'utf8');
            const lines = summary.split('\n').slice(0, 30).join('\n');
            
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: `## 📊 Performance Benchmark Results\n\n\`\`\`\n${lines}\n\`\`\`\n\n[View detailed results](https://github.com/${{ github.repository }}/actions/runs/${{ github.run_id }})`
            });
```

### Features

✅ Runs on every push and PR
✅ Scheduled daily builds
✅ Automatic regression detection
✅ Artifact retention (30 days)
✅ PR comments with results
✅ Detailed performance tracking

## Azure Pipelines Integration

### Setup

Create `azure-pipelines.yml` in the repository root:

```yaml
trigger:
  - main
  - develop

schedules:
  - cron: "0 2 * * *"
    displayName: Daily benchmark run
    branches:
      include:
        - main

pool:
  vmImage: 'windows-latest'

variables:
  buildConfiguration: 'Release'
  dotnetVersion: '8.0.x'

stages:
  - stage: Build
    jobs:
      - job: BuildAndTest
        steps:
          - task: UseDotNet@2
            inputs:
              version: $(dotnetVersion)
              packageType: sdk

          - task: DotNetCoreCLI@2
            inputs:
              command: 'restore'

          - task: DotNetCoreCLI@2
            inputs:
              command: 'build'
              arguments: '--configuration $(buildConfiguration)'

  - stage: Benchmark
    jobs:
      - job: PerformanceBenchmarks
        steps:
          - task: UseDotNet@2
            inputs:
              version: $(dotnetVersion)

          - task: PowerShell@2
            inputs:
              targetType: 'inline'
              script: |
                cd tests/MonadoBlade.Tests.Performance
                dotnet run -c Release --artifacts BenchmarkResults
            continueOnError: true

          - task: PublishBuildArtifacts@1
            inputs:
              pathToPublish: '$(System.DefaultWorkingDirectory)/tests/MonadoBlade.Tests.Performance/BenchmarkResults'
              artifactName: 'benchmark-results-$(System.JobId)'
            condition: always()

          - task: PowerShell@2
            inputs:
              targetType: 'inline'
              script: |
                $summaryFile = Get-ChildItem -Path "tests/MonadoBlade.Tests.Performance/BenchmarkResults/**/BENCHMARK_SUMMARY.txt" -Recurse | Select-Object -First 1
                if ($null -ne $summaryFile) {
                  $content = Get-Content $summaryFile.FullName | Select-Object -First 30
                  Write-Host "##[section]Performance Summary:"
                  Write-Host $content
                }
                
                $criticalFound = Get-ChildItem -Path "tests/MonadoBlade.Tests.Performance/BenchmarkResults/**/BENCHMARK_SUMMARY.txt" -Recurse -ErrorAction SilentlyContinue | 
                  Where-Object { (Get-Content $_.FullName) -match "CRITICAL" }
                
                if ($null -ne $criticalFound) {
                  Write-Host "##vso[task.logissue type=error]CRITICAL performance regression detected!"
                  exit 1
                }
```

## Jenkins Integration

### Setup

Create `Jenkinsfile` in the repository root:

```groovy
pipeline {
    agent {
        label 'windows-agent'
    }

    environment {
        BUILD_CONFIG = 'Release'
        DOTNET_VERSION = '8.0'
    }

    stages {
        stage('Setup') {
            steps {
                sh '''
                    dotnet --version
                    dotnet --list-sdks
                '''
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet build -c ${BUILD_CONFIG}'
            }
        }

        stage('Run Benchmarks') {
            steps {
                dir('tests/MonadoBlade.Tests.Performance') {
                    sh '''
                        mkdir -p BenchmarkResults
                        dotnet run -c ${BUILD_CONFIG} --artifacts BenchmarkResults || true
                    '''
                }
            }
        }

        stage('Analyze Results') {
            steps {
                script {
                    def summaryFile = findFiles(glob: '**/BENCHMARK_SUMMARY.txt').first()
                    if (summaryFile != null) {
                        def summary = readFile(file: summaryFile.path)
                        echo summary
                        
                        if (summary.contains('CRITICAL')) {
                            error('CRITICAL performance regression detected!')
                        }
                    }
                }
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: '**/BenchmarkResults/**', allowEmptyArchive: true
            junit testResults: '**/BenchmarkResults/**/*.xml', allowEmptyResults: true
        }
        failure {
            emailext(
                subject: "Performance Benchmark Failed - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                body: "Performance benchmarks detected a critical regression.\nSee ${env.BUILD_URL}",
                to: "${env.BUILD_NOTIFICATION_EMAIL}"
            )
        }
    }
}
```

## GitLab CI/CD Integration

### Setup

Create `.gitlab-ci.yml` in the repository root:

```yaml
stages:
  - build
  - benchmark
  - analyze

variables:
  DOTNET_VERSION: "8.0"
  BUILD_CONFIG: "Release"

build:
  stage: build
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet build -c $BUILD_CONFIG

benchmark:
  stage: benchmark
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - cd tests/MonadoBlade.Tests.Performance
    - dotnet run -c $BUILD_CONFIG --artifacts BenchmarkResults
  artifacts:
    paths:
      - tests/MonadoBlade.Tests.Performance/BenchmarkResults/
    expire_in: 30 days
  allow_failure: true

analyze:
  stage: analyze
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - |
      SUMMARY_FILE=$(find . -name "BENCHMARK_SUMMARY.txt" | head -1)
      if [ -f "$SUMMARY_FILE" ]; then
        head -30 "$SUMMARY_FILE"
        if grep -q "CRITICAL" "$SUMMARY_FILE"; then
          echo "CRITICAL performance regression detected!"
          exit 1
        fi
      fi
  dependencies:
    - benchmark
```

## Local Development Integration

### Pre-commit Hook

Create `.git/hooks/pre-commit`:

```bash
#!/bin/bash
# Run quick performance checks before commit

echo "Running performance sanity checks..."

cd tests/MonadoBlade.Tests.Performance

# Run only core benchmarks (faster)
dotnet run -c Release --filter '*CoreModule*' || exit 1

echo "✓ Performance checks passed"
```

### Pre-push Hook

Create `.git/hooks/pre-push`:

```bash
#!/bin/bash
# Run full benchmarks before pushing

echo "Running full benchmark suite before push..."
echo "This may take a few minutes..."

cd tests/MonadoBlade.Tests.Performance
dotnet run -c Release --artifacts BenchmarkResults

# Check for critical regressions
if grep -r "CRITICAL" BenchmarkResults/*/BENCHMARK_SUMMARY.txt 2>/dev/null; then
    echo "⚠️  Critical performance regression detected!"
    echo "Please address regressions before pushing."
    exit 1
fi

echo "✓ All benchmarks passed"
```

## Regression Baseline Tracking

### Save Baseline

```powershell
# After confirming good performance
$baselineDir = "tests/MonadoBlade.Tests.Performance/BenchmarkResults/BASELINE"
mkdir $baselineDir -Force

# Run benchmarks
cd tests/MonadoBlade.Tests.Performance
dotnet run -c Release --artifacts $baselineDir

# Copy latest results as baseline
$latest = Get-ChildItem "BenchmarkResults" -Directory | Sort-Object CreationTime -Descending | Select-Object -First 1
Copy-Item "$latest/*" "$baselineDir/" -Recurse -Force

echo "Baseline saved to $baselineDir"
```

### Compare Against Baseline

```powershell
# Compare current run against baseline
$baselineFile = "tests/MonadoBlade.Tests.Performance/BenchmarkResults/BASELINE/summary.json"
$currentFile = "tests/MonadoBlade.Tests.Performance/BenchmarkResults/*/summary.json" | Get-ChildItem | Sort-Object CreationTime -Descending | Select-Object -First 1

if (Test-Path $baselineFile) {
    $baseline = Get-Content $baselineFile | ConvertFrom-Json
    $current = Get-Content $currentFile | ConvertFrom-Json
    
    # Calculate regressions
    foreach ($bench in $current.benchmarks) {
        $baseLine = $baseline.benchmarks | Where-Object { $_.name -eq $bench.name }
        if ($baseLine) {
            $regression = (($bench.mean - $baseLine.mean) / $baseLine.mean) * 100
            if ($regression -gt 10) {
                Write-Host "⚠️  WARNING: $($bench.name) regressed by ${regression}%"
            }
        }
    }
}
```

## Monitoring and Alerts

### Performance Dashboard

Create a dashboard in your CI/CD system to track:
- Average execution times over time
- Memory allocation trends
- GC collection frequency
- Regression patterns

### Alert Thresholds

Configure alerts for:
- **Performance**: > 10% regression triggers warning, > 20% blocks merge
- **Memory**: > 15% allocation increase triggers warning, > 30% blocks merge
- **Build Time**: If benchmark suite exceeds 30 minutes

### Notification Channels

- Slack/Teams: Post benchmark results to development channels
- Email: Send digest reports daily/weekly
- Pull Request: Comment with performance impact
- Metrics System: Send to Prometheus, DataDog, or New Relic

## Best Practices

✅ **Run benchmarks before every merge**
✅ **Track baseline performance**
✅ **Alert on regressions**
✅ **Archive results for trend analysis**
✅ **Document performance targets**
✅ **Review metrics before release**
✅ **Include benchmarks in code review**
✅ **Automate regression detection**

---

**For questions or issues**, refer to the main BENCHMARK_GUIDE.md
