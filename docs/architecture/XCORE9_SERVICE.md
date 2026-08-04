# XCore-9 bounded evaluation service

XCore-9 is a C#-orchestrated policy evaluation service. Stable contracts cover sanitized run history, explicit feature extraction, route candidates and scores, worker-template leases, dependency-closed toolchains, retry decisions, negotiation records, and policy promotion decisions. Every operation retains a correlation ID or evidence link where it crosses a system boundary.

## Trust boundary

The service accepts only catalogued `WorkerTemplate` and `ToolchainDefinition` values. A template has a fixed prompt digest and declared CPU, memory, instance, and toolchain limits. Selection cannot create identities, permissions, tools, prompts, or templates. Authorization and audit are injected C# interfaces; production hosts must bind them to Guardian-approved implementations.

Run history stores only allowlisted finite numeric features normalized to `[0,1]`; prompts and responses are not contract fields. Negotiations store a digest rather than proposal content. Provider/model calls remain outside this service in approved AIHub Python adapters.

## Learning and promotion

Deterministic ranking, confidence calibration, anomaly detection, and prediction evaluation live in the F# analytics assembly. A routing policy can be promoted only after a passing holdout of the configured minimum size and minimum loss improvement, with evidence and authorization from an actor that is not XCore. Promotion retains the formerly active policy as the rollback policy. XCore can evaluate a proposal but cannot approve its own promotion.

No native component is introduced: no benchmark currently demonstrates a hotspot that justifies a C++ implementation or stable C ABI. The fleet autopilot likewise labels historical cost, speed, and quality figures unresolved until measured benchmark artifacts exist.
