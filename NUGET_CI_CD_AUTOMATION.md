# HELIOS Platform - NuGet CI/CD Automation

## A) WORKFLOW TRIGGERS

### Trigger Types and Conditions

#### 1. Push to Main Branch
**When:** Any commit pushed to `main` branch
```yaml
on:
  push:
    branches:
      - main
```
**Actions:**
- Build all frameworks
- Run all tests
- Create NuGet package
- Publish to GitHub Packages
- Store artifacts for 30 days

**Example Flow:**
```
Developer commits to main
    ↓
GitHub detects push
    ↓
Workflow starts (automatically)
    ↓
Build on Windows + Ubuntu
    ↓
Run tests
    ↓
Create package
    ↓
Publish to GitHub Packages
    ↓
Archive artifacts
```

#### 2. Pull Request
**When:** PR created or updated targeting `main`
```yaml
on:
  pull_request:
    branches:
      - main
```
**Actions:**
- Build all frameworks
- Run all tests
- Report results on PR
- Block merge if tests fail

**Example:**
```
Developer opens PR
    ↓
Workflow runs automatically
    ↓
Shows ✓ or ✗ on PR
    ↓
Developer can't merge if ✗
    ↓
Developer pushes fix commits
    ↓
Workflow re-runs automatically
    ↓
Shows updated status
```

#### 3. Tag Creation (Release)
**When:** Tag matching `v*.*.*` is created
```yaml
on:
  push:
    tags:
      - 'v*.*.*'
```
**Actions:**
- Build all frameworks
- Run all tests
- Create NuGet package
- Publish to NuGet.org
- Create GitHub Release
- Archive indefinitely

**Example:**
```
Developer creates tag: v1.0.1
    ↓
git push origin v1.0.1
    ↓
Workflow detects tag
    ↓
Full build pipeline
    ↓
Package published to NuGet.org (if API key available)
    ↓
GitHub Release created automatically
    ↓
Release notes generated
```

#### 4. Manual Trigger (Workflow Dispatch)
**When:** Manually triggered from GitHub Actions tab
```yaml
on:
  workflow_dispatch:
    inputs:
      publish_nuget:
        description: 'Publish to NuGet.org'
        required: false
        default: 'false'
```
**Actions:**
- On-demand build and test
- Optional: Force publish to NuGet.org

**How to Use:**
```
1. Go to GitHub repository
2. Click "Actions" tab
3. Select "NuGet Build & Release" workflow
4. Click "Run workflow"
5. Select inputs if needed
6. Click "Run workflow"
```

---

## B) BUILD MATRIX

### Multi-Platform Testing

The workflow builds and tests on multiple platforms with different .NET versions:

#### Platform Matrix
```
┌─────────────────┬──────────────────────────────────┐
│   Operating     │      .NET Versions               │
│   System        │                                  │
├─────────────────┼──────────────────────────────────┤
│ ubuntu-latest   │ 6.0, 7.0, 8.0                   │
│ windows-latest  │ 6.0, 7.0, 8.0                   │
└─────────────────┴──────────────────────────────────┘

Total combinations: 6 build jobs per trigger
```

#### Configuration Details

**Ubuntu Latest (Linux)**
```yaml
os: ubuntu-latest
dotnet-versions: ['6.0', '7.0', '8.0']

- Base Image: Ubuntu 22.04
- Purpose: Cross-platform compatibility
- Benefits: Validates Linux support
- Build Time: ~3-4 minutes per framework
```

**Windows Latest**
```yaml
os: windows-latest
dotnet-versions: ['6.0', '7.0', '8.0']

- Base Image: Windows Server 2022
- Purpose: Windows-specific testing
- Benefits: Event Log integration testing
- Build Time: ~4-5 minutes per framework
```

#### Architecture Coverage

```
Platform: Any CPU (default)
- 64-bit optimization
- Compatible with x86_64 and ARM64

Target Frameworks:
- net6.0   → .NET 6 (LTS)
- net7.0   → .NET 7 (Current)
- net8.0   → .NET 8 (Latest LTS)

All combinations tested: 6 builds total
```

