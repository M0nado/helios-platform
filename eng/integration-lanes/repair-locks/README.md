# PR 293 dependency lock recovery

Source reviewed: `e7e10c14b26d5245141ca811735528392b645dd9`.

The Integration SDK lanes workflow failed at the stable npm audit and the isolated F# preview restore. The other three previously reported workflow failures (Build All Modules, Component Version Check, and Code Checks) pass on this source.

## Required repair

- Preserve `@modelcontextprotocol/sdk` exactly at `1.30.0` and all direct dependency declarations.
- Regenerate only the `fast-uri` and `qs` transitive npm entries with npm, then execute `npm ci --ignore-scripts`, `npm audit --audit-level=moderate`, and `npm run check`.
- Regenerate the isolated FSharpProbe lock using SDK `11.0.100-preview.6.26359.118` and `dotnet restore --force-evaluate`; immediately verify a subsequent locked restore and build.
- Do not manufacture or manually substitute package integrity hashes.
- Preserve all existing audit, locked-restore, preview-isolation, release, approval, and publication gates.
- Store candidate changes as a review patch and require exact-head CI after the reviewed lockfiles are committed.

No Azure operation, tenant permission change, credential access, package publication, merge, or paid model call is authorized by this recovery record.

Local package regeneration was blocked in the ChatGPT build container by DNS resolution failures for GitHub, npm, NuGet and the .NET SDK download host. Therefore the candidate hashes and a successful restore are not claimed here.
