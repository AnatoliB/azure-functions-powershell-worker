using namespace System.Net

param($Context)

Write-Host 'FunctionChainingOrchestrator: started.'

$output = @()

Write-Warning 'Invoking SayHello(Tokyo)'
$output += Invoke-ActivityFunction -FunctionName 'SayHello' -Input 'Tokyo'
Write-Warning 'Invoked SayHello(Tokyo)'

Write-Warning 'Invoking SayHello(Seattle)'
$output += Invoke-ActivityFunction -FunctionName 'SayHello' -Input 'Seattle'
Write-Warning 'Invoked SayHello(Seattle)'

Write-Warning 'Invoking SayHello(London)'
$output += Invoke-ActivityFunction -FunctionName 'SayHello' -Input 'London'
Write-Warning 'Invoked SayHello(London)'

Write-Host 'FunctionChainingOrchestrator: finished.'

$output
