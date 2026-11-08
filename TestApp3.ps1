$process = Start-Process dotnet -ArgumentList 'run', '--no-build', '--urls', 'http://localhost:5128' -WorkingDirectory 'C:\Users\Prpjects\Bookify\FreeBook' -PassThru -RedirectStandardOutput 'C:\Users\Prpjects\Bookify\app.log' -RedirectStandardError 'C:\Users\Prpjects\Bookify\app.err'
Start-Sleep 12
$response = curl.exe -s -v -b '.AspNetCore.Culture=c=ar|uic=ar' http://localhost:5128/ 2>&1
$response
Start-Sleep 2
Get-Content 'C:\Users\Prpjects\Bookify\app.err' -Tail 100
Stop-Process -Id $process.Id -Force