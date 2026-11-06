$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
dotnet ef database update --project Infrastructure --startup-project FreeBook
exit $LASTEXITCODE
