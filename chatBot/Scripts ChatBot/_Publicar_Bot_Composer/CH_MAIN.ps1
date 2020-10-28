Import-Module "$PSScriptRoot\Fun_Azure.psm1" -Force 
Import-Module "$PSScriptRoot\LOG.psm1" -Force 
Import-Module "$PSScriptRoot\_Secrets.psm1" -Force 

$appId = $null

function CH_MAIN {
    try {
        # Iniciando CLI
        Write-Host "Iniciando sesión..."
        $continuar, $obj_login = Login_Azure -user_name $user_name -user_pass $user_pass
        
        if ($true -eq $continuar) {           
            Generar_Bot
        }
    }
    catch {
        Write-Host "Se produjo un error, checar log."
        LOG -Level "ERROR" -Action ("Resumen: " + $_.Exception.Message) -Date $Fecha
        LOG -Level "ERROR" -Action $_ -Date $Fecha
    }
    finally {
        LOG -Level "###" -Action "######################################## END OF CODE ##################################################" -Date $Fecha
        # read-host "Press ENTER to continue..."
        Exit
    }
}

function Generar_Bot{
    # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    # <<<<<<<<<<<<<<<<<<<<                      Obteniendo appID               >>>>>>>>>>>>>>>>>>>>
    # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

    Write-Host "Login Correcto"
    
    Write-Host "Validando si ya existe aplicación"
    # Buscando si existe una aplicación con ese nombre
    $string_response = az ad app list --display-name "$nombre_bot"
    $vec_app_list_bot = StringJson_to_ObjVecPS -string_response $string_response
    
    # Validando aplicaciones
    $app_encontradas = $vec_app_list_bot.result.Count
    Write-Host "--> Aplicaciones encontradas: $app_encontradas"
    if ($app_encontradas -eq 0) {
        
        Write-Host "--> Creando la aplicación"
        $string_response = az ad app create --display-name "$nombre_bot" --available-to-other-tenants  #--password "$password"
        $obj_app_create = StringJson_to_ObjVecPS -string_response $string_response
        
        # Obteniendo appID
        $appId = $obj_app_create.result.appId
        Write-Host "--> Aplicación creada con appId: $appId`n"
    }
    else { 
        if ($app_encontradas -eq 1) {   

            do
            {
            Write-Host "--> Ya existe $app_encontradas aplicación con ese nombre. ¿Deseas utilizarla?" 
            $ReadHost = read-host "S / N"
            
                Switch ($ReadHost) 
                { 
                    'S' {
                        $appId = $vec_app_list_bot.result[0].appId; 
                        Write-host "--> Aplicación seleccionada con appId: $appId"
                        $resp_valida = $true
                    } 
                    'N' {Write-Host "Bien, modifica el nombre del bot y vuelve a correr el script."; Exit } 
                    Default {Write-Host "Esa no fue una respuesta valida."; $resp_valida = $false } 
                } 
            }
            while ($resp_valida -eq $false) 
        }
        else {   
            Write-Host "--> Ya existen $app_encontradas aplicaciones con ese nombre, por favor, intenta con otro." 
        }
    }

    # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    # <<<<<<<<<<<<<<<<<<<<          Creando Plan de ser necesario              >>>>>>>>>>>>>>>>>>>>
    # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    
    # Verificando si ya se cuenta con el appId
    if($null -ne $appId -and $appId -ne ""){


        # Añadiendo secret
        $string_response = az ad app credential reset `
            --id $appId `
            --credential-description "Secret" `
            --end-date "2299-12-31" `
            --append   

        $obj_app_credential_reset = StringJson_to_ObjVecPS -string_response $string_response
        $password = $obj_app_credential_reset.result.password
        
        #------------------------------------------------------------------------------------------

        # Obteniendo todos los app service plan
        $string_response= az appservice plan list
        $obj_appservice_plan_list = StringJson_to_ObjVecPS -string_response $string_response

        # Validando si ya existe el service plan
        $vec_app_ser_plan = $obj_appservice_plan_list.result
        $existe_app_ser_plan = $null
        foreach ($item in $vec_app_ser_plan){
            # $item.name
            if ($item.name -eq $app_service_plan) {
                $existe_app_ser_plan = $true
            } 
        }

        # Si no existe el app service plan
        if($null -eq $existe_app_ser_plan){
            write-host "Creando APP SERVICE PLAN"   
            
            if ($app_service_pan_OS_LINUX -eq $false){
                $string_response = az appservice plan create `
                --name $app_service_plan `
                --resource-group $resource_group `
                --sku F1
            }
            else{
                $string_response = az appservice plan create `
                --name $app_service_plan `
                --resource-group $resource_group `
                --sku F1 `
                --is-linux
            }
            $obj_appservice_plan_create = StringJson_to_ObjVecPS -string_response $string_response

            # $string_response = az appservice plan create `
            #     --name $app_service_plan `
            #     --resource-group $resource_group `
            #     --sku F1

            # $string_json = '{ result:' + $string_response + '}';
            # $obj_json4 = ConvertFrom-Json -InputObject $string_json;    
            
            # $obj_json4
            write-host "--> APP SERVICE PLAN creado"

            
        }
        else{
            write-host "Ya existe el APP Service Plan indicado"   
        }
    }

    # write-host "Encontrando las app deployadas"
    # az deployment group list --resource-group
    
    # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    # <<<<<<<<<<<<<<<<<<<<           Creando APP en App Service Plan           >>>>>>>>>>>>>>>>>>>>
    # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>           

    # # Validando si el template es valida en el grupo de recursos
    # write-host "Validando template"
    # az deployment group validate `
    #     --resource-group $resource_group `
    #     --template-file "template-with-preexisting-rg.json" `
    #     --parameters `
    #     appId=$appId `
    #     appSecret="$password" `
    #     botId="$nombre_bot" `
    #     appServicePlanLocation=$appServicePlanLocation
    #!!!!!!!!!!!! Falta validar si ya existe la app, por el momento parece sobreescribir

    write-host "Añadiendo APP al App Service Plan"
    $string_response = az deployment group create `
        --resource-group $resource_group `
        --template-file "template-with-preexisting-rg.json" `
        --name "$nombre_bot" `
        --parameters `
        appId="$appId" `
        appSecret="$password" `
        botId="$nombre_bot" `
        newWebAppName="$nombre_bot" `
        existingAppServicePlan=$app_service_plan `
        appServicePlanLocation=$appServicePlanLocation

    $obj_deployment_group_create_bot = StringJson_to_ObjVecPS -string_response $string_response
    write-host "--> App añadida al Service Plan"

    if ($generar_AD){
        write-host "`n>>>>>>>>>>>>>>>>---------------<<<<<<<<<<<<<<<<<<`n"
        Generar_AD
        write-host "`n>>>>>>>>>>>>>>>>---------------<<<<<<<<<<<<<<<<<<`n"
    }
    
    # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    # <<<<<<<<<<<<<<<<<<<<            Generando Json para composer             >>>>>>>>>>>>>>>>>>>>
    # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

    write-host "Obteniendo token"
    $string_response = az account get-access-token   #--resource "https://$nombre_bot.azurewebsites.net/"
    $obj_account_get_token = StringJson_to_ObjVecPS -string_response $string_response
    $token = $obj_account_get_token.result.accessToken

    write-host "Creando Json"
    if ($luisResource -eq ""){
        $profile_json = "
        {
            accessToken: '$token',
            name: '$nombre_bot',
            hostname: '$nombre_bot',
            settings: {
                MicrosoftAppId: '$appId',
                MicrosoftAppPassword: '$password'
            }
        }
        "
    }
    else{
            # Obteniendo keys de runtime y authoring de Luis
        $string_response = az cognitiveservices account keys list --name $luisResource --resource-group $resource_group
        $obj_cognitiveservices_key_list_1 = StringJson_to_ObjVecPS -string_response $string_response
        $luis_endpointKey = $obj_cognitiveservices_key_list_1.result.key1

        $string_response = az cognitiveservices account keys list --name "$luisResource-Authoring" --resource-group $resource_group
        $obj_cognitiveservices_key_list_2 = StringJson_to_ObjVecPS -string_response $string_response
        $luis_authoringKey = $obj_cognitiveservices_key_list_2.result.key1

        $string_response = az cognitiveservices account show --name $luisResource --resource-group $resource_group
        $obj_cognitiveservices_show = StringJson_to_ObjVecPS -string_response $string_response
        $obj_cognitiveservices_show
        $luis_region = $obj_cognitiveservices_show.result.location

        $profile_json = "
        {
            accessToken: '$token',
            name: '$nombre_bot',
            environment: 'Composer',
            hostname: '$nombre_bot',
            luisResource: '$luisResource',
            settings: {
                luis: {
                    endpointKey: '$luis_endpointKey',
                    authoringKey: '$luis_authoringKey',
                    region: '$luis_region'
                        },
                MicrosoftAppId: '$appId',
                MicrosoftAppPassword: '$password'
            }
        }
        "
    }
            
    # Convirtiendo a Objeto
    $myobject = $profile_json | ConvertFrom-Json
    # Convirtiendo a JSON e imprimiedo resultado
    $final_json = $myobject | ConvertTo-Json
    $final_json

    # Creando carpeta Respaldo si no existe
    $ruta_actual = (Get-Location).Path
    if (-Not (Test-Path ($ruta_actual + "\Respaldo") -PathType Container)) {
        $salida_carpeta = New-Item -ItemType Directory -Force -Path ( $ruta_actual + "\Respaldo")
    }
    # Guardando respaldo
    Write-Host "Guardando json de respaldo"
    Set-Content -Path "Respaldo\PUBLIC_$nombre_bot.json" -Value $final_json

    # # Convirtiendo a JSON comprimido
    # $json_string_compress = $myobject | ConvertTo-Json -Compress
    # # $json_string_compress = $json_string -replace '"', '\"'

    # # Leyendo el archivo
    # $string_response = Get-Content -Path "$ruta_composer\appsettings.json" -Raw | ConvertFrom-Json

    # # Modificando el json
    # ($string_response.publishTargets | where {$_.name -eq "sss"}).configuration = $json_string_compress

    # # Escribiendo el json
    # $string_response | ConvertTo-Json -depth 32| set-content "$ruta_composer\test____.json"
}


########################################################################################################################################################################################################################################################################
########################################################################################################################################################################################################################################################################
########################################################################################################################################################################################################################################################################
########################################################################################################################################################################################################################################################################
########################################################################################################################################################################################################################################################################
########################################################################################################################################################################################################################################################################
########################################################################################################################################################################################################################################################################


function Generar_AD {
    ##################################################################################################################
    ##############################             Creación de la aplicacion            ##################################
    ##################################################################################################################

    # Buscando si existe una aplicación con ese nombre
    $nombre_bot_AD = "$nombre_bot-AD"
    Write-Host "`nValidando si ya existe aplicacion AD`n--> Buscando... $nombre_bot_AD"
    $string_response = az ad app list --display-name $nombre_bot_AD
    $vec_app_list = StringJson_to_ObjVecPS -string_response $string_response

    # Obteniendo el total de aplicaciones encontradas
    $app_encontradas = $vec_app_list.result.Count
    Write-Host "--> Apps encontradas: $app_encontradas" 

    # Validando aplicaciones encontradas
    Switch ($app_encontradas) 
    { 
        0 { 
            $resources = construir_permisos 
            Write-Host "`nCreando la aplicacion AD"
            $replyUrl = "https://token.botframework.com/.auth/web/redirect"
            $string_response = az ad app create --display-name "$nombre_bot_AD" --available-to-other-tenants false  --reply-urls $replyUrl --required-resource-accesses $resources
            $obj_app_create = StringJson_to_ObjVecPS -string_response $string_response
            
            # Obteniendo appID
            $ClientId_AD = $obj_app_create.result.appId
            # $Tenant_AD = $obj_app_create.result.appId
            Write-Host "--> Aplicacion creada con ClientId_AD: $ClientId_AD" 

            # ------------------------------------------------------------------------------------------  
            # Añadiendo secret
            Write-Host "--> Creando secret"
            $string_response = az ad app credential reset --id $ClientId_AD `
                                --credential-description "Secret" --end-date "2299-12-31" --append   
            $obj_app_credential = StringJson_to_ObjVecPS -string_response $string_response

            # Obteniendo el client_secret
            $ClientSecret_AD = $obj_app_credential.result.password
            Write-Host "--> Secret: $ClientSecret_AD" 

            # ------------------------------------------------------------------------------------------
            # Obteniendo TenantID
            $TenantId = $obj_login.result[0].tenantId
            #Creando scopes
            foreach ($item_scope in $vector_scopes){
                $scopes += "$item_scope "
            }
            
            # Creando respaldo de información
            Respaldo -Nombre_App $nombre_bot_AD -Client_ID $ClientId_AD -Client_Secret $ClientSecret_AD -TenantId $TenantId -scopes $scopes
            Write-Host "--> Respaldo creado en carpeta \Respaldo\$nombre_bot_AD.json`n" 
            ; break  
        } 
    ##########################################################################
        1 {   
            $ClientId_AD = $vec_app_list.result.appId;
            $string_response = Get-Content -Path "Respaldo\AD_$nombre_bot_AD.json" 
            $obj_respaldo = StringJson_to_ObjVecPS -string_response $string_response
            
            $ClientSecret_AD = $obj_respaldo.result.Client_Secret
            $TenantId = $obj_respaldo.result.TenantId
            $scopes = $obj_respaldo.result.scopes

            Write-host "--> Aplicacion seleccionada con ClientId_AD: $ClientId_AD `n"           
            ; break  
        } 
    ##########################################################################
        Default {    
            Write-Host "Existe mas de 1 app para el nombre seleccionado, replantear." 
            Read-Host "Press ENTER to continue..."
            Exit
        } 
    } 



    ##################################################################################################################
    ##############################                 Añadiendo conector               ##################################
    ##################################################################################################################
    
    # Verificando que cuente con el ID
    if($null -ne $ClientId_AD -and $null -ne $ClientSecret_AD){
        Write-Host "Agregando conector al BOT"
        $ConnectionName = "ConnectionBot"

        # Para verificar que este siga siendo el ID https://dev.botframework.com/api/connectionsettings/GetServiceProviders
        # "serviceProviderDisplayName": "Azure Active Directory v2"
        # "serviceProviderId": "30dd229c-58e3-4a48-bdfd-91ec48eb906c",

        $string_response = az deployment group create `
            --resource-group $resource_group `
            --template-file "zAAD_V2_template_add_botservice_connection_params.json" `
            --name "Deploy_$nombre_bot" `
            --parameters `
                nombre_bot=$nombre_bot `
                ClientId=$ClientId_AD `
                ClientSecret=$ClientSecret_AD `
                TenantId=$TenantId `
                scopes=$scopes
        
        # Write-Host $string_response
        $obj_app_deployment_group_create = StringJson_to_ObjVecPS -string_response $string_response
        $res_add_perm = $obj_app_deployment_group_create.result.properties.provisioningState
        Write-Host "--> Resultado: $res_add_perm`n"

        # --------------------------------------------------------------------------------------------------------------------------

        # Para añadir los permisos desde el CLI es necesario tener permisos de Microsoft.BotService/listAuthServiceProviders/action 
        # Ya no se implementó de esta forma pero algunos de los pasos son los siguientes:

        # az bot authsetting create --resource-group $resource_group --name $nombre_bot --setting-name $ConnectionName `
        #                 --client-id $ClientId_AD --client-secret $ClientSecret_AD --provider-scope-string "openid profile User.ReadBasic.All User.Read" `
        #                 --service google 
        
        # az bot authsetting create --resource-group $resource_group --name $nombre_bot --setting-name $ConnectionName `
        #                 --client-id $MicrosoftAD_Id --client-secret $MicrosoftAD_Password --provider-scope-string "openid" `
        #                 --service google 
    }
}

function construir_permisos {
    Write-Host "`nBuscando permisos de API" 
    $graphId = "00000003-0000-0000-c000-000000000000"

    # Vector Vacío
    $recursos = @()

    # Obteniendo los Servicios Principales
    $string_response = az ad sp show --id $graphId --query "appRoles[].{Value:value, Id:id}" 
    $vec_sp_show_appRoles = StringJson_to_ObjVecPS -string_response $string_response
    $template = ""
    foreach ($item in $vec_sp_show_appRoles.result){
        foreach ($scope in $vector_scopes){
            if ($scope -eq $item.Value) {
                # Write-host $item.Value " - " $item.Id
                $recursos += $item.Id
                $template += " { ""id"": " + $item.Id + ", ""type"": ""Scope"" },"
            }   
        }
    }

    $string_response = az ad sp show --id $graphId --query "oauth2Permissions[].{Value:value, Id:id}"
    $vec_sp_show_oauth2Permissions = StringJson_to_ObjVecPS -string_response $string_response

    foreach ($item in $vec_sp_show_oauth2Permissions.result){
        foreach ($scope in $vector_scopes){
            if ($scope -eq $item.Value) {
                # Write-host $item.Value " - " $item.Id
                $recursos += $item.Id
                $template += "{ ""id"": """ + $item.Id + """, ""type"": ""Scope"" }, "
            }
        }
    }

    # Borrar último caracter
    # $template = $template -replace ".$"

    # Borrar últimos 2 caracteres
    $template = $template -replace ".{2}$"
    
    # Construyendo recursos
    $resources = @"
[{ "resourceAppId": "$graphId", "resourceAccess": [$template]}]
"@               
    Write-Host "--> Recursos a agergar:`n$resources"
    $resources = ConvertTo-Json $resources
    # Write-Host $resources

    return $resources
}



CH_MAIN