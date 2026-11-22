# AutoHDRForceTouch

Read ../AGENTS.md and README.md. Source is in src/, tests in tests/, and build.ps1
builds the portable WinForms application. Keep the picker-first touch workflow.
Preserve backup-before-write and verified per-application HKCU behavior; never
change global HDR, launch a selected game, or request elevation implicitly.
Building is separate from launching the GUI or testing production registry keys.
Use -SkipTests for compilation without executing the isolated registry tests.
