# Locales
import shutil
import os
import json

# Externas
import in_place  # pip install in-place
import unidecode # pip install unidecode

#################################################################################
##################         Variables del custom action         ##################
#################################################################################

# Nombre y descripción de las nuevas custom activities a crear
vec_custom_actions = ["Consultar Base de Datos", "GET API ZOOM", "POST API ZOOM", "Insertar Base de Datos", "Lo que quieras"]
vec_description = ["Descripción 1", "Descripción 2", "Descripción 3", "Descripción 4", "Descripción 5"]

# Toma en cuenta que debes activar el Runtime en el proyecto, antes de ejecutar el código
Proyecto_Composer_Destino = r'D:\5) Entregables\E17 Servicios Cognitivos\E17_3 Bots\2) Composer\DescargaM\Bot-Test-DEMO'

# No cambiar por el momento
dir_code_prin = "azurewebapp"
#################################################################################
##################             Ejecutando el código            ##################
#################################################################################

def generando_codigo_base():
    print("     Generando Código Base")
    # Copiando el folder
    if os.path.isdir(folder_name):
        shutil.rmtree(path = folder_name)
    # if not os.path.isdir(folder_name):
    shutil.copytree(src= 'customaction', dst= folder_name)

    # Cambiando el nombre de los archivos
    sln_file = f"{folder_name}/CustomAction.sln"
    project_file = f"{folder_name}/Microsoft.BotFramework.Composer.CustomAction.csproj"

    x = open(sln_file, "rt")
    data = x.read()
    data = data.replace('Microsoft.BotFramework.Composer.CustomAction.csproj', f'Microsoft.BotFramework.Composer.CustomAction_{CustomAction_Name}.csproj')
    x.close()

    x = open(sln_file, "wt")
    x.write(data)
    x.close()

    os.rename(sln_file, f"{folder_name}/CustomAction_{CustomAction_Name}.sln")    
    os.rename(project_file, f"{folder_name}/Microsoft.BotFramework.Composer.CustomAction_{CustomAction_Name}.csproj")  

    # Archivos a modificar
    vec_files=[
        f"{folder_name}/Action/MultiplyDialog.cs",
        f"{folder_name}/CustomActionComponentRegistration.cs"
        ]

    # Leyendo los archivos y reemplazando datos
    for _file in vec_files:
        ca_file = open(_file, "rt")
        data = ca_file.read()
        data = data.replace('CustomActionComponentRegistration', f'CustomComponentRegistration_{CustomAction_Name}')
        data = data.replace('CustomAction', f'CustomAction_{CustomAction_Name}')
        data = data.replace('MultiplyDialog', CustomAction_Name)
        ca_file.close()

        # Sobreescribiendo el archivo con los datos cambiados
        new_ca_file = open(_file, "w")
        new_ca_file.write(data)
        new_ca_file.close()
        

def generando_instrucciones():
    print("     Generando Instrucciones")
    file_name = f"_{CustomAction_Name}.txt"
    instruc = f"""<ProjectReference Include="..\\{folder_name}\\Microsoft.BotFramework.Composer.CustomAction_{CustomAction_Name}.csproj" />
using Microsoft.BotFramework.Composer.CustomAction_{CustomAction_Name};
ComponentRegistration.Add(new CustomComponentRegistration_{CustomAction_Name}());
    """
    file_instruc = open(f"{file_name}", "w")
    file_instruc.write(instruc)
    file_instruc.close() 
    return file_name    

def generando_json_base():
    print("     Generando Json Base")
    file_name = f"{folder_name}/Schemas/schema.json"
    # file_name = f"schema.json"

    data= {
        "$schema": "https://raw.githubusercontent.com/microsoft/botframework-sdk/master/schemas/component/component.schema",
        "$role": "implements(Microsoft.IDialog)",
        "title":  f"{CustomAction_Name.replace('_', ' ')}",
        "description": f"{description}",
        "type": "object",
        "additionalProperties": False,
        "properties": {
            "arg1": {
                "$ref": "#/definitions/integerExpression",
                "title": "Arg1",
                "description": "Value from callers memory to use as arg 1"
            },
            "arg2": {
                "$ref": "#/definitions/integerExpression",
                "title": "Arg2",
                "description": "Value from callers memory to use as arg 2"
            },
            "resultProperty": {
                "$ref": "#/definitions/stringExpression",
                "title": "Result",
                "description": "Value from callers memory to store the result"
            }
        }
    }
    
    # Ordenandolo y escribiendolo
    preety = json.dumps(data,indent=4)
    new_ca_file = open(file_name, "wt")
    new_ca_file.write(preety)
    new_ca_file.close()
    return preety
        