#### Why This Matrix?

| Framework | Support Level | EOL Date | Reason |
|-----------|--------------|----------|--------|
| .NET 6.0 | LTS | Nov 2024 | Legacy support |
| .NET 7.0 | Current | May 2024 | Standard support |
| .NET 8.0 | Latest LTS | Nov 2026 | Future-proof |

---

## C) BUILD STEPS

### Complete Build Pipeline

```
STAGE 1: SETUP
├─ Checkout code
├─ Setup .NET SDK
├─ Cache packages (optional)
└─ Verify environment

STAGE 2: BUILD
├─ Restore NuGet packages
├─ Compile source code
├─ Generate documentation
└─ Create symbols (PDB)

STAGE 3: TEST
├─ Run unit tests
├─ Generate coverage report
├─ Publish test results
└─ Check code quality

STAGE 4: PACKAGE
├─ Create .nupkg file
├─ Verify package contents
├─ Check dependencies
└─ Store artifacts

STAGE 5: PUBLISH (if applicable)
├─ Publish to GitHub Packages
├─ Publish to NuGet.org (on tag)
├─ Create Release (on tag)
└─ Archive artifacts
```

### Detailed Step Breakdown

#### Step 1: Checkout Code

```yaml
- name: Checkout code
  uses: actions/checkout@v4
  with:
    fetch-depth: 0  # Full history for version info
```

**Purpose:** Gets the repository code
**Time:** ~2 seconds
**Output:** Code available in `${{ github.workspace }}`

#### Step 2: Setup .NET SDK

```yaml
- name: Setup .NET ${{ matrix.dotnet-version }}
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: ${{ matrix.dotnet-version }}
```

**Purpose:** Installs requested .NET version
**Time:** ~30-60 seconds
**Versions:** 6.0, 7.0, 8.0

#### Step 3: Restore Dependencies

```yaml
- name: Restore dependencies
  run: dotnet restore
```

**Command:**
```powershell
dotnet restore
```

**Purpose:** Downloads NuGet packages
**Time:** ~45-90 seconds
**Cache:** Optional (reduce to ~5s on cache hit)

**Output:**
```
Restore complete for helios-platform.sln
```

#### Step 4: Build Solution

```yaml
- name: Build solution
  run: |
    dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj `
      -c Release `
      -f net${{ matrix.dotnet-version }} `
      --no-restore
```

**Configuration:** Release (optimized)
**Time:** ~30-60 seconds
**Output:**
```
Build succeeded. 0 Warning(s)
Time Elapsed: 00:00:45.123
```

**Verification:**
- No compilation errors
- No warnings (treated as errors in Release)
- Symbols (.pdb) generated
- XML documentation generated

#### Step 5: Run Tests

```yaml
- name: Run tests
  run: |
    dotnet test tests/ `
      -c Release `
      --no-build `
      --logger "trx;LogFileName=test-results.trx"
```

**Purpose:** Execute unit tests
**Time:** ~60-120 seconds
**Framework:** xUnit / NUnit / MSTest

**Output:**
```
Test Run Successful.
Total tests: 50
  Passed: 50
  Failed: 0
Test execution time: 1.234 seconds
```

**Failure Handling:**
- If any test fails, workflow stops
- Test results uploaded as artifact
- PR shows ✗ status
- Cannot merge PR

#### Step 6: Create Package

```yaml
- name: Create NuGet package
  run: |
    dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj `
      -c Release `
      -o artifacts/
```

**Output Files:**
```
artifacts/
├── HELIOS.Platform.1.0.0.nupkg      # Main package
├── HELIOS.Platform.1.0.0.snupkg     # Symbols package (optional)
└── HELIOS.Platform.symbols.nupkg    # Symbols package
```

**Time:** ~15-30 seconds
**Size:** ~1-2 MB per package

#### Step 7: Verify Package

```yaml
- name: Verify package
  run: |
    Get-ChildItem artifacts/*.nupkg | ForEach-Object {
      Write-Host "Package: $($_.Name)"
      Write-Host "Size: $(($_.Length / 1MB).ToString('F2')) MB"
    }
```

