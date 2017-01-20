$files = Get-ChildItem -Path $rootPath -Include "wallabag.pdb" -Recurse

foreach ($file in $files)
{
    Get-FileHash -Path $file | Format-List
}