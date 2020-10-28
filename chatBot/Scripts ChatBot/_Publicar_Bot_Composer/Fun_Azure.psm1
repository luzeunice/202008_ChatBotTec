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
        return $continuar, $obj_login
    }
    catch {
        LOG -Level "ERROR" -Action ("Resumen: " + $_.Exception.Message) -Date $Fecha
        LOG -Level "ERROR" -Action $_ -Date $Fecha
    }
}


function StringJson_to_ObjVecPS {
    param (
        [string]$string_response
    )
    # Acondicionando a objeto
    $string_json = '{ result:' + $string_response + '}';
    $obj_or_vec = ConvertFrom-Json -InputObject $string_json;
    # $obj_or_vec = $obj_json;

    return $obj_or_vec
}

function Respaldo {
    param (
        [string]$Nombre_App,
        [string]$Client_ID,
        [string]$Client_Secret,
        [string]$TenantId,
        [string]$scopes
    )

    # Obteniendo carpeta actual
    $ruta_actual = (Get-Location).Path

    # Creando carpeta si no existe
    if (-Not (Test-Path ($ruta_actual + "\Respaldo") -PathType Container)) {
        $salida_carpeta = New-Item -ItemType Directory -Force -Path ( $ruta_actual + "\Respaldo")
    }

    # Moviendo a carpeta 
    Set-Location ( $ruta_actual + "\Respaldo")   

    ### Guardando el respaldo ###
    $Nombre_Archivo = "$Nombre_App.json"
    $fecha_generacion = (Get-Date).ToString()

    $obj = @"
{
    "Fecha_creacion" : "$fecha_generacion",
    "Nombre_APP" : "$Nombre_App",
    "Client_ID" : "$Client_ID",
    "Client_Secret" : "$Client_Secret",
    "TenantId" : "$TenantId",
    "scopes" : "$scopes",
    "Nombre_del_conector" : "Connector"
}
"@ 
    Set-Content -Path "AD_$Nombre_Archivo" -Value $obj
    # Write-Host $obj 

    # (Get-Date).ToString() + "`r`n"  |    out-file $Nombre_Archivo -Append                # Fecha de registro
    # "Nombre APP:"                   |    out-file $Nombre_Archivo -Append      
    # "$Nombre_App`r`n"               |    out-file $Nombre_Archivo -Append      
    # "Client_ID:"                    |    out-file $Nombre_Archivo -Append      
    # "$Client_ID`r`n"                |    out-file $Nombre_Archivo -Append      
    # "Client_Secret:"                |    out-file $Nombre_Archivo -Append      
    # "$Client_Secret`r`n"            |    out-file $Nombre_Archivo -Append   
    
    # Regresando a carpeta actual
    Set-Location $ruta_actual
}