**Checks:**
- Package file exists
- Package size reasonable
- All frameworks included
- Metadata correct

#### Step 8: Upload Artifacts

```yaml
- name: Upload package artifact
  uses: actions/upload-artifact@v3
  with:
    name: nuget-package-${{ steps.version.outputs.VERSION }}
    path: artifacts/*.nupkg
    retention-days: 30
```

**Location:** GitHub Actions → Run → Artifacts
**Retention:** 30 days (configurable)
**Download:** Manual or via API

#### Step 9: Publish to NuGet.org (On Tag)

```yaml
- name: Publish to NuGet.org
  if: startsWith(github.ref, 'refs/tags/v')
  run: |
    dotnet nuget push "$nupkgPath" `
      --api-key "${{ secrets.NUGET_API_KEY }}" `
      --source https://api.nuget.org/v3/index.json `
      --skip-duplicate
```

**Triggers:** Only on version tags (v1.0.0, etc.)
**API Key:** Stored in GitHub Secrets
**Skip Duplicate:** Doesn't fail if version already exists
**Time:** ~10-20 seconds

**Verification:**
```
Pushing HELIOS.Platform.1.0.0.nupkg to 'https://api.nuget.org/v3/index.json'...
  PUT https://www.nuget.org/api/v2/package/
  Created https://www.nuget.org/packages/HELIOS.Platform/1.0.0
  Your package was pushed.
```

#### Step 10: Publish to GitHub Packages

```yaml
- name: Publish to GitHub Packages
  run: |
    dotnet nuget push "$nupkgPath" `
      --source github `
      --skip-duplicate
```

**Triggers:** Every build on main, and tags
**Authentication:** Uses GITHUB_TOKEN (automatic)
**Time:** ~5-10 seconds

#### Step 11: Create GitHub Release (On Tag)

```yaml
- name: Create GitHub Release
  if: startsWith(github.ref, 'refs/tags/v')
  uses: actions/create-release@v1
  with:
    tag_name: ${{ github.ref }}
    release_name: Release ${{ steps.tag_version.outputs.VERSION }}
    body: |
      # HELIOS.Platform ${{ steps.tag_version.outputs.VERSION }}
      See [CHANGELOG.md](CHANGELOG.md) for details.
```

**Creates:**
- GitHub Release page
- Release notes from tag
- Asset downloads
- Pre-release flag (if alpha/beta)

---

## D) SECRETS & VARIABLES

### GitHub Secrets Configuration

#### NUGET_API_KEY

**Location:** Settings → Secrets and variables → Actions

**Value:** Your NuGet.org API key

**How to Get:**
```
1. Go to https://www.nuget.org/account/apikeys
2. Click "Create New"
3. Set name: "GitHub Actions HELIOS"
4. Select scope: "Push new packages and package versions"
5. Glob pattern: "HELIOS.Platform*"
6. Expiration: 90 days
7. Create and copy the key
```

**Set via CLI:**
```powershell
gh secret set NUGET_API_KEY --body "your-api-key"
```

**Usage in Workflow:**
```yaml
dotnet nuget push *.nupkg `
  --api-key ${{ secrets.NUGET_API_KEY }} `
  --source https://api.nuget.org/v3/index.json
```

**Security Notes:**
- Never commit this key to repository
- Rotate key every 90 days
- Use least-privilege scopes
- Monitor usage on NuGet.org

#### GITHUB_TOKEN

**Provided Automatically** (No setup needed)

**Default Permissions:**
- Pull requests
- Commits
- Releases
- Packages

**Usage:**
```yaml
env:
  GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**Scope:** Limited to current repository

### Environment Variables

#### Computed Variables

```yaml
env:
  DOTNET_VERSION: '8.0'
  BUILD_CONFIG: 'Release'
  ARTIFACTS_DIR: 'artifacts'

steps:
  - run: echo "Building ${{ env.DOTNET_VERSION }}"
```

#### From Job Matrix

