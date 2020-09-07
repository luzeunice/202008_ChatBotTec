function Login_Azure {
    Param
    (
        # [ValidateNotNullOrEmpty()]
        [string]$user_name,
        # [ValidateNotNullOrEmpty()]
        [string]$user_pass
    )
    # $Fecha = $script:Fecha
    # $Fecha = Get-Date -Format "yyyyMMdd";
    try{
        ##-- --## Iniciando sesión
        LOG -Action "Iniciando sesión" -Date $Fecha
        if ($user_name -eq "" -Or $user_pass -eq ""){
            $json_login = az login --output json
        }
        else {
            $json_login = az login --username $user_name --password $user_pass --output json
        }
        LOG -Level "INFO_VAR" -Action "Resultado login: $json_login" -Date $Fecha

        $continuar = ""
        #-- --## Validación de resultado login null
        if ($null -ne $json_login) {
            $json_login = '{ result:' + $json_login + '}';
            $obj_login = ConvertFrom-Json -InputObject $json_login;
            
            if ($obj_login.result.state[0] -eq "Enabled") {  # Verificar cunado sólo hay una suscripcion $obj_login.result.state
                LOG -Action "Login Exitoso" -Date $Fecha
                $continuar = $true
            }
            else{
                $continuar = "Revisar"
            }
        }
        else {
            $continuar = $false
        }
        return $continuar
    }
    catch {
        LOG -Level "ERROR" -Action ("Resumen: " + $_.Exception.Message) -Date $Fecha
        LOG -Level "ERROR" -Action $_ -Date $Fecha
    }
}

