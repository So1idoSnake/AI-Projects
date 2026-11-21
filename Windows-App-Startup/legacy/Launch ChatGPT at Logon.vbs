Option Explicit

Const ForAppending = 8

Dim launcher, fileSystem, logPath
Set launcher = CreateObject("WScript.Shell")
Set fileSystem = CreateObject("Scripting.FileSystemObject")
logPath = "C:\Users\micha\AppData\Local\CodexStartup\ChatGPT-Xbox-Startup.log"

Sub WriteLog(message)
    Dim logFile
    Set logFile = fileSystem.OpenTextFile(logPath, ForAppending, True)
    logFile.WriteLine Now & " | " & message
    logFile.Close
    Set logFile = Nothing
End Sub

WriteLog "Startup launcher began"

' Windows already delays Startup-folder items; this small buffer is enough for packaged apps.
WScript.Sleep 10000

On Error Resume Next
launcher.Run """C:\Windows\explorer.exe"" shell:AppsFolder\OpenAI.Codex_2p2nqsd0c76g0!App", 0, False
If Err.Number = 0 Then
    WriteLog "ChatGPT launch requested successfully"
Else
    WriteLog "ChatGPT launch failed: " & Err.Number & " - " & Err.Description
End If
Err.Clear

' Stagger the launches slightly so both packaged apps initialize reliably.
WScript.Sleep 3000
launcher.Run """C:\Windows\explorer.exe"" shell:AppsFolder\Microsoft.GamingApp_8wekyb3d8bbwe!Microsoft.Xbox.App", 0, False
If Err.Number = 0 Then
    WriteLog "Xbox launch requested successfully"
Else
    WriteLog "Xbox launch failed: " & Err.Number & " - " & Err.Description
End If
On Error GoTo 0

WriteLog "Startup launcher finished"
Set launcher = Nothing
Set fileSystem = Nothing
