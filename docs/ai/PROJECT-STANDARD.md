# Project structure and README standard

Use this as a lightweight checklist, not a requirement to create empty directories or a skill.
Create one project folder per actual tool, application, automation, or code-backed workflow. Prefer an existing folder for related
changes. Do not convert each conversation into a project. Preserve existing organization where useful.

## Suggested layout

```text
Project-Name/
  README.md          # purpose, setup, build/run, verification and status
  AGENTS.md          # concise project-specific constraints and navigation
  src/               # actual application/library source, when applicable
  scripts/           # setup, build, migration or operational scripts
  config/            # safe examples/templates; no live credentials
  assets/            # required original assets and generation inputs
  tests/             # meaningful automated checks, when warranted
  docs/              # architecture, source guide, decisions or detailed runbook
  backups/           # deliberate dated snapshots with restore limits
  local-private/     # ignored personal evidence/backups, only when requested
```

Only create what the project needs. Existing root build.ps1 or tools/ layouts can remain.
Generated outputs, caches and normal runtime logs should be ignored. Document any deliberate
tracked binary snapshot, its source/version and restore limitations.

## README template

Adapt the following headings; omit sections that add no useful information.

```markdown
# Project name

What it does, why it exists and its current status.

## Source and structure

Identify the actual source entry points, build/setup code and important folders.
Distinguish an original implementation from historical code or a later adaptation.

## Prerequisites

OS/platform, runtimes/compiler, dependencies, driver/hardware requirements and official links.
State known tested versions as dated evidence; distinguish them from genuine version requirements.

## Fresh-machine setup

Install prerequisites, discover machine-specific IDs/paths, configure safe templates,
build the project and describe the separate deployment/registration step.
Use configurable values or clear placeholders instead of original-machine account/device IDs.

## Build and run

Exact commands, working directory, expected outputs and any relevant side effects.
State required privileges where known. A setup script should preview or support WhatIf when useful.

## Configuration and integration

Document configuration locations, ownership, startup/service dependencies and connected projects.
Explain which settings are source examples versus the installed machine's live state.

## Verification

Meaningful checks and what they establish. Separate syntax/build checks, simulated tests,
historical observations and real device/application verification. Do not claim tests not run.

## Deployment, backup and rollback

Explain what is installed where, how to preserve the existing state, and what can be restored.
Treat logs/state/account databases separately from source and executable rollback.

## Known limitations and next work

List unresolved issues, planned-but-unimplemented features and device-specific assumptions.

## Context and provenance

Concise relevant decisions, original code locations and upstream dependencies/attribution.
Use a conversation ID only when it helps recover missing context; do not summarize every chat.
```

## AGENTS.md contents

Link to the root playbook and project README. Add only constraints specific to this project:
source/build entry points, deployment boundary, behavior that must be preserved, and relevant
checks or high-impact operations. Do not duplicate long runbooks or paste personal memory.

## When importing work from another machine

Identify the existing source before copying anything. Preserve scripts used to create the output,
not only the final executable. Verify copies by hashes when appropriate. Do not relocate a running
deployment without updating its callers as an explicitly requested implementation step.
Prefer documenting the original installation separately from reusable setup. Do not move secrets,
personal exports or unreviewed vendor binaries into the shareable source tree.

## Before the non-main-branch commit

Review changed paths and private-data exclusions. Check relative links and exact command examples.
Perform only relevant validation. Update the root navigation if this is a new code project, and
record pending work without implying it shipped. Preserve unrelated user changes.
