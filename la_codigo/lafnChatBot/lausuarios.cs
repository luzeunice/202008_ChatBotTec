
/****************************
* ****** FUNCION PARA: 
* GET: 
* CONSULTAR LA INFORMACIÓN DE USUARIO
* 
* ******ENTRADA: 
* 
* GET:
* Identificador ( Nomina o correo) 
* 
* ******SALIDA: 
* GET:
* Resultado: Información del usuario como nombre, nómina, correo, etc. 
* 
* ******VALIDACIÓN: 
* GET:
* Si no encuentra información de usuario colocar en nombre: "Sin Datos"
* Se cambia a mayúsculas: indetificador
* 
* Creado por: Luz Eunice Angeles Ochoa 
* Fecha: 26 de agosto de 2020
****************************/

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CosmosTableSamples.Model;


using Azure.Identity;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;


namespace lafnChatBot
{
    public static class lausuarios
    {
        [FunctionName("lausuarios")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            string identificador = "";

            if ((string)req.Method == "POST")
            {

            }

            /****************************
            * GET
            ****************************/
            if ((string)req.Method == "GET")
            {


                log.LogInformation("C# HTTP trigger function processed a request GET.");
                
                /* **************************
                 * Identificador 
                 ****************************/ 

                identificador = req.Query["identificador"];
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                identificador = identificador ?? data?.identificador;
                identificador = identificador.ToUpper();

                /* **************************
                 * Conexion Key Vault 
                 Referencia: https://docs.microsoft.com/en-us/azure/key-vault/general/tutorial-net-virtual-machine
                 ****************************/
                SecretClientOptions options = new SecretClientOptions()
                {
                    Retry =
                    {
                        Delay= TimeSpan.FromSeconds(2),
                        MaxDelay = TimeSpan.FromSeconds(16),
                        MaxRetries = 5,
                        Mode = RetryMode.Exponential
                    }
                };

                var client = new SecretClient(new Uri("https://kevaultchatbot.vault.azure.net/"), new DefaultAzureCredential(), options);
                KeyVaultSecret secret = client.GetSecret("storageTablas");
                string secretValue = secret.Value;
                CloudStorageAccount storageAccount;
                storageAccount = CloudStorageAccount.Parse(secretValue);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());


                /* **************************
                 * Conexion a la tabla de usuarios 
                * **************************/

                CloudTable tableUsuarios = tableClient.GetTableReference("la0usuario");
                List<Usuario> usuario = tableUsuarios.CreateQuery<Usuario>().AsQueryable<Usuario>().Where(e => e.PartitionKey == "Empleado" && ( e.nomina == identificador || e.correo == identificador)).ToList();
               
                var usuarioVacio = new List<Usuario>() { new Usuario() { nombre = "Sin Datos" } };

                /* **************************
                * Si tiene registros regresar la información , sino colocar en el nombre="Sin Datos"
                 **************************/

                if (usuario.Count >= 1 )
                {
                        return new OkObjectResult(JsonConvert.SerializeObject(new { Resultado = usuario[0] }));
                }
                else 
                {
                        return new OkObjectResult(JsonConvert.SerializeObject(new { Resultado = usuarioVacio[0] })); 
                }

                }
            string responseMessage = "OK";

            return new OkObjectResult(responseMessage);
        }
    }
}
