# AI app configuration troubleshooting

Historical Codex desktop configuration case: computer_use.windows.always_allowed_app_ids had
conflicting documentation/runtime support, and features.thread_tools was an obsolete session flag.
Earlier advice alternated between table, array and removal; do not turn all of it into a setup recipe.

The last reviewed follow-up reported Computer Use and Chrome connectivity working without further
file/settings changes. That does not establish today's configuration schema or imply every old
config snippet should be restored. No standalone application code was created for this case.

## Fresh-PC use

Install the current app and plugins, inspect its accepted schema and current official docs, and
configure permissions through supported app controls. Preserve unrelated user settings. Do not
copy auth files, entire personal config.toml, old approval lists or stale feature flags into Git.
Private recovered session code is historical context only. This import changed no app configuration.
