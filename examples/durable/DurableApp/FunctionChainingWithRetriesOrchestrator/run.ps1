using namespace System.Net

param($Context)

$ErrorActionPreference = 'Stop'

$output = @()

$retryOptions = New-DurableRetryOptions `
                    -FirstRetryInterval (New-Timespan -Seconds 5) `
                    -MaxNumberOfAttempts 3

$output += Invoke-ActivityFunction -FunctionName 'FlakyActivity' -Input 'Tokyo' -RetryOptions $retryOptions
$output += Invoke-ActivityFunction -FunctionName 'FlakyActivity' -Input 'Seattle' -RetryOptions $retryOptions
$output += Invoke-ActivityFunction -FunctionName 'FlakyActivity' -Input 'London' -RetryOptions $retryOptions

$output
