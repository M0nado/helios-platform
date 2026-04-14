# Team Setup Guide - HELIOS Platform v2

## 📋 Overview

This guide helps team leads, managers, and administrators set up and manage teams on the HELIOS Platform v2.

---

## 👔 For Team Leads

### Getting Started

#### 1. Understand Your Role
As a team lead, you will:
- Assign team members to work tracks
- Monitor progress on GitHub Project board
- Facilitate sprint planning
- Unblock team members when they hit obstacles
- Ensure quality standards are met
- Lead technical discussions

#### 2. Set Up Your Team

**Step 1: Add Team Members to Organization** (Admin only)
- Go to https://github.com/orgs/M0nado/settings/members
- Click "Add member"
- Invite team members by GitHub username
- Set role (write access minimum for developers)

**Step 2: Set Team Permissions**
- Assign developers to "Development" team
- Assign QA to "QA" team
- Assign leads to "Team Leads" team
- Ensure access to main branch

**Step 3: Configure Project Board Access**
- Go to GitHub Project: https://github.com/orgs/M0nado/projects/3
- Click Settings → Access
- Add team members with edit permissions

### How to Assign Team Members to Work Tracks

#### 1. Using GitHub Issues
1. Open issue on GitHub
2. Click "Assignees" → Select team member
3. Add labels for track (e.g., "track-infrastructure", "track-deployment")
4. Add to Project board (if not already there)

#### 2. Using GitHub Project Board
1. Go to https://github.com/orgs/M0nado/projects/3
2. Right-click on card → Assign
3. Select team member
4. Move to correct column (Backlog → In Progress → etc.)

#### 3. Track Categories (Use These Labels)
- **track-infrastructure** - System setup and CI/CD
- **track-deployment** - Deployment and release
- **track-features** - New functionality
- **track-documentation** - Docs and guides
- **track-testing** - QA and testing

### How to Use GitHub Project for Sprint Planning

#### 1. Set Up Sprint Structure
- Create a project view per sprint (e.g., "Sprint 1", "Sprint 2")
- Use milestone feature for sprint dates
- Group issues by track

#### 2. Plan Your Sprint
**Before Sprint Starts** (Monday morning):
1. Create new milestone for sprint
2. Create Project board view for sprint
3. Estimate story points (use labels: points-1, points-2, points-3, etc.)
4. Assign issues to team members
5. Update status in Project board (all in Backlog)

**Sprint Kickoff** (Monday 10 AM):
1. Review sprint goals
2. Discuss blockers
3. Confirm assignments
4. Set team capacity (e.g., 40 story points)

**During Sprint** (Daily):
1. Check Project board status
2. Move cards through workflow
3. Address blockers in standup
4. Update issue descriptions

**Sprint End** (Friday):
1. Review completed work
2. Mark done items in project
3. Evaluate team velocity
4. Plan retro discussion

#### 3. Project Board Workflow
```
Backlog → To Do → In Progress → Review → Done
  ↓        ↓          ↓          ↓       ↓
 New     Ready      Active    Testing  Complete
```

### How to Monitor Progress in Real-Time

#### Daily Dashboard Check
1. **Open GitHub Project**: https://github.com/orgs/M0nado/projects/3
2. **Check WIP (Work in Progress)**
   - Cards in "In Progress" column
   - Should match team capacity
   - Flag if anyone has >3 cards
3. **Check For Blockers**
   - Issues with "blocked" label
   - Comments with 🚫 or ⚠️
   - Resolve immediately

#### Weekly Status Report
**Every Friday:**
1. Count cards in each column
2. Calculate completed/total
3. Note blocker patterns
4. Share with stakeholders

#### Metrics to Track
- **Velocity**: Story points completed per sprint
- **WIP**: Cards currently in progress
- **Blockers**: Issues with "blocked" label
- **PR Review Time**: Time from PR creation to merge
- **Cycle Time**: Time from "To Do" to "Done"

### How to Handle Blockers

#### Step 1: Identify Blocker
Common blockers:
- "Documentation unclear"
- "Waiting for code review"
- "Infrastructure not ready"
- "Design not finalized"

#### Step 2: Add Label
Add `blocked` label to issue

#### Step 3: Document in Issue Comment
```
🚫 **BLOCKER**: [reason]

Blocked by:
- [Link to blocking issue/PR]

Action needed:
- [What will unblock]

Owner:
- @username (Who should fix)
```

#### Step 4: Follow Up
- Check every day until resolved
- Escalate if needed (manager/CTO)
- Update team in standup
- Remove label when unblocked

#### Step 5: Prevent Pattern
- Track recurring blockers
- Add automation (tests, pre-checks)
- Improve documentation
- Adjust workflow

---

