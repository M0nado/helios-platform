# Reduced-chat protocol

Use one ongoing OpenAI/ChatGPT project conversation named **HELIOS Control Record**. Open separate
threads only for restricted incidents, disposable design experiments, legal/compliance review, or a
large implementation that becomes its own GitHub PR. Every child task returns one short result to
the master record.

| Surface | Permanent object |
|---|---|
| GitHub | one master control issue plus normal implementation PRs |
| Slack | one authoritative root thread in `#helios-control-plane` |
| Linear | one master issue/project; child issues only for real work |
| SharePoint | one `HELIOS_CONTROL_PLANE_CURRENT.md` plus immutable evidence |
| Azure DevOps | one epic/work item plus validation pipeline runs |
| OpenAI/ALVIS | one project binding and one master control conversation |

Every checkpoint carries `correlationId`, `runId`, `sourceSha`, `stage`, `status`, `approvalState`,
`summary`, `nextAction`, and evidence links. A message missing those fields is commentary, not
authority.
