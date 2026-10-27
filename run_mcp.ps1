param(
    [string]$Method = "tools/call",
    [string]$ToolName = "execute_menu_item",
    [string]$MenuPath = "BattleSummoner/Diagnose Magic City Scene"
)

function Send-McpRequest($body, $sessionId = $null) {
    $req = [System.Net.HttpWebRequest]::Create("http://127.0.0.1:8080/mcp")
    $req.Method = "POST"
    $req.ContentType = "application/json"
    $req.Accept = "application/json, text/event-stream"
    $req.Timeout = 180000
    if ($sessionId) {
        $req.Headers.Add("mcp-session-id", $sessionId)
    }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
    $req.ContentLength = $bytes.Length
    $s = $req.GetRequestStream()
    $s.Write($bytes, 0, $bytes.Length)
    $s.Close()

    try {
        $resp = $req.GetResponse()
        $respSessionId = $resp.Headers["mcp-session-id"]
        $respStream = $resp.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($respStream)
        $respText = $reader.ReadToEnd()
        $resp.Close()

        return @{
            SessionId = $respSessionId
            Body = $respText
        }
    } catch {
        Write-Host "Exception: $($_.Exception.Message)"
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            Write-Host "Response Body: $($reader.ReadToEnd())"
        }
        return @{
            SessionId = $sessionId
            Body = "Error"
        }
    }
}

$initBody = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"pwsh-mcp","version":"1.0"}}}'
$initResult = Send-McpRequest $initBody
$sid = $initResult.SessionId

if ($ToolName -eq "refresh_unity") {
    $callBody = '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"refresh_unity","arguments":{"compile":"request","mode":"force","scope":"all","wait_for_ready":true}}}'
} else {
    $callBody = "{`"jsonrpc`":`"2.0`",`"id`":2,`"method`":`"tools/call`",`"params`":{`"name`":`"$ToolName`",`"arguments`":{`"menu_path`":`"$MenuPath`"}}}"
}

$callResult = Send-McpRequest $callBody $sid
Write-Host "Call Result: $($callResult.Body)"
