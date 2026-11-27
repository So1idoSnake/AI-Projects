$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$apps = @(
    @{ Label = 'Radarr - Movies and 4K HDR'; Name = 'Radarr'; Port = 7878 },
    @{ Label = 'Radarr - Small Movies'; Name = 'Radarr Small Movies'; Port = 7879 },
    @{ Label = 'Sonarr - TV'; Name = 'Sonarr'; Port = 8989 },
    @{ Label = 'Prowlarr - Indexers'; Name = 'Prowlarr'; Port = 9696 },
    @{ Label = 'NZBGet - Usenet Downloads'; Name = 'NZBGet'; Port = 6789 },
    @{ Label = 'qBittorrent - Queue'; Name = 'qBittorrent'; Port = 8080 }
)

$form = [Windows.Forms.Form]::new()
$form.Text = 'Media Server Logins'
$form.StartPosition = 'CenterScreen'
$form.AutoScaleMode = 'Font'
$form.ClientSize = [Drawing.Size]::new(490, 252)
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false

$layout = [Windows.Forms.TableLayoutPanel]::new()
$layout.Dock = 'Fill'
$layout.Padding = [Windows.Forms.Padding]::new(16)
$layout.ColumnCount = 1
$layout.RowCount = 6
foreach ($height in 26, 34, 30, 30, 38) {
    [void]$layout.RowStyles.Add([Windows.Forms.RowStyle]::new('Absolute', $height))
}
[void]$layout.RowStyles.Add([Windows.Forms.RowStyle]::new('Percent', 100))
$form.Controls.Add($layout)

$label = [Windows.Forms.Label]::new()
$label.Text = 'Choose a media app'
$label.Dock = 'Fill'
$layout.Controls.Add($label, 0, 0)

$picker = [Windows.Forms.ComboBox]::new()
$picker.AccessibleName = 'Media app'
$picker.DropDownStyle = 'DropDownList'
$picker.Dock = 'Fill'
foreach ($app in $apps) { [void]$picker.Items.Add($app.Label) }
$layout.Controls.Add($picker, 0, 1)

$username = [Windows.Forms.TextBox]::new()
$username.AccessibleName = 'Username'
$username.ReadOnly = $true
$username.Dock = 'Fill'
$layout.Controls.Add($username, 0, 2)

$passwordBox = [Windows.Forms.TextBox]::new()
$passwordBox.AccessibleName = 'Password'
$passwordBox.ReadOnly = $true
$passwordBox.Dock = 'Fill'
$passwordBox.Visible = $false
$layout.Controls.Add($passwordBox, 0, 3)

$actions = [Windows.Forms.FlowLayoutPanel]::new()
$actions.Dock = 'Fill'
$layout.Controls.Add($actions, 0, 4)

$open = [Windows.Forms.Button]::new()
$open.Text = 'Copy password and open app'
$open.AutoSize = $true
$actions.Controls.Add($open)
$form.AcceptButton = $open

$show = [Windows.Forms.Button]::new()
$show.Text = 'Show password'
$show.AutoSize = $true
$actions.Controls.Add($show)

$status = [Windows.Forms.Label]::new()
$status.Dock = 'Fill'
$layout.Controls.Add($status, 0, 5)
$hint = 'The password is copied to your Windows clipboard for pasting into the sign-in page.'

try {
    $file = Join-Path $PSScriptRoot 'Private\LocalUiCredentials.json'
    $logins = Get-Content -LiteralPath $file -Raw | ConvertFrom-Json
} catch {
    $status.Text = 'Saved logins could not be opened. Use this shortcut from the Windows account that set up the media server.'
    $picker.Enabled = $false
    $open.Enabled = $false
    $show.Enabled = $false
}

function Read-AppPassword($Login) {
    $secure = ConvertTo-SecureString -String $Login.Password
    try { [PSCredential]::new($Login.User, $secure).GetNetworkCredential().Password }
    finally { $secure.Dispose() }
}

$picker.Add_SelectedIndexChanged({
    $selectedApp = $apps[$picker.SelectedIndex]
    $login = $logins.PSObject.Properties[$selectedApp.Name].Value
    $username.Text = $login.User
    $passwordBox.Clear()
    $passwordBox.Visible = $false
    $show.Text = 'Show password'
    $status.Text = $hint
})

$show.Add_Click({
    if ($passwordBox.Visible) {
        $passwordBox.Clear()
        $passwordBox.Visible = $false
        $show.Text = 'Show password'
        $status.Text = $hint
        return
    }
    try {
        $selectedApp = $apps[$picker.SelectedIndex]
        $login = $logins.PSObject.Properties[$selectedApp.Name].Value
        $passwordBox.Text = Read-AppPassword $login
        $passwordBox.Visible = $true
        $show.Text = 'Hide password'
        $status.Text = 'Use this password for the selected app. Hide it when you finish.'
    } catch {
        $status.Text = 'The saved password could not be opened under this Windows account.'
    }
})

$open.Add_Click({
    $selectedApp = $apps[$picker.SelectedIndex]
    $login = $logins.PSObject.Properties[$selectedApp.Name].Value
    $password = $null
    try {
        $password = Read-AppPassword $login
        $clipboard = [Windows.Forms.DataObject]::new()
        $clipboard.SetText($password)
        $clipboard.SetData('ExcludeClipboardContentFromMonitorProcessing', $false, [IO.MemoryStream]::new([byte[]]@(1, 0, 0, 0)))
        [Windows.Forms.Clipboard]::SetDataObject($clipboard, $true, 10, 100)
    } catch {
        $status.Text = 'Could not copy the password. Use Show password to view it here.'
        return
    } finally {
        $password = $null
        $clipboard = $null
    }

    $status.Text = 'Password copied. Use the username above and press Ctrl+V in the browser password field.'
    try {
        Start-Process "http://127.0.0.1:$($selectedApp.Port)/"
    } catch {
        $status.Text = 'Password copied. Open the desktop link for this app and press Ctrl+V in the password field.'
    }
})

if ($picker.Enabled) { $picker.SelectedIndex = 0 }
try { [void]$form.ShowDialog() } finally { $form.Dispose() }
