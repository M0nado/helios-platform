# Isolated preview lane

Preview SDK evaluation runs only in this container or an equivalent disposable virtual
environment. Add exact, hashed Python dependencies to `requirements-preview.txt`; npm
experiments must use a local `package-lock.json`; and .NET experiments must use a
container-local NuGet cache and project. Never reference this directory from a product
project or copy its output into a release artifact.

Promotion requires an accepted ADR in `docs/architecture/decisions`, updated stable lock
files, and passing `python eng/integration-lanes/validate_lanes.py --contracts`.
The immutable base-image digest and non-publishing rules are recorded in the Dockerfile
and `preview-lane.json`; changing either is a security-review event.
