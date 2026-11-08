Start-Process dotnet -ArgumentList 'run', '--no-build', '--urls', 'http://localhost:5128' -WorkingDirectory 'C:\Users\Prpjects\Bookify\FreeBook' -WindowStyle Hidden
Start-Sleep 12
$response = curl.exe -s -v -b '.AspNetCore.Culture=c=ar|uic=ar' http://localhost:5128/ 2>&1
$response