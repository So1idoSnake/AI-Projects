# Launch Windows Apps at Sign-In with Codex

This guide reproduces the startup method that worked reliably on the original PC. It uses normal Windows Startup-folder shortcuts—no persistent PowerShell, batch, VBS, registry launcher, or scheduled task.

Windows controls the exact launch time and order. Applications can appear a little after sign-in; reliability is the priority here, not an exact delay.

## Prompt to give Codex on the other PC

Replace the bracketed list with the applications you want:

```text
Configure these Windows applications to open at sign-in: [APP NAMES].

Use script-free shortcuts in my per-user Windows Startup folder. First inventory the relevant existing Startup-folder items, HKCU Run values, scheduled tasks, and any launcher files from earlier attempts. Show or log the exact matching items, then remove only obsolete entries that clearly belong to these applications or earlier attempts to launch them. Do not touch unrelated startup items.

Resolve each installed application's current Windows app ID with Get-StartApps. For a packaged application, create a normal .lnk in [Environment]::GetFolderPath('Startup') that targets C:\Windows\explorer.exe and passes shell:AppsFolder\<APP_ID> as its argument. For a traditional desktop application, use its existing Start-menu shortcut or executable instead.

Test each new shortcut directly, waiting three seconds between applications, without closing applications that are already running. Finally verify the shortcuts with Get-CimInstance Win32_StartupCommand and confirm that the obsolete mechanisms are gone. Tell me which items were added and removed. Do not restart the PC; I will test the next sign-in myself.
```

## What Codex should do

### 1. Inventory before changing anything

Codex should inspect:

- The current user's Startup folder: `[Environment]::GetFolderPath('Startup')`
- Matching values in `HKCU:\Software\Microsoft\Windows\CurrentVersion\Run`
- Matching scheduled tasks
- Launcher files created by earlier attempts
- Installed application names and IDs from `Get-StartApps`

Only entries clearly associated with the requested applications should be changed. Existing unrelated startup applications must remain untouched.

### 2. Resolve the application ID

For Microsoft Store/MSIX applications, run:

```powershell
Get-StartApps |
    Where-Object Name -Match 'APP NAME' |
    Format-Table Name, AppID -AutoSize
```

A packaged-app ID commonly resembles:

```text
PackageFamilyName!ApplicationName
```

The ID is preferable to a versioned path under `C:\Program Files\WindowsApps` because the ID remains stable when the application updates.

### 3. Create a script-free Startup shortcut

The following is an example for one packaged application. It is a one-time setup command; the resulting startup item is an ordinary `.lnk`, not a script.

```powershell
$startupFolder = [Environment]::GetFolderPath('Startup')
$shortcutPath = Join-Path $startupFolder 'APP NAME.lnk'
$appId = 'RESOLVED_APP_ID'

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $env:SystemRoot 'explorer.exe'
$shortcut.Arguments = 'shell:AppsFolder\' + $appId
$shortcut.WorkingDirectory = $env:SystemRoot
$shortcut.Description = 'Launch APP NAME at Windows sign-in'
$shortcut.Save()
```

For a traditional Win32 program, Codex should prefer copying its existing Start-menu shortcut into the Startup folder. If that is unavailable, it can create a shortcut whose target is the program's installed executable.

### 4. Test without rebooting

Codex should launch each newly created shortcut directly. If several applications are being configured, wait about three seconds between tests so they do not all hit the system simultaneously.

An application that is already running may simply bring its existing window forward instead of starting another process. That still confirms that the shortcut resolves correctly.

### 5. Verify Windows registration

Run:

```powershell
Get-CimInstance Win32_StartupCommand |
    Where-Object {
        $_.Name -Match 'APP NAME 1|APP NAME 2'
    } |
    Select-Object Name, Command, Location, User |
    Format-List
```

Each new entry should report `Location : Startup` for the intended Windows user.

### 6. Perform the real test

Sign out and back in, or restart Windows. Give Windows time to finish loading; Startup-folder applications may be delayed.

## Why this approach

- It uses Microsoft's standard per-user Startup folder.
- It works with packaged applications whose executables live in protected, versioned WindowsApps directories.
- Stable application IDs survive ordinary app updates.
- There are no stored scripts to be blocked by execution policy or script-host security.
- No administrator access is normally required to create per-user shortcuts.
- Removing the shortcut cleanly disables the behavior.

## Important Xbox behavior

The Xbox app can register a native startup task that launches its tray/background component rather than the full Xbox window. If the goal is to display the full Xbox application at sign-in, use the Xbox application shortcut in the Startup folder even when Xbox's own “runs at log-in” option is enabled.

## Removing an application later

Open `shell:startup` from the Run dialog and delete only that application's `.lnk`. Alternatively, Codex can remove the exact shortcut after confirming its full path.
