# 🌟 HELIOS PLATFORM - GITHUB FEATURES CHEAT SHEET

**Quick Reference for Using HELIOS on GitHub**

---

## 📍 QUICK LINKS (Copy & Use)

```
Main Repository:
https://github.com/M0nado/helios-platform

Projects Board:
https://github.com/M0nado/helios-platform/projects

Actions Workflows:
https://github.com/M0nado/helios-platform/actions

Create Codespace:
https://github.com/codespaces/new?repo=M0nado/helios-platform

Discussions:
https://github.com/M0nado/helios-platform/discussions

Issues:
https://github.com/M0nado/helios-platform/issues

Security Advisories:
https://github.com/M0nado/helios-platform/security
```

---

## 🖥️ GITHUB WEBSITE FEATURES

### Keyboard Shortcuts (Press "?" on any page)

```
"/" - Open search
"g c" - Go to Code
"g i" - Go to Issues
"g p" - Go to Projects
"g a" - Go to Actions
"g d" - Go to Discussions
"." - Open in web editor (VS Code)
"t" - File search
```

### Search Operators

```
# Find code
filename:*.ps1 applock

# Find issues
is:issue is:open label:bug

# Find PRs
is:pr is:merged author:@me

# Find commits
is:commit message:"initial setup"

# Find discussions
is:discussion category:help
```

### Useful Views

**See Repository Statistics**
```
Repository Home → Insights tab
├─ Code frequency
├─ Network graph
├─ Contributors
├─ Community health
└─ Dependency graph
```

**See Pulse (Activity)**
```
Repository Home → Pulse tab
├─ Recent commits
├─ PRs/Issues opened
├─ Authors
├─ Top contributors
└─ Active dates
```

**Browse File History**
```
Click any file → Click "History"
See:
├─ All commits affecting file
├─ Who changed what
├─ When changes made
├─ Commit messages
└─ Diff of changes
```

---

## 📊 GITHUB PROJECTS (Task Tracking)

### 4 Ways to View Project

**1. Board View (Kanban)**
```
https://github.com/M0nado/helios-platform/projects/1
Columns:
├─ Backlog (todo)
├─ In Progress (working)
├─ In Review (testing)
├─ Done (complete)

Drag cards between columns to update status
```

**2. Table View (Spreadsheet)**
```
Click "Table" tab at top
See all fields:
├─ Title
├─ Status
├─ Assignee
├─ Priority
├─ Milestone
├─ Date
└─ Custom fields

Sort/filter by any column
```

**3. Roadmap View (Timeline)**
```
Click "Roadmap" tab
See:
├─ Start dates
├─ Due dates
├─ Progress bars
├─ Milestones
└─ Timeline dependencies
```

**4. Backlog View (Queue)**
```
Click "Backlog" tab
See all unstarted items
├─ Prioritize by dragging
├─ Assign priority labels
├─ View dependencies
└─ Batch operations
```

### Quick Actions in Projects

**Change Status**
```
Click card → Select new status
Or drag card to different column
Status automatically updates
```

**Add Assignee**
```
Click card → Type assignee name
Or click assignee field
Can assign multiple people
```

**Set Priority**
```
Click card → Add label
Choose:
├─ Critical (red)
├─ High (orange)
├─ Medium (yellow)
└─ Low (green)
```

**Set Deadline**
```
Click card → Set date
Shows in timeline view
Sends reminder notifications
```

**Add Notes**
```
Click card → Type in description
Add context/details
@ mention people for discussions
```

---

## ⚙️ GITHUB ACTIONS (CI/CD)

### Monitor Workflow Status

**See Status Badges**
```
Look at README.md in repository:
├─ 🟢 Green badge = passing
├─ 🟡 Yellow badge = running
└─ 🔴 Red badge = failed

Click badge to see details
```

**View Action Details**
```
Actions tab → Click workflow name
See:
├─ All recent runs
├─ Success/failure status
├─ Duration
├─ Trigger type (push/schedule/manual)
└─ Commit info
```

