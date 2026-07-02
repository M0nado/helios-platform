# HELIOS Branch Intelligence Dashboard

Generated: 2026-07-02T17:35:24Z

## Remote setup
| Remote | Enabled | URL configured | Action | Result |
|---|---|---|---|---|
| origin | False | False | skip | disabled |
| helios-control | False | False | skip | disabled |
| hermes-fleet-production | False | False | skip | disabled |
| xcore | False | False | skip | disabled |

## Branch ranking
| Branch | Score | Action | Files | Modules |
|---|---|---|---|---|
| work | 75 | compare-selectively | 11 | src/analytics, src/core, tests |

## Idea impact
| Category | Module | Source | Action | How it affects us |
|---|---|---|---|---|
| github-automation | .devcontainer | .devcontainer/README.md:11 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| azure-automation | .devcontainer | .devcontainer/README.md:19 | save-and-score | Improves deployment repeatability and environment verification through Azure CLI automation. |
| github-automation | .devcontainer | .devcontainer/README.md:87 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .devcontainer | .devcontainer/README.md:100 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .devcontainer | .devcontainer/README.md:142 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .devcontainer | .devcontainer/README.md:355 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:1 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:12 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:30 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:35 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| azure-automation | .github | .github/CODESPACES_GUIDE.md:37 | save-and-score | Improves deployment repeatability and environment verification through Azure CLI automation. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:82 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:101 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:152 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:177 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:208 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:212 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:216 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:257 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |
| github-automation | .github | .github/CODESPACES_GUIDE.md:263 | merge-or-rewrite | Improves branch pruning, PR selection, and dashboard freshness. |

## Connectivity
| Tool | Available | Authenticated | Detail |
|---|---|---|---|
| git | True | True | ['git version 2.43.0'] |
| GitHub CLI | False | False | gh not found |
| Azure CLI | False | False | az not found |
| .NET SDK | False | False | dotnet not found |
| Python | True | True | ['Python 3.12.13'] |

## Safe merge policy
1. Extract unique ideas before merging.
2. Merge only high-value, low-risk branches.
3. Archive stale ideas before pruning.
4. Prune only reviewed, low-value, already-merged branches.
5. Refresh this dashboard after each ranking run.
