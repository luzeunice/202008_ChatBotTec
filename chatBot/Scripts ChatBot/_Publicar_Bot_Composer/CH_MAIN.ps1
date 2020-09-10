Import-Module "$PSScriptRoot\Fun_Azure.psm1" -Force 
Import-Module "$PSScriptRoot\LOG.psm1" -Force 
Import-Module "$PSScriptRoot\_Secrets.psm1" -Force 

$appId = $null

function CH_MAIN {
    try {
        # Iniciando CLI
        Write-Host "Iniciando sesión..."
        $continuar = Login_Azure -user_name $user_name -user_pass $user_pass
        
        if ($true -eq $continuar) {           
            # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            # <<<<<<<<<<<<<<<<<<<<                      Obteniendo appID               >>>>>>>>>>>>>>>>>>>>
            # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

            Write-Host "Login Correcto"
            
            Write-Host "Validando si ya existe aplicación"
            # Buscando si existe una aplicación con ese nombre
            $string_response = az ad app list --display-name "$nombre_bot"

            # Acondicionando a objeto
            $string_json = '{ result:' + $string_response + '}';
            $obj_json = ConvertFrom-Json -InputObject $string_json;
            
            # Validando aplicaciones
            $app_encontradas = $obj_json.result.Count
            if ($app_encontradas -eq 0) {

                Write-Host "Creando la aplicación"
                $string_response = az ad app create --display-name "$nombre_bot" --password "$password" --available-to-other-tenants
                
                # Acondicionando a objeto
                $string_json = '{ result:' + $string_response + '}';
                $obj_json2 = ConvertFrom-Json -InputObject $string_json;
                
                # Obteniendo appID
                $appId = $obj_json2.result.appId
                Write-Host "Aplicación creada con appId: $appId`n"
            }
            else { 
                if ($app_encontradas -eq 1) {   

                    do
                    {
                    Write-Host "Ya existe $app_encontradas aplicación con ese nombre. ¿Deseas utilizarla?" 
                    $ReadHost = read-host "S / N"
                    
                        Switch ($ReadHost) 
                        { 
                            'S' {
                                $appId = $obj_json.result.appId; 
                                Write-host "Aplicación seleccionada con appId: $appId"
                                $resp_valida = $true
                            } 
                            'N' {Write-Host "Bien, modifica el nombre del bot y vuelve a correr el script."; Exit } 
                            Default {Write-Host "Esa no fue una respuesta valida."; $resp_valida = $false } 
                        } 
                    }
                    while ($resp_valida -eq $false) 
                }
                else {   
                    Write-Host "Ya existen $app_encontradas aplicaciones con ese nombre, por favor, intenta con otro." 
                }
            }

            # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            # <<<<<<<<<<<<<<<<<<<<          Creando Plan de ser necesario              >>>>>>>>>>>>>>>>>>>>
            # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            
            # Verificando si ya se cuenta con el appId
            if($null -ne $appId){

                # Obteniendo todos los app service plan
                $string_response= az appservice plan list

                # Acondicionando a objeto
                $string_json = '{ result:' + $string_response + '}';
                $obj_json3 = ConvertFrom-Json -InputObject $string_json;

                # Validando si ya existe el service plan
                $vec_app_ser_plan = $obj_json3.result
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
                    $string_response = az appservice plan create `
                        --name $app_service_plan `
                        --resource-group $resource_group `
                        --sku F1

                    $string_json = '{ result:' + $string_response + '}';
                    $obj_json4 = ConvertFrom-Json -InputObject $string_json;
                    # $obj_json4
                    write-host "APP SERVICE PLAN creado"
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
            
            $string_json = '{ result:' + $string_response + '}';
            $obj_json5 = ConvertFrom-Json -InputObject $string_json;
            # $obj_json5
            write-host "App añadida al Service Plan"
            
            # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            # <<<<<<<<<<<<<<<<<<<<            Generando Json para composer             >>>>>>>>>>>>>>>>>>>>
            # <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

            write-host "Obteniendo token"
            $string_response = az account get-access-token   #--resource "https://$nombre_bot.azurewebsites.net/"
            $string_json = '{ result:' + $string_response + '}';
            $obj_json6 = ConvertFrom-Json -InputObject $string_json;
            $token = $obj_json6.result.accessToken

            # Obteniendo keys de runtime y authoring de Luis
            $string_response = az cognitiveservices account keys list --name $luisResource --resource-group $resource_group
            $string_json = '{ result:' + $string_response + '}';
            $obj_json7 = ConvertFrom-Json -InputObject $string_json;
            $luis_endpointKey = $obj_json7.result.key1
            
            $string_response = az cognitiveservices account keys list --name "$luisResource-Authoring" --resource-group $resource_group
            $string_json = '{ result:' + $string_response + '}';
            $obj_json8 = ConvertFrom-Json -InputObject $string_json;
            $luis_authoringKey = $obj_json8.result.key1

            $string_response = az cognitiveservices account show --name $luisResource --resource-group $resource_group
            $string_json = '{ result:' + $string_response + '}';
            $obj_json9 = ConvertFrom-Json -InputObject $string_json;
            $obj_json9
            $luis_region = $obj_json9.result.location

            write-host "Creando Json"
            if ($luisResource -eq ""){
                $profile_json = "
                {
                    accessToken: '$token',
                    name: '$nombre_bot',
                    environment: 'Composer',
                    hostname: '$nombre_bot',
                    settings: {
                        MicrosoftAppId: '$appId',
                        MicrosoftAppPassword: '$password'
                    }
                }
                "
            }
            else{
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
            $myobject | ConvertTo-Json

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

CH_MAIN