**View Logs**
```
Click specific run → Click job → Click step
See:
├─ Full output
├─ Errors highlighted
├─ Timing info
└─ Expandable sections
```

### Manually Trigger Workflows

**Run Tests on Demand**
```
Actions tab → Select workflow
Click "Run workflow"
Optionally specify:
├─ Branch to run on
├─ Custom parameters
└─ Environment
```

**Re-run Failed Jobs**
```
When workflow fails:
1. Open failed run
2. Click "Re-run failed jobs"
3. Only failed steps re-run
4. Saves time vs full re-run
```

### Understand Workflow Results

**Green Checkmark** ✅
```
All tests passed
├─ Code is good to merge
├─ No security issues
├─ Performance acceptable
└─ All checks passing
```

**Red X** ❌
```
Tests failed - fix needed
├─ Click to see which step failed
├─ Read error message
├─ Fix code
├─ Push to re-run
```

**Yellow Dot** 🟡
```
Tests running
├─ In progress
├─ Check back shortly
├─ Or click to watch live
```

---

## 💻 GITHUB CODESPACES

### Start Coding in 30 Seconds

**From GitHub Website**
```
1. Open repository
2. Click green "Code" button
3. Click "Codespaces" tab
4. Click "Create codespace on main"
5. Wait for startup (30-60 sec)
6. VS Code opens in browser
```

**From Command Line**
```
gh codespace create -r M0nado/helios-platform
gh codespace ssh
```

**From VS Code Desktop**
```
1. Install "GitHub Codespaces" extension
2. Open VS Code
3. Remote Explorer → Create New Codespace
4. Select repository
5. Wait for startup
```

### Common Codespace Commands

```
# Terminal in Codespace (Ctrl+`)

# Install dependencies
npm install

# Run tests
npm test

# Build project
npm run build

# Start development server
npm run dev

# View current branch
git branch

# Make a commit
git add .
git commit -m "message"

# Push changes
git push

# Create new branch
git checkout -b feature/name
```

### Share Codespace

**Live Share (Pair Programming)**
```
1. Click Live Share icon (bottom)
2. Click "Share"
3. Copy invite link
4. Send to teammate
5. They join in real-time
6. Edit same files together
```

**Share Read-Only**
```
1. Live Share → Share (readonly)
2. Send link
3. They can see but not edit
```

---

## 💬 GITHUB DISCUSSIONS

### Find Answers

**Search Existing Discussions**
```
Discussions tab → Search
Look for:
├─ "Getting Started Help"
├─ "Deployment Questions"
├─ "Customization"
└─ "How-to Guides"

View answers from community
```

**Ask Question**
```
Discussions tab → New discussion
1. Choose category (Help/Idea/Q&A)
2. Type title (clear & specific)
3. Describe problem
4. Add code if relevant
5. Wait for responses
```

**Upvote Useful Answers**
```
See answer you like:
1. Click up arrow 👆
2. Answer gets promoted
3. Appears higher in sort
```

### Best Practices

```
GOOD QUESTION:
"How do I run Phase 3 with custom tools?"

BAD QUESTION:
"It's not working"

GOOD TITLE:
"Error when running build with Xbox tools"

BAD TITLE:
"Help!"
```

---

## 🐛 GITHUB ISSUES

### Report Bug

```
Issues tab → New Issue
Select "Bug report" template

Title: Clear description
Example: "Phase 2 fails on Windows Server 2022"

Description:
├─ What happened
├─ What you expected
├─ Steps to reproduce
└─ Error message

Attach:
├─ Screenshot
├─ Error log
└─ Environment info
```

### Request Feature

```
Issues tab → New Issue
Select "Feature request" template

Title: Short description
Example: "Add support for WSL2"

