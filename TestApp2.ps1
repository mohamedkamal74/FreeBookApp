$process = Start-Process dotnet -ArgumentList 'run', '--no-build', '--urls', 'http://localhost:5128' -WorkingDirectory 'C:\Users\Prpjects\Bookify\FreeBook' -PassThru -RedirectStandardOutput 'C:\Users\Prpjects\Bookify\app.log' -RedirectStandardError 'C:\Users\Prpjects\Bookify\app.err'
Start-Sleep 10
$response = curl.exe -s -v -b '.AspNetCore.Culture=c=ar|uic=ar' http://localhost:5128/ 2>&1
$response
Start-Sleep 2
Get-Content 'C:\Users\Prpjects\Bookify\app.err' -Tail 50
Get-Content 'C:\Users\Prpjects\Bookify\app.log' -Tail 50
Stop-Process -Id $process.Id -Force