## 💻 For Developers

### Getting Started

#### 1. Understand Your Role
As a developer, you will:
- Work on assigned issues
- Use GitHub Codespaces for development
- Submit pull requests
- Review teammates' code
- Help with documentation
- Communicate blockers

#### 2. Set Up Your Development Environment

**Option A: GitHub Codespaces (Easiest - No Setup)**
1. Go to https://github.com/codespaces
2. Click "Create codespace on main"
3. Wait for environment to load (~2-3 min)
4. Start developing immediately

**Option B: Local Setup**
1. Clone repository: `git clone https://github.com/M0nado/helios-platform.git`
2. Read [DEVELOPMENT.md](DEVELOPMENT.md) for setup instructions
3. Run setup script
4. Verify with `npm run build` or `python setup.py test`

### How to Claim Issues from GitHub Project

#### Step 1: Find Your Work
1. Go to GitHub Project: https://github.com/orgs/M0nado/projects/3
2. Look at "Backlog" or "To Do" column
3. Find issue tagged with your track or assigned to you
4. Read issue description carefully

#### Step 2: Claim the Issue
1. Click on issue card
2. Click "Assignees" → Select yourself
3. Add comment: "Taking this on!" 👍
4. Move card to "In Progress" column

#### Step 3: Start Work
1. Create feature branch: `git checkout -b issue-123-feature-name`
2. Make changes following code standards
3. Test locally
4. Commit with clear message: `git commit -m "fix: description of change"`

#### Step 4: Update Progress
- Update issue description with progress
- Post screenshots/demo links in comments
- Request help if needed
- Move card back to "To Do" if blocked

### How to Use GitHub Codespaces

#### Launch Codespace
1. Go to https://github.com/codespaces
2. Click "New codespace"
3. Select "M0nado/helios-platform"
4. Select "main" branch
5. Click "Create codespace"
6. Wait for environment to load

#### Codespaces Environment Includes
- ✅ Node.js (v18, v20)
- ✅ Python (3.10+)
- ✅ Git & GitHub CLI
- ✅ VS Code (or Web IDE)
- ✅ All dependencies pre-installed
- ✅ Build tools configured

#### Common Codespaces Commands
```bash
# Start development server
npm run dev          # Start local server
npm run build        # Build project
npm run test         # Run tests
npm run lint         # Check code style

# Work with Git
git status           # See changes
git add .            # Stage all changes
git commit -m "msg"  # Commit with message
git push             # Push to GitHub
```

#### Customize Your Codespace
- Install extensions in VS Code sidebar
- Adjust font size (Ctrl/Cmd + +/-)
- Install additional tools as needed
- Codespace saves automatically

### How to Run Phase Builds Locally

#### Phase 0: Foundation
```powershell
cd C:\Users\ADMIN\helios-platform\phases\0-foundation
# Read the README first
notepad README.md

# Run the phase
.\setup-phase-0.ps1
```

#### Phase 1: Security
```powershell
cd C:\Users\ADMIN\helios-platform\phases\1-security
notepad README.md
.\setup-phase-1.ps1
```

#### Phase 2: Optimization
```powershell
cd C:\Users\ADMIN\helios-platform\phases\2-optimization
notepad README.md
.\setup-phase-2.ps1
```

#### Phase 3: Capability
```powershell
cd C:\Users\ADMIN\helios-platform\phases\3-capability
notepad README.md
.\setup-phase-3.ps1
```

#### Build Variants
```bash
# Development build
npm run build:dev

# Staging build
npm run build:staging

# Production build
npm run build:prod

# Test build (with coverage)
npm run build:test
```

### How to Submit Pull Requests

#### Step 1: Prepare Your Branch
```bash
# Update from main
git fetch origin
git rebase origin/main

# Run tests locally
npm run test

# Build project
npm run build
```

#### Step 2: Push Your Branch
```bash
git push origin issue-123-feature-name
```

#### Step 3: Create Pull Request
1. Go to GitHub repository
2. Click "Pull Requests" → "New Pull Request"
3. Select your branch
4. Add title: `Fixes #123: Clear description`
5. Fill description using template:
   ```markdown
   ## Description
   [What did you change and why?]

   ## Type
   - [ ] Bug fix
   - [ ] Feature
   - [ ] Documentation
   - [ ] Performance improvement

   ## Testing
   [How did you test this?]

   ## Checklist
   - [ ] Code follows style guide
   - [ ] All tests pass
   - [ ] Documentation updated
   - [ ] No console errors
   ```
6. Click "Create Pull Request"

#### Step 4: Address Feedback
1. Wait for code review
2. Make requested changes
3. Push additional commits
4. Request re-review when ready

