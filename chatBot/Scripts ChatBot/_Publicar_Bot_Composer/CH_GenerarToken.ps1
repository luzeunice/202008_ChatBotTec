
Import-Module "$PSScriptRoot\Fun_Azure.psm1" -Force 
Import-Module "$PSScriptRoot\LOG.psm1" -Force 

$Fecha = Get-Date -Format "yyyyMMdd";
Set-Variable -Name "Fecha" -Value $Fecha -Scope global -Force

Write-Host "Iniciando sesion..."
$continuar = Login_Azure -user_name $user_name -user_pass $user_pass

if ($true -eq $continuar) {   

    write-host "`nObteniendo token`n"
    $string_response = az account get-access-token   #--resource "https://$nombre_bot.azurewebsites.net/"
    $string_json = '{ result:' + $string_response + '}';
    $obj_json6 = ConvertFrom-Json -InputObject $string_json;
    $token = $obj_json6.result.accessToken
    $token
    write-host ""
    
}