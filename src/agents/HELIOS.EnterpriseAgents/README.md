# HELIOS.EnterpriseAgents

Typed contracts and registry loading for the HELIOS Enterprise Deployment Manager sub-agent fleet.

## Responsibilities

- load and validate `enterprise-deployment-agent-fleet.v1.json`;
- load and validate `custom-connections.v1.json`;
- expose bounded agent and connection definitions to the HELIOS GUI, broker, Hermes, AIHub, and OpenAI provider;
- provide normalized task/result/evidence envelopes;
- keep production disabled until an external gate verifies GitHub Issue #162 is closed;
- reject undeclared or unsafe capabilities before runtime dispatch.

## Intended dependency direction

```text
HELIOS.ControlPlane / MonadoBlade.GUI
                 |
                 v
       HELIOS.EnterpriseAgents
                 |
        +--------+--------+
        |                 |
        v                 v
HELIOS.Azure       HELIOS.Integrations
        |                 |
        v                 v
Azure/Entra        Graph/Slack/Linear
        |
        v
Hermes / AIHub / OpenAI Provider
```

This module contains no Azure credentials, connector tokens, or OpenAI keys. It stores only policy and secret-reference names.