#### Step 5: Merge
Once approved:
1. Click "Squash and merge" (or "Rebase and merge")
2. Confirm merge
3. Delete branch
4. Move issue to "Done" in Project board

---

## 👨‍💼 For Managers

### Project Status Dashboard

#### Weekly Metrics Report
**Every Friday at 4 PM:**
1. Open GitHub Project board
2. Document metrics:
   - Total issues: Count in Project
   - In Progress: Count in "In Progress" column
   - Completed this week: Count "Done" column
   - Velocity: Story points completed
   - Blocked issues: Count with "blocked" label

#### Sample Report Format
```
HELIOS v2 Weekly Report - Week 12

📊 Metrics:
- Total Issues: 47
- In Progress: 8
- Completed this week: 12
- Velocity: 35 story points
- Blocked: 2

✅ Completed:
- Issue #5: Feature X
- Issue #8: Bug fix Y
- Issue #12: Documentation Z

🚫 Blockers:
- Issue #22: Waiting for design (Owner: @alice)
- Issue #35: Infrastructure not ready (Owner: @bob)

⚠️ Risks:
- Team capacity near limit
- Design phase is 1 week behind

📈 Trends:
- Velocity stable
- Blocker rate increasing
- Review time: avg 6 hours
```

### Team Velocity Metrics

#### Calculate Velocity
1. Count story points in "Done" column for sprint
2. Track over multiple sprints
3. Use for capacity planning

**Example:**
- Sprint 1: 32 points
- Sprint 2: 35 points
- Sprint 3: 28 points (1 person out)
- Average: 31.7 points

#### Use for Planning
- Estimate points needed: Divide by velocity
- Plan realistic sprints: Don't exceed velocity
- Track improvements: Velocity should increase with process improvements

### Deployment Timeline

#### Standard Release Process
1. **Monday**: Sprint starts, backlog refinement
2. **Wednesday**: Mid-sprint check-in
3. **Friday**: Sprint ends, retro & review
4. **Next Monday**: Staging deployment
5. **Wednesday**: Production deployment

#### Deployment Checklist
- [ ] All tests passing
- [ ] Code reviewed and approved
- [ ] Documentation updated
- [ ] Release notes prepared
- [ ] Stakeholders notified
- [ ] Rollback plan ready
- [ ] Team available for issues

### Risk Assessment

#### Monthly Risk Review
**First Friday of month:**

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Team member departure | Medium | High | Cross-training, documentation |
| Scope creep | High | Medium | Strong backlog grooming |
| Technical debt | High | High | Schedule tech debt sprints |
| Integration issues | Medium | High | Continuous testing |

#### Escalation Matrix
- **Green** (No blocker): Continue as planned
- **Yellow** (1-2 blockers): Daily monitoring, escalation consideration
- **Red** (3+ blockers or critical): Immediate escalation to CTO

---

## 🔧 Common Team Setups

### Small Team (3-5 people)
- 1 Team Lead (part-time)
- 2-4 Developers
- Shared QA responsibilities
- Weekly sprints

### Medium Team (6-10 people)
- 1-2 Team Leads
- 4-6 Developers
- 1-2 QA Engineers
- Bi-weekly sprints

### Large Team (10+ people)
- 2-3 Team Leads
- 7-9 Developers
- 2-3 QA Engineers
- Multiple tracks
- Agile scrum master
- Weekly sprints with multiple work tracks

---

## 📞 Getting Help

### For Team Leads
- GitHub Project navigation: See [GITHUB_INFRASTRUCTURE.md](GITHUB_INFRASTRUCTURE.md)
- Project management tips: See [PROJECT_STATUS_DASHBOARD.md](PROJECT_STATUS_DASHBOARD.md)

### For Developers
- Development setup: See [DEVELOPMENT.md](DEVELOPMENT.md)
- Troubleshooting: See [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- Codespaces help: See [CODESPACES_READINESS.md](CODESPACES_READINESS.md)

### For Managers
- Architecture overview: See [MODULAR_ARCHITECTURE.md](MODULAR_ARCHITECTURE.md)
- Complete guide: See [PLATFORM_COMPREHENSIVE_OVERVIEW.md](PLATFORM_COMPREHENSIVE_OVERVIEW.md)

---

## 🎯 Success Metrics

**Team is functioning well when:**
- ✅ All team members understand their assignments
- ✅ Issues move through workflow smoothly
- ✅ Blockers are resolved within 24 hours
- ✅ PRs are reviewed within 6 hours
- ✅ Code quality is consistent
- ✅ Documentation is up-to-date
- ✅ Team communication is clear
- ✅ Velocity is stable or improving

---

**Last Updated**: April 2026  
**Status**: ✅ Production Ready  
**Questions?** Create a GitHub issue or contact team lead
