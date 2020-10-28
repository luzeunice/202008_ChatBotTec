# ######################################################################
# >>>>>>>>>>>        Configuración de la publicación       <<<<<<<<<<<<
# ######################################################################

# Datos para el BOT
# $nombre_bot = "CH-19OCT-Bot" 
$nombre_bot = "LAShareSkills01"                                 ## !!! Obligatorio !!!###

# En qué service plan se va a publicar el app del bot
$app_service_plan = "LAShareSkills01"                                                ## !!! Obligatorio !!!###
$appServicePlanLocation = "South Central US" #"West US" # "South Central US"    ## !!! Obligatorio !!!###

# Grupo de recursos general
$resource_group = "rg_ChatBot"
#$resource_group = "rg_chbot_insc"                                                  ## !!! Obligatorio !!!###

# En caso de necesitar añadir Active Directory
$generar_AD = $false
$vector_scopes = @('openid', 'profile','User.ReadBasic.All','User.Read')

# En caso de contener LUIS (Dejar en blanco "" si no tiene Luis)
$luisResource = "LAShareSkills01"                                                

# Aun no terminado (para crear bots de linux)
$app_service_pan_OS_LINUX = $false



# $user_name = "torta@jamon.mx"
# $user_pass = "milanesa"
# 
# # Ruta proyecto COMPOSER
# $ruta_composer = "D:\5) Entregables\E17 Servicios Cognitivos\E17_3 Bots\2) Composer\202008_ChatBotTec\ch_codigo\Luis-Publicar\settings"
# $perfil_composer_publicar = "paquito"

############## No modificar ################
$Fecha = Get-Date -Format "yyyyMMdd";
Export-ModuleMember -Variable *
#_______________________________________________________________________




# "LUIS",
# "LUIS.Authoring",


# az cognitiveservices account create `
# --name "CH-Autom-borrar" `
# --kind "LUIS" `
# --location "West US" `
# --resource-group "rg_ChatBot" `
# --sku F0 `
# --subscription "8b8959ee-bc8c-4480-9126-4e7f2e1d0b20" `
# --yes `


# Si la cuenta ya tiene una cuenta gratuita se debe crear como S0

# az cognitiveservices account create `
# --name "CH-Autom-borrar" `
# --kind "LUIS" `
# --location "West US" `
# --resource-group "rg_ChatBot" `
# --sku S0 `
# --subscription "8b8959ee-bc8c-4480-9126-4e7f2e1d0b20" `
# --yes `

$nombre_bot 
$password 

# En qué service plan se va a publicar el app del bot
$app_service_plan 
$appServicePlanLocation 

# Grupo de recursos general
$resource_group 

# En caso de contener LUIS (Dejar en blanco "" si no tiene Luis)
$luisResource 

# Cuenta Azure (Dejar en blanco "" si se quiere loguear en navegador)
$user_name 
$user_pass 


