$vsixPath = "$($env:USERPROFILE)\sqlite-uwp.vsix"
(New-Object Net.WebClient).DownloadFile('http://sqlite.org/2017/sqlite-uwp-3170000.vsix', $vsixPath)
"`"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\VSIXInstaller.exe`" /q /a $vsixPath" | out-file ".\install-vsix.cmd" -Encoding ASCII
& .\install-vsix.cmd
Write-Host "Installed SQLite." -ForegroundColor green