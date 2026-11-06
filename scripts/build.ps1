dotnet restore FreeBook.sln
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build FreeBook.sln --no-restore
exit $LASTEXITCODE
