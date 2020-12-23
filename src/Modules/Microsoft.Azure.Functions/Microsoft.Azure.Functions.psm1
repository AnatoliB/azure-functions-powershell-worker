function Invoke-ActivityFunction(
    [Parameter(Mandatory = $true)]
    [string]
    $FunctionName,

    [ValidateNotNull]
    [object]
    $Input,

    [switch]
    $NoWait
) {
    throw 'Not implemented'
}
