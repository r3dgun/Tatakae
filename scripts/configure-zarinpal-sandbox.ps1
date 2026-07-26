param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$MerchantId,

    [string]$CallbackUrl = "https://localhost:7075/api/payments/zarinpal/callback",

    [string]$WebReturnUrl = "https://localhost:7076/payment-result"
)

$ErrorActionPreference = "Stop"
$apiProject = Join-Path $PSScriptRoot "..\src\Tatakae.Api\Tatakae.Api.csproj"

if (-not (Test-Path $apiProject)) {
    throw "Tatakae.Api.csproj پیدا نشد: $apiProject"
}

Write-Host "Configuring Zarinpal sandbox secrets for Tatakae.Api..."

dotnet user-secrets set "Zarinpal:Enabled" "true" --project $apiProject
dotnet user-secrets set "Zarinpal:Sandbox" "true" --project $apiProject
dotnet user-secrets set "Zarinpal:RefundEnabled" "false" --project $apiProject
dotnet user-secrets set "Zarinpal:MerchantId" $MerchantId --project $apiProject
dotnet user-secrets set "Zarinpal:Currency" "IRT" --project $apiProject
dotnet user-secrets set "Zarinpal:CallbackUrl" $CallbackUrl --project $apiProject
dotnet user-secrets set "Payments:WebReturnUrl" $WebReturnUrl --project $apiProject

Write-Host "Sandbox configured. Refund remains disabled to avoid calling a Production GraphQL endpoint."
Write-Host "Check status after login: GET /api/admin/payments/zarinpal/configuration"
