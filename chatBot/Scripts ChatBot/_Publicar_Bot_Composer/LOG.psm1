function LOG {
    Param 
    ( 
        [ValidateSet("INFO", "INICIO", "INFO_VAR", "FIN", "ERROR", "###")] 
        [string]$Level = "INFO",

        [ValidateNotNullOrEmpty()]
        [string]$Action,
    
        [ValidateNotNullOrEmpty()]
        [string]$Date
    ) 

    # Obteniendo carpeta actual
    $ruta_actual = (Get-Location).Path
    $ruta_dest_log = $ruta_actual
    # $ruta_dest_log = "D:\5) Entregables\E14 Salesforce DataFactory\Cubo primer entregable" 

    # Creando carpeta LOG si no existe
    if (-Not (Test-Path ($ruta_dest_log + "\LOG") -PathType Container)) {
        New-Item -ItemType Directory -Force -Path ( $ruta_dest_log + "\LOG")
    }

    # Moviendo a carpeta log
    Set-Location ( $ruta_dest_log + "\LOG")   

    
    ### Guardando datos log ###
    $Nombre_Log = "Log_Publicar_Composer-$Date.txt"
    (Get-Date).ToString() | out-file $Nombre_Log -Append                # Fecha de registro
    "$Level : $Action`r`n" | out-file $Nombre_Log -Append               # Datos de parámetros
    
    # Regresando a carpeta actual
    Set-Location $ruta_actual
}