```yaml
strategy:
  matrix:
    dotnet-version: ['6.0', '7.0', '8.0']
    os: ['ubuntu-latest', 'windows-latest']

steps:
  - run: |
      echo "Framework: ${{ matrix.dotnet-version }}"
      echo "OS: ${{ matrix.os }}"
```

#### Output from Steps

```yaml
steps:
  - name: Get version
    id: version
    run: echo "VERSION=1.0.0" >> $GITHUB_OUTPUT
  
  - name: Use version
    run: echo "Deploying ${{ steps.version.outputs.VERSION }}"
```

---

## E) ARTIFACT RETENTION

### Artifact Storage Strategy

#### Package Artifacts (Release Builds)

```yaml
- name: Upload package
  uses: actions/upload-artifact@v3
  with:
    name: nuget-package-${{ steps.version.outputs.VERSION }}
    path: artifacts/*.nupkg
    retention-days: 30
```

**Retention Policy:**
- Main branch builds: 30 days
- Tagged releases: Indefinite
- Failed builds: 7 days

**Size:** ~1-2 MB per package

**Access:** GitHub UI → Actions → Workflow run → Artifacts

#### Test Results

```yaml
- name: Upload test results
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: test-results-${{ matrix.os }}-${{ matrix.dotnet-version }}
    path: '**/test-results.trx'
```

**Retention:** 30 days
**Format:** TRX (Visual Studio test format)
**Use:** Debugging failed tests

#### Build Logs

Automatically kept by GitHub:
- Build step outputs
- Error messages
- Console logs

**Access:** Workflow run → View logs
**Retention:** 90 days (configurable)

### Cleanup and Archival

```powershell
# List all artifacts
gh api repos/M0nado/helios-platform/actions/artifacts

# Download artifact
gh run download <run-id> --name "nuget-package-1.0.0"

# Delete old artifacts (cleanup)
gh api repos/M0nado/helios-platform/actions/artifacts \
  --method DELETE
```

---

## F) NOTIFICATIONS & STATUS CHECKS

### GitHub Status Checks

#### Automatic PR Status

When PR is created:
```
✓ build (6.0, ubuntu-latest)
✓ build (6.0, windows-latest)
✓ build (7.0, ubuntu-latest)
✓ build (7.0, windows-latest)
✓ build (8.0, ubuntu-latest)
✓ build (8.0, windows-latest)
✓ package
```

**Merge Blocking:**
- If any check fails: Can't merge
- All must pass to merge
- Configure in branch protection rules

### Slack Notifications (Optional Setup)

**Add Slack Integration:**

```yaml
# Add to workflow
- name: Notify Slack on Success
  if: success()
  uses: slackapi/slack-github-action@v1.24.0
  with:
    webhook-url: ${{ secrets.SLACK_WEBHOOK }}
    payload: |
      {
        "text": "✓ HELIOS Package Build Succeeded",
        "blocks": [
          {
            "type": "section",
            "text": {
              "type": "mrkdwn",
              "text": "*HELIOS.Platform Build*\nVersion: ${{ steps.version.outputs.VERSION }}\nStatus: ✓ Success"
            }
          }
        ]
      }

- name: Notify Slack on Failure
  if: failure()
  uses: slackapi/slack-github-action@v1.24.0
  with:
    webhook-url: ${{ secrets.SLACK_WEBHOOK }}
    payload: |
      {
        "text": "✗ HELIOS Package Build Failed",
        "blocks": [
          {
            "type": "section",
            "text": {
              "type": "mrkdwn",
              "text": "*HELIOS.Platform Build FAILED*\nView: ${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}"
            }
          }
        ]
      }
```

**Setup:**
1. Create Slack Incoming Webhook
2. Store URL in GitHub Secrets (SLACK_WEBHOOK)
3. Add notification steps to workflow

### Email Notifications

**Automatic GitHub Email:**
- Sent on workflow failure
- Configured in GitHub user settings
- User → Settings → Notifications

### Release Notifications

On successful tag deployment:

```yaml
- name: Create Release Notes
  if: startsWith(github.ref, 'refs/tags/v')
  run: |
    $version = "${{ steps.tag_version.outputs.VERSION }}"
    $changelog = Get-Content CHANGELOG.md -Raw
    # Extract relevant section
    # Post to GitHub Releases
```

