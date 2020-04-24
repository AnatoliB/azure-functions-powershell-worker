param($name)

Write-Host "SayHello($name) started"
if ($name -eq 'Seattle') {
    throw "Injected error"
}
Write-Host "SayHello($name) finished"

"Hello $name"