def merge_jsons(preety):
    print("     Combinando Esquemas")
    nombre = "sdk"
    file_name = f"{nombre}.schema"

    # Leyendo el schema principal
    ca_file = open(f"{path_schema}/{file_name}", "rt")
    json_string = ca_file.read()
    ca_file.close()

    # Creando copia de restauración
    schema_file = open(f"{path_schema}/{nombre}-back-{CustomAction_Name}.schema", "wt")
    schema_file.write(json_string)
    schema_file.close()   

    json_schema = json.loads(json_string)
    json_data = json.loads(preety)

    vec_new_custom_action = [{'$ref': f'#/definitions/{CustomAction_Name}'}]
    # Añadiendo nuevo valor
    json_schema["definitions"]["Microsoft.IDialog"]["oneOf"].extend(vec_new_custom_action)
    json_schema["definitions"][f"{CustomAction_Name}"] = json_data
    
    # # Añadiendo nuevo valor con verificacion
    # for key1 in json_schema:
    #     if key1.lower() == "definitions":
    #         json_temp = json_schema[key1]
    #         for key2 in json_temp:
    #             if key2.lower() == "microsoft.idialog":
    #                 json_temp2 = json_temp[key2]
    #                 for key3 in json_temp2:
    #                     if key3.lower() == "oneof":
    #                         json_temp2[key3].extend(vec_new_custom_action)

    preety_final_schema = json.dumps(json_schema,indent=4)
    # print(preety)
    schema_file = open(f"{path_schema}/{file_name}", "wt")
    schema_file.write(preety_final_schema)
    schema_file.close()              

def moviendo_codigo_instru(file_instr_name):
    print("     Moviendo el codigo al proyecto")
    # Borrando directorio y archivo si existen
    if os.path.isdir(f"{path_runtime}/{folder_name}"):
        shutil.rmtree(path = f"{path_runtime}/{folder_name}")
    if os.path.isfile(f"{path_runtime}/{file_instr_name}"):
        os.remove(f"{path_runtime}/{file_instr_name}")

    # Moviendo
    shutil.move(folder_name, f"{path_runtime}/{folder_name}")
    shutil.move(file_instr_name, path_runtime)

def modificando_codigo_principal():
    print("     Añadiendo el código al proyecto principal")
    path_principal= f"{path_runtime}/{dir_code_prin}"
    csproj = "Microsoft.BotFramework.Composer.WebApp.csproj"
    startup = "Startup.cs"

    ################################################################################################
    ca_file = open(f"{path_principal}/{csproj}", "rt")
    data = ca_file.read()
    ca_file.close()

    if not folder_name in data:
        bandera1 = None
        bandera1 = None
        bandera2 = None
        with in_place.InPlace(f"{path_principal}/{csproj}") as file_proy:
            for line in file_proy:
                
                if "ProjectReference" in line:
                    bandera1 = True

                if bandera1 and "/ItemGroup" in line:
                    bandera2 = True
                
                # Insertando la nueva linea
                if bandera1 and bandera2:
                    string = f'    <ProjectReference Include="..\\{folder_name}\\Microsoft.BotFramework.Composer.CustomAction_{CustomAction_Name}.csproj" />\n'
                    file_proy.write(string)  
                    bandera1 = False
                    bandera2 = False
                # Insertando linea común
                file_proy.write(line)

    ################################################################################################
    ca_file = open(f"{path_principal}/{startup}", "rt")
    data = ca_file.read()
    ca_file.close()     
    string1 = f"using Microsoft.BotFramework.Composer.CustomAction_{CustomAction_Name};\n"
    string2 = f"            ComponentRegistration.Add(new CustomComponentRegistration_{CustomAction_Name}());\n"
    
    vec_using = [string1]
    vec_component_reg = []
    if not string1 in data:
        ca_file = open(f"{path_principal}/{startup}", "rt")
        Lines = ca_file.readlines()  
        ca_file.close()     
        
        
        # Encontrando/almacenando using y component_registration
        for line in Lines:
            if "using Microsoft.BotFramework.Composer" in line:
                vec_using.append(line)
            if "ComponentRegistration.Add" in line:
                vec_component_reg.append(line)
        # vec_using.sort(key=len)

        vec_using.sort(key=lambda s: s.replace('.', ''))
        # vec_using.sort(key=lambda item: (-len(item), item))
        # vec_component_reg.append(string2)
        
        normal1 = False
        ignorar = False
        check = False
        with in_place.InPlace(f"{path_principal}/{startup}") as file_proy:
            for line in file_proy:   
                # Escribiendo usings
                if "using Microsoft.BotFramework.Composer" in line:
                    if check == False:
                        for string_ in vec_using:
                            if "//" in string_:
                                file_proy.write("\n")
                            file_proy.write(string_)
                        file_proy.write("\n")
                        check = True
                    ignorar = True
                else:
                    ignorar = False

                if "namespace" in line:
                    file_proy.write("\n")
                    normal1 = True
                    

                if ignorar == False:
                    if not line == "\n" and normal1 == False:
                        file_proy.write(line)
                    if normal1:
                        file_proy.write(line)
                    
                    if vec_component_reg[-1] in line:
                        file_proy.write(string2)  


                # if "namespace" in line:
                #     for string_ in vec_using:
                #         file_proy.write(string_)        
                #     normal1 = True
                # Escribiendo resto de codigo
                # if normal1:
                #     file_proy.write(line)
                #     if vec_component_reg[-1] in line:
                #         file_proy.write(string2)  
    else:
        print("         *** La Custom Action ya se encuentra agregado al código principal")

    
            

    #ca_file = open(f"{path_principal}/{startup}", "rt")
    # line = line.replace('test', 'testZ')
               