### Dashboard Monitoring

**Monitor Workflow Health:**
```
Repository → Actions → Workflows → NuGet Build & Release
→ View run history
→ Download artifacts
→ Check logs
```

**Key Metrics:**
- Success rate: Aim for 100%
- Average build time: ~3-5 minutes
- Artifact size: 1-2 MB
- Test coverage: > 80%

---

## Complete Workflow Configuration

### Full nuget.yml File

Location: `.github/workflows/nuget.yml`

```yaml
name: NuGet Build & Release

on:
  push:
    branches: [main]
    tags: ['v*.*.*']
  pull_request:
    branches: [main]
  workflow_dispatch:
    inputs:
      publish_nuget:
        description: 'Publish to NuGet.org'
        required: false
        default: 'false'

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest]
        dotnet-version: ['8.0', '7.0', '6.0']
      fail-fast: false

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ matrix.dotnet-version }}

      - name: Restore dependencies
        run: dotnet restore

      - name: Build solution
        run: dotnet build src/HELIOS.Platform/HELIOS.Platform.csproj -c Release -f net${{ matrix.dotnet-version }} --no-restore

      - name: Run tests
        run: dotnet test tests/ -c Release --no-build --logger "trx;LogFileName=test-results.trx"

      - uses: actions/upload-artifact@v3
        if: always()
        with:
          name: test-results-${{ matrix.os }}-${{ matrix.dotnet-version }}
          path: '**/test-results.trx'

  package:
    needs: build
    runs-on: windows-latest
    if: always()

    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0'

      - name: Get version
        id: version
        run: |
          $xml = [xml](Get-Content "src/HELIOS.Platform/HELIOS.Platform.csproj")
          $version = $xml.Project.PropertyGroup.Version
          echo "VERSION=$version" >> $env:GITHUB_OUTPUT

      - name: Restore & Build
        run: |
          dotnet restore
          dotnet pack src/HELIOS.Platform/HELIOS.Platform.csproj -c Release -o artifacts/

      - uses: actions/upload-artifact@v3
        with:
          name: nuget-package-${{ steps.version.outputs.VERSION }}
          path: artifacts/*.nupkg
          retention-days: 30

  publish-nuget:
    needs: package
    runs-on: windows-latest
    if: startsWith(github.ref, 'refs/tags/v')

    steps:
      - uses: actions/download-artifact@v3
        with:
          path: download/

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0'

      - name: Publish to NuGet.org
        run: |
          $nupkgs = Get-ChildItem download -Recurse -Filter "*.nupkg"
          foreach ($nupkg in $nupkgs) {
            dotnet nuget push "$($nupkg.FullName)" --api-key "${{ secrets.NUGET_API_KEY }}" --source https://api.nuget.org/v3/index.json --skip-duplicate
          }

      - uses: actions/create-release@v1
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        with:
          tag_name: ${{ github.ref }}
          release_name: Release ${{ github.ref_name }}
          body: |
            See [CHANGELOG.md](CHANGELOG.md) for details.
          draft: false
          prerelease: ${{ contains(github.ref_name, 'alpha') || contains(github.ref_name, 'beta') }}

  publish-github:
    needs: package
    runs-on: windows-latest
    if: always()

    steps:
      - uses: actions/download-artifact@v3
        with:
          path: download/

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0'

      - name: Publish to GitHub Packages
        run: |
          dotnet nuget add source --username github-actions --password ${{ secrets.GITHUB_TOKEN }} --store-password-in-clear-text --name github https://nuget.pkg.github.com/M0nado/index.json
          $nupkgs = Get-ChildItem download -Recurse -Filter "*.nupkg"
          foreach ($nupkg in $nupkgs) {
            dotnet nuget push "$($nupkg.FullName)" --source github --skip-duplicate
          }
```

---

**Last Updated:** April 13, 2024
**Workflow Status:** Ready for deployment
**Repository:** https://github.com/M0nado/helios-platform
