# AI: start here

This is the shared entry point for any AI working in this repository. It is ordinary checked-in
documentation, not a plugin or installable skill. AIs that do not discover AGENTS.md automatically
should be explicitly directed to this file.

## Understand the owner's goal

Keep the actual code, tools, applications, scripts and automations created with AI assistance
in one Git repository. Give each real project a clear folder and enough supporting context for
another AI or person to maintain it, share its source, or rebuild it on a fresh computer.
The user values stability and continuity. Preserve working behavior outside the requested change.
Do not ask the user to repeat facts that the repository or available local evidence can establish.

This is not a general conversation archive. Do not create a project for every troubleshooting chat,
research question, memory import or recommendation. Existing imported history/research folders
are intentionally left in place; do not remove or expand them as routine housekeeping.

## Read in this order

1. Root [AGENTS.md](AGENTS.md), this file, and [PROJECT_INDEX.md](PROJECT_INDEX.md).
2. The relevant project's README.md, AGENTS.md and linked context/source guides.
3. [Project standard](docs/ai/PROJECT-STANDARD.md) when adding or organizing code.
4. [REBUILD.md](REBUILD.md) for a fresh-PC setup or migration.
5. [SHARING.md](SHARING.md) for source sharing and [SYNC.md](SYNC.md) for remote backup.

Read only the projects and local evidence relevant to the current task. A file's presence is not
proof that its contents are installed, current, tested, or appropriate for the present hardware.

## Establish the working context independently

- Confirm the actual repository root, current branch, working-tree changes and relevant source.
- Identify which machine is executing the work: desktop host, handheld, or a fresh PC.
- Distinguish repository source, installed deployment, historical snapshots and private evidence.
- Discover current app/monitor/device IDs and paths when needed; do not assume another PC matches.
- Read the recorded status and unresolved items before treating a project as complete.
- State meaningful uncertainty; ask only for decisions or facts that inspection cannot resolve.

The original checkout is C:/Github/AI-Projects and the remote is
https://github.com/So1idoSnake/AI-Projects.git. These are context, not hardcoded requirements
for another machine. Local Codex memory is not the project's source of truth: files in this
repository carry the durable project context across tools and devices.

## Work and commit rules

Work only on a non-main branch, ordinarily projects/<topic> or backup/<snapshot>.
If currently on main/master, create a suitable branch before writing. If already on a relevant
non-main branch, continue there. Do not overwrite other work, auto-stash it, reset it, or commit
unrelated edits. Do not switch branches while another task has changes in progress.

Keep changes within the user's requested scope. Inspection and documentation requests do not
authorize deployment, driver/firmware installation, startup changes, service restarts or save edits.
When implementation is requested, make and verify the change in proportion to its impact.
Commit the task's reviewed code and documentation on its non-main branch. A local commit is not
a GitHub backup; claim remote sync only after verifying the matching remote commit.
Never merge into or push to main/master without a new explicit instruction.
Use the existing sync rules rather than creating duplicate schedules or force-pushing.

## What belongs in a project

Keep source and the code used to produce the work: build scripts, setup scripts, tests where useful,
configuration examples, asset-generation code, and required source assets. Executables are
supplemental snapshots, not a substitute for application source.
Use the [project standard](docs/ai/PROJECT-STANDARD.md) as a flexible template. Reuse an existing
project for related work instead of creating duplicates. Add useful instructions for both a
maintainer resuming work and a person starting from a blank computer.

Separate first-party code from third-party components. Preserve attribution and licenses.
If source was not recovered or the work was performed through a UI, say so. Do not invent a
reusable generator or imply a newly reconstructed script is the original implementation.

## Documentation that helps the next person

Keep the README accurate about purpose, source entry points, prerequisites, setup/build/run commands,
configuration, verification, known issues and deployment boundaries. Record the decision and reason
when it affects future work; avoid transcript-style summaries. Document machine-specific assumptions
and explain what a fresh machine must rediscover. Label planned features and historical findings.
Update root project navigation when adding a real project; keep detailed context inside that project.
Use relative links for repository files and official/upstream links for dependencies when available.

## Privacy and sharing

Never commit credentials, tokens, cookies, pairing databases, personal exports, account history,
game saves, raw crash dumps or raw diagnostic logs. Put explicitly requested private backups in
ignored local-private/ and verify they are ignored before staging. Ignored files are local-only;
they need a separate private backup if the user wants them protected from disk loss.
Do not publish third-party downloads merely because they are present on disk.

## Finish each task with evidence

Confirm the intended files changed, source/config are preserved as needed, relevant checks passed,
private data is excluded, and the commit is on a non-main branch. Update project status and pending
work. Report the outcome, branch/commit, verification limits, and whether sync actually succeeded.
Do not run unrelated live experiments just to make a documentation task appear tested.

## Current repository landmarks

- Display-Switcher: actual C# application source, build scripts, icon tooling and binary snapshots.
- Windows-App-Startup: reusable shortcut setup plus clearly marked superseded launchers.
- Sunshine-Streaming: streaming context/configuration; switching implementation lives in Display-Switcher.
- PROJECT_INDEX.md: all imported folders and provenance, including retained non-code history.
- SYNC.md: local schedule details and authentication status; do not assume it works on a fresh PC.
