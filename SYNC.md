# Periodic GitHub sync

Configured on 2026-11-20 as the local app automation **Sync AI project branches**
(automation ID: sync-ai-project-branches), daily at 7 PM America/New_York.

Repository: C:/Github/AI-Projects.
Remote: https://github.com/So1idoSnake/AI-Projects.git.

Only committed work on backup/* and projects/* branches is eligible for ordinary
same-name, fast-forward pushes. The automation must not change main/master, switch
the current checkout, force-push, merge/rebase, or commit unreviewed working edits.
Uncommitted changes are reported for a deliberate commit. All local-private folders
and excluded personal data remain local and require a separate private backup.

Git runs noninteractively with GIT_TERMINAL_PROMPT=0, GCM_INTERACTIVE=never, and
credential.interactive=never. Missing authentication is reported rather than
opening a sign-in dialog. Network attempts should be time-bounded.

## Current limitation

The first interactive push was cancelled after its credential helper stalled.
A noninteractive retry confirmed that usable command-line credentials were absent.
The initial branches have NOT been confirmed on GitHub. Their commits are local.
Configure a supported GitHub authentication method later and rerun the sync.
Never paste or commit passwords, tokens, cookies or credential-store contents.

The schedule is stored by the app, not installed by cloning this repository.
On a new computer, configure authentication and recreate the daily automation
with the rules above. Then verify the remote branch commit hashes after a push.
