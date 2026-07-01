# hermes-fleet-production Inventory

Source remote: `https://github.com/Yolkster64/hermes-fleet-platforms.git`

Fetched branches:

| Branch | SHA | Notes |
|---|---|---|
| `hermes-fleet-production/main` | `7f42640422bfde5d05fab3aacf20f5616f817636` | Large platform branch with GUI, workflows, docs, and existing HELIOS artifacts. |
| `hermes-fleet-production/chore/update-marker` | `0dbe237062b07615d53ed6032e9a320a08c76cfc` | Adds X-tier polyglot C++/C#/Python/Hermes/XCore/AIHub assets. Selected assets imported. |
| `hermes-fleet-production/sre/restore-and-azure-scaffold-no-workflow-20260609` | `9e3fa731358060753f00dd4268126d74d070e085` | Adds Azure scaffold. Selected assets imported under `infra/hermes-fleet/`. |

## Imported assets

| Source concern | Imported target path |
|---|---|
| Hermes/XCore Python adapter modules | `src/python/hermes_xcore/src/hermes_xcore/` |
| Imported Python training/feedback/vector reference material | `docs/integration/hermes-xcore/imported-python-reference/` |
| C++ X-tier runtime/performance assets | `src/native/HELIOS.Performance/x-tier/` |
| C# Polyglot X-tier host assets | `src/integrations/Hermes/PolyglotXTier/` |
| Hermes/XCore operator scripts | `scripts/hermes-xcore/` |
| Hermes/XCore integration docs | `docs/integration/hermes-xcore/` |
| Azure fleet scaffold | `infra/hermes-fleet/` |

## Deferred assets

The remote contains a very large amount of historical root documentation, generated artifacts, binaries, and overlapping workflows. Those were not bulk-merged to avoid reintroducing contradictory status claims and unstable workflows.

## Merge decision log

- Accepted targeted X-tier/Hermes/XCore/Python/C++/C#/Azure assets from `chore/update-marker` and SRE Azure branch.
- Deferred full branch merge because `HEAD..hermes-fleet-production/main` contains thousands of files and millions of inserted lines, including historical/generated content.

## Python import hygiene

Some imported `python/x-tier/hermes_xcore/imported/*.py` files contain prose fragments after executable code. They were preserved as `.py.txt` reference material under `docs/integration/hermes-xcore/imported-python-reference/` instead of shipping as importable package modules.
