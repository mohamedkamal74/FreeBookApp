$connectionString = 'Server=(localdb)\MSSQLLocalDB;Database=BookDb;Trusted_Connection=True;MultipleActiveResultSets=True;'
$sql = Get-Content -Path 'C:\Users\Prpjects\Bookify\TestData.sql' -Raw -Encoding UTF8
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = $sql
$command.CommandTimeout = 120
$result = $command.ExecuteNonQuery()
Write-Host "Rows affected: $result"
$connection.Close()