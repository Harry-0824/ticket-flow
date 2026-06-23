param(
    [string]$ApiBaseUrl = $env:TICKETFLOW_API_BASE_URL,
    [string]$RunId = "",
    [int]$TimeoutSec = 60
)

$ErrorActionPreference = "Stop"

function Fail([string]$Step, [string]$Message) {
    Write-Host "FAIL $Step $Message"
    exit 1
}

function Pass([string]$Step, [string]$Message) {
    Write-Host "PASS $Step $Message"
}

function Normalize-ApiOrigin([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        Fail "config" "ApiBaseUrl is required. Pass -ApiBaseUrl or set TICKETFLOW_API_BASE_URL."
    }

    $normalized = $Value.Trim().TrimEnd("/")
    if ($normalized.EndsWith("/api", [StringComparison]::OrdinalIgnoreCase)) {
        $normalized = $normalized.Substring(0, $normalized.Length - 4)
    }

    return $normalized
}

function Invoke-TicketFlowRequest {
    param(
        [string]$Step,
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [string]$Token = "",
        [int[]]$ExpectedStatus
    )

    $request = @{
        Uri = "$ApiOrigin$Path"
        Method = $Method
        TimeoutSec = $TimeoutSec
        UseBasicParsing = $true
    }

    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        $request["Headers"] = @{ Authorization = "Bearer $Token" }
    }

    if ($null -ne $Body) {
        $request["ContentType"] = "application/json"
        $request["Body"] = ($Body | ConvertTo-Json -Depth 8)
    }

    try {
        $response = Invoke-WebRequest @request
    }
    catch {
        $statusCode = $null
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        if ($statusCode) {
            Fail $Step "status=$statusCode"
        }

        Fail $Step ($_.Exception.Message -replace "\s+", " ")
    }

    $actualStatus = [int]$response.StatusCode
    if ($ExpectedStatus -notcontains $actualStatus) {
        Fail $Step "expected=$($ExpectedStatus -join ',') actual=$actualStatus"
    }

    $parsedBody = $null
    if (-not [string]::IsNullOrWhiteSpace($response.Content)) {
        try {
            $parsedBody = $response.Content | ConvertFrom-Json
        }
        catch {
            Fail $Step "response was not valid JSON"
        }
    }

    return [pscustomobject]@{
        StatusCode = $actualStatus
        Body = $parsedBody
    }
}

$ApiOrigin = Normalize-ApiOrigin $ApiBaseUrl
if ([string]::IsNullOrWhiteSpace($RunId)) {
    $RunId = ([guid]::NewGuid().ToString("N")).Substring(0, 10)
}

$email = "tf-smoke-$RunId@ticketflow.local"
$password = "TfSmoke-$([guid]::NewGuid().ToString("N"))!"
$ticketTitle = "TicketFlow smoke $RunId"
$ticketId = $null
$ticketDeleted = $false
$token = ""

try {
    $health = Invoke-TicketFlowRequest -Step "health" -Method "GET" -Path "/health" -ExpectedStatus @(200)
    if ($health.Body.status -ne "Healthy") {
        Fail "health" "body.status=$($health.Body.status)"
    }
    Pass "health" "status=200 body.status=Healthy"

    $register = Invoke-TicketFlowRequest -Step "register" -Method "POST" -Path "/api/auth/register" -ExpectedStatus @(201) -Body @{
        email = $email
        displayName = "TicketFlow Smoke"
        password = $password
    }
    if ([string]::IsNullOrWhiteSpace($register.Body.token)) {
        Fail "register" "missing token"
    }
    Pass "register" "status=201 email=$email"

    $login = Invoke-TicketFlowRequest -Step "login" -Method "POST" -Path "/api/auth/login" -ExpectedStatus @(200) -Body @{
        email = $email
        password = $password
    }
    $token = $login.Body.token
    if ([string]::IsNullOrWhiteSpace($token)) {
        Fail "login" "missing token"
    }
    Pass "login" "status=200 email=$email"

    $create = Invoke-TicketFlowRequest -Step "create-ticket" -Method "POST" -Path "/api/tickets" -ExpectedStatus @(201) -Token $token -Body @{
        title = $ticketTitle
        description = "Created by production smoke test run $RunId."
        status = "Open"
        priority = "Medium"
        assignee = "Smoke Test"
    }
    $ticketId = $create.Body.id
    if (-not $ticketId) {
        Fail "create-ticket" "missing id"
    }
    Pass "create-ticket" "status=201 id=$ticketId"

    $list = Invoke-TicketFlowRequest -Step "list-tickets" -Method "GET" -Path "/api/tickets" -ExpectedStatus @(200) -Token $token
    $matchingTickets = @(@($list.Body) | Where-Object { [string]$_.id -eq [string]$ticketId })
    if ($matchingTickets.Count -ne 1) {
        Fail "list-tickets" "created ticket not found"
    }
    Pass "list-tickets" "status=200 found=true"

    $read = Invoke-TicketFlowRequest -Step "read-ticket" -Method "GET" -Path "/api/tickets/$ticketId" -ExpectedStatus @(200) -Token $token
    if ($read.Body.id -ne $ticketId) {
        Fail "read-ticket" "id mismatch"
    }
    Pass "read-ticket" "status=200 id=$ticketId"

    $update = Invoke-TicketFlowRequest -Step "update-ticket" -Method "PUT" -Path "/api/tickets/$ticketId" -ExpectedStatus @(200) -Token $token -Body @{
        title = "$ticketTitle updated"
        description = "Updated by production smoke test run $RunId."
        status = "InProgress"
        priority = "High"
        assignee = "Smoke Test"
    }
    if ($update.Body.status -ne "InProgress") {
        Fail "update-ticket" "status not updated"
    }
    Pass "update-ticket" "status=200 id=$ticketId"

    Invoke-TicketFlowRequest -Step "delete-ticket" -Method "DELETE" -Path "/api/tickets/$ticketId" -ExpectedStatus @(204) -Token $token | Out-Null
    $ticketDeleted = $true
    Pass "delete-ticket" "status=204 id=$ticketId"

    Pass "summary" "runId=$RunId ticketDeleted=$ticketDeleted accountCleanup=not-supported"
}
finally {
    if ($ticketId -and -not $ticketDeleted -and -not [string]::IsNullOrWhiteSpace($token)) {
        try {
            Invoke-TicketFlowRequest -Step "cleanup-ticket" -Method "DELETE" -Path "/api/tickets/$ticketId" -ExpectedStatus @(204, 404) -Token $token | Out-Null
            Write-Host "PASS cleanup-ticket id=$ticketId"
        }
        catch {
            Write-Host "WARN cleanup-ticket id=$ticketId message=$($_.Exception.Message -replace '\s+', ' ')"
        }
    }
}
