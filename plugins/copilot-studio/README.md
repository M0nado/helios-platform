# HELIOS Copilot Studio connector

`helios-openapi.yaml` is the governed OpenAPI surface for Microsoft Copilot Studio and Power Platform custom connectors.

It exposes the same bounded broker operations as the OpenAI MCP server:

- status and catalog reads;
- event search;
- action-policy preview;
- creation of approval-pending action requests.

The connector does not expose arbitrary shell execution or direct tenant administration. Replace the placeholder server URL only after the development broker is deployed behind HTTPS. Configure authentication through the approved Entra/API Management or broker token design; never paste a credential into this file.