def main():
    generando_codigo_base()
    file_instr_name = generando_instrucciones()
    data = generando_json_base()
    merge_jsons(data)
    moviendo_codigo_instru(file_instr_name)
    modificando_codigo_principal()


# Validando si existe la carpeta
path_runtime = Proyecto_Composer_Destino + "/runtime/"
path_schema = Proyecto_Composer_Destino + "/schemas/"
if not os.path.isdir(path_runtime):
    print("Lo siento debes configurar el runtime del proyecto antes.")  
else:
    cont=0
    for name in vec_custom_actions:
        
        # Eliminando espacios en blanco y acentos
        name = name.replace(' ','_')
        name = unidecode.unidecode(name)

        CustomAction_Name = name
        folder_name = f'Custom_{CustomAction_Name}'

        description = vec_description[cont]
        cont += 1

        print("Aregando ",folder_name)
        main()
        print("")
    
    
    # Compilando el código para verificar si existen errores
    # resultado = os.system(r'cd "D:\5) Entregables\E17 Servicios Cognitivos\E17_3 Bots\2) Composer\202008_ChatBotTec\ch_codigo\El_buenas\runtime\azurewebapp" & dotnet build')
    print("\n\n########################################################")
    print("##############      Compilando Código     ##############")
    print("########################################################\n\n")
    path_compile = f"{path_runtime}{dir_code_prin}"
    unidad = path_compile[:2]
    resultado = os.system(f'{unidad} & cd "{path_compile}" & dotnet build')
    print(resultado)

#  dotnet build;
# "ls C:\Users\L03143140; exit 0", stderr=subprocess.STDOUT, shell=True


# Otra forma de validar
# archivos_carpetas = os.listdir(Proyecto_Composer_Destino)
# print("runtime" in archivos_carpetas)


# D:\5) Entregables\E17 Servicios Cognitivos\E17_3 Bots\2) Composer\202008_ChatBotTec\ch_codigo\El_buenas\runtime\azurewebapp
# instruc = f"""
#     Insertar en el proyecto:
#     <ProjectReference Include="..\\{folder_name}\\Microsoft.BotFramework.Composer.CustomAction_{CustomAction_Name}.csproj" />

#     Insertar en startup
#     using Microsoft.BotFramework.Composer.CustomAction_{CustomAction_Name};
#     ComponentRegistration.Add(new CustomComponentRegistration_{CustomAction_Name}());
#     """




##########################################################


    # tree = ET.parse(f"{path_principal}/{csproj}")
    # root = tree.getroot()
    # # for child in root:
    # #     print(child.tag, child.attrib)
    
    # for ItemGroup in root.iter('ItemGroup'):
    #     for x in ItemGroup:
    #         if x.tag == "ProjectReference":
    #             # ItemGroup.set("hola","mundo")
    #             new_dec = ET.SubElement(ItemGroup, 'ProjectReference2')
    #             new_dec.attrib["Include"] = "fff" #"..\\{folder_name}\\Microsoft.BotFramework.Composer.CustomAction_{CustomAction_Name}.csproj"

    # # for ItemGroup in root.iter('ItemGroup'):
    # #     for x in ItemGroup:
    # #         if x.tag == "ProjectReference":
    # #             print(x.tag,x.attrib)

    # tree.write(file_or_filename="_______test_______.csproj")
