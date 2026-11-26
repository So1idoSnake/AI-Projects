# Historical StreamLight rollback scripts

These are original scripts written during October 31 handheld configuration.
They are source backups, not current reset or setup tools. Both require
StreamLight to be closed and modify its current user's registry settings.

- `Restore-Settings.ps1`: removes selected Docked overrides and restores four
  historical global values. It predates the successful 4K120 retry and can undo
  later desired changes.
- `Restore-Portable.ps1`: removes selected Portable overrides and restores its
  historical 116 FPS value. It is not the final 1080p120 profile.

Neither script needs an old registry-export file, but neither is a complete
rollback of all later changes. Review every value and create a new backup before
deliberately using one. Full registry exports and logs stay on the original
machine and are not included, because they may contain pairing/session data.
