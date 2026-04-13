# GitHub Project Automation Rules Guide
**Complete Automation Configuration** | **4+ Automation Rules**

---

## Table of Contents
1. [Automation Overview](#automation-overview)
2. [Rule 1: Auto-Add PRs](#rule-1-auto-add-prs)
3. [Rule 2: Auto-Move on Label](#rule-2-auto-move-on-label)
4. [Rule 3: Auto-Archive After 7 Days](#rule-3-auto-archive-after-7-days)
5. [Additional Rules](#additional-rules)
6. [Automation Best Practices](#automation-best-practices)
7. [Troubleshooting](#troubleshooting)

---

## Automation Overview

### What is Automation?

GitHub Project automation rules execute actions automatically when specific conditions are met:

```
Trigger (When) → Condition (If) → Action (Then)
```

### Benefits

- **Reduce Manual Work**: Eliminate repetitive manual updates
- **Improve Consistency**: Standardize workflows across team
- **Enable Scale**: Work with dozens/hundreds of issues without overhead
- **Real-time Updates**: Items move automatically as status changes
- **Reduce Errors**: Automation more reliable than manual updates

### Automation Capabilities

| Capability | Available | Example |
|-----------|-----------|---------|
| **Move Issues** | ✅ | Move to Done when PR merged |
| **Add Labels** | ✅ | Add "phase-1" when Phase field set |
| **Archive Issues** | ✅ | Archive after 7 days in Done |
| **Add to Milestones** | ✅ | Add to "Foundation" milestone |
| **Change Custom Fields** | ✅ | Update status when column changes |
| **Notifications** | ⚠️ | Limited (send to Slack) |
| **Create Issues** | ❌ | Not supported yet |

---

## Rule 1: Auto-Add PRs

### Purpose
Automatically add pull requests to the project board and move them to the correct column based on their status.

### Configuration

#### Trigger
```
When: Pull request opened
```

#### Conditions
```
If: PR is linked to an issue in the project
```

#### Actions
```
Then:
  1. Add PR to project board
  2. Move to "👀 In Review" column
  3. Add label: "needs-review"
  4. Add label: "{status}"
```

#### Implementation Steps

**Step 1**: Navigate to **Project Settings** → **Automation**

**Step 2**: Click **Create automation**

**Step 3**: Choose trigger
```
When: Pull request
```

**Step 4**: Select the exact trigger
```
When: Pull request is opened
```

**Step 5**: Add conditions
```
Filter: Linked to an issue in this project
```

**Step 6**: Add actions
```
Set custom field: Status → "In Review"
Add to project: This project
Add label: "needs-review"
```

**Step 7**: Save automation

#### YAML Configuration (if available)
```yaml
name: "Auto-add PRs to project"
trigger:
  event: pull_request
  action: opened
conditions:
  - linked_to_project: true
actions:
  - add_to_project: true
  - set_column: "In Review"
  - add_label: "needs-review"
  - update_field:
      field: "Status Detail"
      value: "Code Review"
```

### Testing the Rule

**Test Case 1**: Open new PR linked to an issue
- Expected: PR added to project in "In Review" column
- Label "needs-review" added

**Test Case 2**: Open new PR NOT linked to issue
- Expected: No change (condition not met)

**Test Case 3**: Link PR to issue after PR is open
- Expected: PR added to project
- Note: Trigger was already fired, may need manual update

### Workflow Impact

```
Issue Created
    ↓
[Developer opens PR linked to issue]
    ↓ (Automation triggers)
[PR automatically added to board]
[PR moved to "In Review" column]
[Label "needs-review" added]
    ↓
[Reviewer reviews PR]
    ↓
[Developer makes changes or gets approval]
```

---

## Rule 2: Auto-Move on Label

### Purpose
Automatically move issues between columns based on labels added or removed.

### Configuration

#### Triggers (Multiple Rules)
```
Trigger 1: When label "ready" is added
Trigger 2: When label "in-progress" is added
Trigger 3: When label "blocked" is added
Trigger 4: When label "review-ready" is added
```

#### Actions per Trigger

**Trigger 1**: Label "ready" added
```
Then:
  - Move to: "📝 Todo" column
  - Set Status Detail: "Ready"
  - Add milestone: (inferred from Phase)
  - Add label: "priority-medium" (if not set)
```

**Trigger 2**: Label "in-progress" added
```
Then:
  - Move to: "🔄 In Progress" column
  - Set Status Detail: "In Development"
  - Ensure: Issue assigned (if not, add label "needs-assignee")
```

**Trigger 3**: Label "blocked" added
```
Then:
  - Move to: "🔄 In Progress" column
  - Set Status Detail: "Blocked"
  - Set Health Status: "At-Risk"
  - Add label: "escalate"
```

**Trigger 4**: Label "review-ready" added
```
Then:
  - Move to: "👀 In Review" column
  - Set Status Detail: "Code Review"
  - Add label: "awaiting-review"
```

#### Implementation Steps

**For each trigger**, repeat:

1. Navigate to **Project Settings** → **Automation**
2. Click **Create automation**
3. Choose trigger:
   ```
   When: Issue
   ```
4. Select action:
   ```
   When: Label is added
   ```
5. Specify label:
   ```
   Label name: "ready"
   ```
6. Add actions:
   ```
   Set column: "Todo"
   Update custom field "Status Detail": "Ready"
   ```
7. Save automation

#### YAML Configuration
```yaml
rules:
  - name: "Auto-move when ready"
    trigger:
      event: issue
      action: labeled
      label: "ready"
    actions:
      - move_to_column: "📝 Todo"
      - update_field:
          field: "Status Detail"
          value: "Ready"

  - name: "Auto-move when in progress"
    trigger:
      event: issue
      action: labeled
      label: "in-progress"
    actions:
      - move_to_column: "🔄 In Progress"
      - update_field:
          field: "Status Detail"
          value: "In Development"

  - name: "Auto-flag when blocked"
    trigger:
      event: issue
      action: labeled
      label: "blocked"
    actions:
      - move_to_column: "🔄 In Progress"
      - update_field:
          field: "Status Detail"
          value: "Blocked"
      - update_field:
          field: "Health Status"
          value: "At-Risk"

  - name: "Auto-move to review"
    trigger:
      event: issue
      action: labeled
      label: "review-ready"
    actions:
      - move_to_column: "👀 In Review"
      - update_field:
          field: "Status Detail"
          value: "Code Review"
```

### Testing the Rule

**Test Case 1**: Add "ready" label to an issue
- Expected: Issue moves to "Todo" column
- Status Detail changes to "Ready"

**Test Case 2**: Add "blocked" label to an issue
- Expected: Issue stays in current column
- Status Detail changes to "Blocked"
- Health Status changes to "At-Risk"
- Label "escalate" added

**Test Case 3**: Add "review-ready" label to an issue
- Expected: Issue moves to "In Review" column
- Status Detail changes to "Code Review"

### Workflow Impact

```
Developer working on issue
    ↓
[Adds label "in-progress"]
    ↓ (Automation triggers)
[Issue moves to "In Progress" column]
[Status Detail updated to "In Development"]
    ↓
[Work continues, PR created]
    ↓
[Adds label "review-ready"]
    ↓ (Automation triggers)
[Issue moves to "In Review" column]
[Status Detail updated to "Code Review"]
    ↓
[Reviewer reviews PR]
```

---

## Rule 3: Auto-Archive After 7 Days

### Purpose
Automatically archive completed items after they've been in Done for 7 days, keeping the board clean.

### Configuration

#### Trigger
```
When: Issue remains in "Done" column for 7+ days
```

#### Conditions
```
If:
  - Column: "✅ Done"
  - Days in column: >= 7
  - No activity: >= 3 days
```

#### Actions
```
Then:
  - Archive issue (remove from view)
  - Add label: "archived"
  - Update timestamp field: "archived-date" (if available)
```

#### Implementation Steps

**Note**: GitHub's built-in automation has limited date-based triggers. This may require:
- **Option 1**: Scheduled GitHub Actions workflow (custom)
- **Option 2**: Manual archive via view filtering + bulk actions
- **Option 3**: Third-party automation (Zapier, etc.)

**Option 1: GitHub Actions Workflow**

Create `.github/workflows/auto-archive.yml`:

```yaml
name: Auto-archive done items
on:
  schedule:
    - cron: '0 0 * * *'  # Daily at midnight UTC

jobs:
  archive:
    runs-on: ubuntu-latest
    steps:
      - name: Archive items done for 7+ days
        uses: actions/github-script@v6
        with:
          script: |
            const sevenDaysAgo = new Date();
            sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);
            
            const items = await github.graphql(`
              query {
                organization(login: "your-org") {
                  projectV2(number: PROJECT_NUMBER) {
                    items(first: 100, filter: {
                      state: DONE
                    }) {
                      nodes {
                        id
                        updatedAt
                      }
                    }
                  }
                }
              }
            `);
            
            for (const item of items.data.organization.projectV2.items.nodes) {
              const updatedDate = new Date(item.updatedAt);
              if (updatedDate < sevenDaysAgo) {
                // Archive item
                await github.graphql(`
                  mutation {
                    archiveProjectV2Item(input: {
                      projectId: "PROJECT_ID"
                      itemId: "${item.id}"
                    }) {
                      clientMutationId
                    }
                  }
                `);
              }
            }
```

**Option 2: Manual Batch Archive**

1. Create view: "Ready to Archive"
   - Filter: `Column = "Done"` AND `Days since update > 7`
   - Sort: Oldest first

2. Use GitHub's bulk select:
   - Select all items in view
   - Use context menu → Archive

3. Run weekly or as needed

**Option 3: Built-in Column Automation**

GitHub allows optional automation on columns:

1. Open **Project Settings** → **Board configuration**
2. Click on "✅ Done" column
3. Enable: "Auto-archive after 7 days" (if available)

#### Testing the Rule

**Test Case 1**: Move item to "Done"
- Expected: No immediate action
- Item remains visible

**Test Case 2**: Item in "Done" for 6 days
- Expected: No action (not yet 7 days)
- Item visible in Done column

**Test Case 3**: Item in "Done" for 7+ days (manual test)
- Expected: Item archived
- Item hidden from default view
- Can be shown with "Include archived" filter

### Workflow Impact

```
Issue completed, PR merged
    ↓
[Issue moves to "Done" column]
    ↓
[Day 1-6: Issue visible in Done]
    ↓
[Day 7: At midnight, automation runs]
    ↓
[Item archived automatically]
    ↓
[Done column stays clean, clutter-free]
```

---

## Additional Rules

### Rule 4: Auto-Update on PR Merge

```yaml
name: "Move to Done on PR merge"
trigger:
  event: pull_request
  action: closed
  merged: true
conditions:
  - linked_to_issue: true
actions:
  - move_to_column: "✅ Done"
  - update_field:
      field: "Status Detail"
      value: "Complete"
  - add_label: "merged"
  - add_label: "deployed"
```

### Rule 5: Auto-Add to Sprint

```yaml
name: "Auto-add Phase items to Sprint"
trigger:
  event: issue
  action: updated
  field_changed: "Phase"
conditions:
  - Phase: "Phase 1"
  - Sprint: (empty)
actions:
  - update_field:
      field: "Sprint"
      value: "Sprint 1"
  - update_field:
      field: "Milestone"
      value: "Foundation"
  - add_label: "{Phase}"
```

### Rule 6: Auto-Escalate Blockers

```yaml
name: "Escalate blocked items"
trigger:
  event: issue
  time_interval:
    every: "1 hour"
conditions:
  - Status Detail: "Blocked"
  - Time in Blocked: ">= 4 hours"
actions:
  - update_field:
      field: "Health Status"
      value: "Critical"
  - add_label: "escalate"
  - notify: "team-lead"
```

### Rule 7: Auto-Link Related Issues

```yaml
name: "Auto-link same component issues"
trigger:
  event: issue
  action: created
conditions:
  - Component: (not empty)
  - Phase: (not empty)
actions:
  - search: "Component={Component} AND Phase={Phase}"
  - suggest_linking: true
  - auto_add_dependency: (if user confirms)
```

---

## Automation Best Practices

### 1. Clear Naming Convention

```
✅ Good names:
  - "Auto-move PRs to In Review"
  - "Archive done items after 7 days"
  - "Escalate blockers"

❌ Bad names:
  - "Rule1"
  - "Auto"
  - "Stuff"
```

### 2. Avoid Conflicts

```
❌ Problematic setup:
  - Rule 1: "When column changes to Done → add label 'done'"
  - Rule 2: "When label 'done' added → move to Done"
  - Result: Infinite loop!

✅ Better approach:
  - Rule 1: "When column manually changes to Done → update Status Detail"
  - Rule 2: "When Status Detail changes to Complete → archive after 7 days"
```

### 3. Document Automation

Create a comment in each rule:
```
Purpose: Auto-archive old done items
Trigger: Item in Done for 7+ days
Action: Archive and label
Maintained by: Platform Team
```

### 4. Test Before Deploying

```
Testing checklist:
- [ ] Trigger conditions verified
- [ ] No loop risk
- [ ] Tested with real data
- [ ] Documented action
- [ ] Team informed
- [ ] Monitoring enabled
```

### 5. Monitor and Adjust

```
Weekly review:
  - [ ] Check automation runs
  - [ ] Review failed executions
  - [ ] Adjust thresholds if needed
  - [ ] Update documentation
```

---

## Automation Limits & Constraints

### GitHub Project Automation Limits

| Aspect | Limit | Note |
|--------|-------|------|
| Automation Rules | No limit | But performance may degrade |
| Actions per Rule | ~5-10 | Depends on API |
| Trigger Types | ~10 | PR, Issue, Label, Column, Date (limited) |
| Frequency | Manual trigger | No scheduled runs (use Actions) |
| Custom Logic | Limited | No if/else, only AND/OR conditions |

### Workarounds

| Need | Solution |
|------|----------|
| **Scheduled runs** | Use GitHub Actions `.yml` |
| **Complex logic** | Use GitHub Actions with script |
| **External systems** | Use webhooks + Actions |
| **Custom calculations** | Use GitHub API + custom script |

---

## Troubleshooting

### Common Issues

#### Issue: Automation not triggering

**Diagnosis**:
```
Check:
  1. Is automation enabled?
  2. Are trigger conditions exactly matched?
  3. Is field/label spelled correctly?
  4. Are there conflicting rules?
```

**Solution**:
```
1. Navigate to Project Settings → Automation
2. Verify rule is enabled (toggle on)
3. Check trigger conditions match exactly
4. Test with a fresh item
```

#### Issue: Automation creating loops

**Diagnosis**:
```
Check:
  1. Rule A triggers Rule B
  2. Rule B triggers Rule A
  3. Result: Infinite automation
```

**Solution**:
```
1. Disable one of the looping rules
2. Refactor to avoid circular logic
3. Use labels to prevent re-triggering
```

#### Issue: Automation moving items unexpectedly

**Diagnosis**:
```
Check:
  1. Multiple rules with similar conditions?
  2. Labels conflicting?
  3. Column changes interfering?
```

**Solution**:
```
1. Audit all active rules
2. Remove duplicate conditions
3. Use more specific filters
```

---

## Complete Automation Ruleset Example

### Production Automation Suite

```yaml
automations:
  - name: "PR Review Workflow"
    description: "Manage PR code review process"
    enabled: true
    rules:
      - "Auto-add PRs to project"
      - "Move to In Review on PR"
      - "Move to Done on merge"

  - name: "Work Status Management"
    description: "Track issue status changes"
    enabled: true
    rules:
      - "Auto-move on label"
      - "Update Status Detail on column change"
      - "Escalate blockers"

  - name: "Board Hygiene"
    description: "Keep board clean and current"
    enabled: true
    rules:
      - "Archive after 7 days"
      - "Remove duplicate items"
      - "Update timestamps"

  - name: "Metadata Management"
    description: "Auto-populate fields"
    enabled: true
    rules:
      - "Add Phase label"
      - "Set Sprint on Phase"
      - "Link dependencies"
```

---

**Last Updated**: 2024  
**Version**: 1.0  
**Maintained By**: Platform Team