Description:
├─ Problem it solves
├─ Suggested solution
├─ Why it matters
└─ Examples of similar features
```

### Check Status

```
Issues tab → Open issue
See:
├─ Status (Open/Closed)
├─ Assignee (who's working on it)
├─ Labels (type, priority)
├─ Milestone (target release)
└─ Comments from team
```

### Get Notified

```
Watch → Click notification icon
Options:
├─ All activity (every change)
├─ Participating (when you comment)
├─ Mentions (when @ mentioned)
└─ Not watching (no notifications)
```

---

## 🔒 GITHUB SECURITY

### Check Security Status

```
Settings → Code security → Security summary
See:
├─ Secret scanning results
├─ Dependency alerts
├─ Code scanning findings
├─ Security advisories
```

**Fix Security Issues**
```
1. Read detailed description
2. Understand the vulnerability
3. Click "Fix" button or
4. Review suggested PR
5. Merge PR to fix
```

### Enable Security Features

```
Settings → Code security & analysis
Toggle on:
├─ ✅ Dependency alerts
├─ ✅ Dependabot security updates
├─ ✅ Secret scanning
├─ ✅ Push protection
└─ ✅ Code scanning
```

---

## 🔗 INTEGRATION WORKFLOW

### Complete Feature Development

```
STEP 1: Create Issue
└─ Issues tab → Report bug or feature
   └─ Gets assigned a number (#123)

STEP 2: Create Branch
└─ Clone repo
└─ git checkout -b fix/issue-123

STEP 3: Open Codespace
└─ Code → Codespaces → Create
└─ Work in cloud environment

STEP 4: Make Changes
└─ Edit files in Codespace
└─ Run tests locally
└─ Commit changes

STEP 5: Push & Create PR
└─ git push origin fix/issue-123
└─ Click notification on GitHub
└─ Create Pull Request

STEP 6: Automated Checks
└─ Actions run automatically
└─ Tests execute
└─ Code quality checked
└─ Results posted on PR

STEP 7: Code Review
└─ Team reviews code
└─ Comments on changes
└─ Request changes if needed
└─ Approve when ready

STEP 8: Merge
└─ Click "Merge PR"
└─ Branch auto-deleted
└─ Actions deploy automatically
└─ Issue auto-closed

STEP 9: Monitor
└─ Actions tab shows deployment
└─ Verify on production
└─ Check monitoring dashboard
```

---

## 📱 GITHUB MOBILE APP

### Download & Setup

```
iOS: App Store → GitHub
Android: Google Play → GitHub

1. Download app
2. Login with GitHub account
3. Select M0nado/helios-platform
4. Get notifications on phone
```

### Use Cases

**Review PRs on the Go**
```
Notifications → Click PR
├─ See changes
├─ Read comments
├─ Approve or request changes
└─ Works offline
```

**Check Workflow Status**
```
Open app → Actions
├─ See latest runs
├─ Check pass/fail
├─ View logs (in browser)
```

**Respond to Issues**
```
Open app → Issues
├─ Read new issues
├─ Reply to comments
├─ Close/reopen as needed
```

---

## 🎯 GITHUB TIPS & TRICKS

### Productivity Tips

**1. Use GitHub CLI**
```
# Install GitHub CLI
brew install gh  # Mac
choco install gh # Windows

# Clone repo
gh repo clone M0nado/helios-platform

# Create issue
gh issue create --title "Bug..." --body "..."

# Create PR
gh pr create --title "Feature..." --body "..."

# Merge PR
gh pr merge 123 --auto --squash
```

**2. Create Custom Shortcuts**
```
# Go to repository → Code → URL bar
# Replace owner/repo in URL:
github.com/M0nado/helios-platform/...

# Bookmark common paths:
- /projects (Projects board)
- /actions (Workflows)
- /issues (Issue tracker)
- /discussions (Community)
- /security (Security status)
```

**3. Use GitHub Web Editor**
```
Press "." anywhere in repository
Opens VS Code in browser
Perfect for quick edits
No need for local setup
```

**4. GitHub Read Mode**
```
Press "r" on any file
Simplified reading view
No editing tools
Great for documentation
```

### Collaboration Tips

**1. Use Issue Templates**
```
When creating issue, template auto-appears
Fill in all sections
Makes issues better
Easier to understand
```

**2. Use PR Templates**
```
When creating PR, template auto-appears
Ensures all info included
Links to related issues
Mentions reviewers
```

**3. Use Branch Protection**
```
Settings → Branches → Add rule
Require:
├─ PR reviews
├─ Status checks passing
├─ Up-to-date branches
└─ Signed commits
```

**4. Use CODEOWNERS File**
```
Create .github/CODEOWNERS
Example:
/scripts/security/ @security-team
/docs/ @docs-team
/tests/ @qa-team
```

---

## 🚨 TROUBLESHOOTING GITHUB FEATURES

### Codespaces Won't Start
```
1. Delete existing Codespace
   Settings → Codespaces → Delete
2. Create new one
3. If still fails, check system status
   github.com/status
```

### Actions Workflow Not Running
```
1. Check if Actions enabled
   Settings → Actions → Enable
2. Check if workflow YAML is correct
3. Verify trigger conditions
4. Check branch protection rules
```

### PR Can't Merge
```
Check for:
├─ ✅ Status checks passing (green)
├─ ✅ PR approved by reviewer
├─ ✅ Branch up to date with main
├─ ✅ No conflicts
└─ ✅ Admin approval (if required)

Fix issues then try again
```

### Can't Clone Repository
```
Check:
1. Have Git installed?
   git --version
2. Have GitHub CLI?
   gh --version
3. Are you authenticated?
   gh auth status
4. Try SSH instead of HTTPS
```

---

## 📞 SUPPORT & HELP

### Get Help Fast

```
1. Discussions Tab
   ├─ Search existing discussions
   ├─ Ask new question
   └─ Browse community answers

2. Issues Tab
   ├─ Search issues
   ├─ Report bug with template
   └─ Track resolution

3. Documentation
   ├─ /docs folder
   ├─ README.md
   └─ TROUBLESHOOTING.md

4. Email Support
   support@helios-platform.dev
```

### Share Your Feedback

```
GitHub Issues → "Feature request" template
or
GitHub Discussions → "Ideas" category

Your feedback helps improve HELIOS!
```

---

## 🎓 LEARN MORE

### GitHub Learning Resources

```
https://docs.github.com - Official docs
https://github.com/skills - Interactive tutorials
https://lab.github.com - Free courses
```

### HELIOS Documentation

```
README.md - Start here
docs/QUICK_START.md - 5-minute setup
docs/ARCHITECTURE.md - System design
docs/TROUBLESHOOTING.md - Common issues
GITHUB_INTEGRATION_COMPLETE_GUIDE.md - This guide
```

---

## ✅ QUICK CHECKLIST

Before Starting:
- [ ] Bookmarked repository link
- [ ] Starred repository (show support!)
- [ ] Watched repository (get notifications)
- [ ] Read README.md
- [ ] Joined Discussions

First Week:
- [ ] Explored Projects board
- [ ] Opened repository in Codespaces
- [ ] Reviewed documentation
- [ ] Ran Phase 1 build
- [ ] Checked monitoring dashboard

Ongoing:
- [ ] Check Actions status
- [ ] Monitor Phase progress
- [ ] Participate in Discussions
- [ ] Report issues found
- [ ] Suggest improvements

---

## 🎉 YOU'RE READY!

Everything you need to use HELIOS on GitHub is now available:

✅ GitHub Website - Browse & manage
✅ GitHub Projects - Track progress
✅ GitHub Actions - Automated tests & deploys
✅ GitHub Codespaces - Cloud development
✅ GitHub Discussions - Community help
✅ GitHub Issues - Bug tracking
✅ GitHub Marketplace - Extensions & tools

**Start exploring now:** https://github.com/M0nado/helios-platform

---

**Last Updated:** 2026-04-13
**Version:** 1.0
**Status:** Ready for Production Use ✅
