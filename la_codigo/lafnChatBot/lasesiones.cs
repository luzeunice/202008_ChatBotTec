/***************************************************
* FUNCION PARA 
* GET: OBTENER 3 SESIONES SIGUIENTES
* 
* ENTRADA: 
* Ninguna
* 
* SALIDA: 
* Resultado1 : Detalle del evento1
* Resultado2 : Detalle del evento2
* Resultado3 : Detalle del evento3
* 
* VALIDACIÓN: 
* Si no hay información en la sesión: Colocar nulls
* 
* Creado por: Luz Eunice Angeles Ochoa 
* Fecha: 26 de agosto de 2020
*******************************************************/

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
    public static class lasesiones
    {
        [FunctionName("lasesiones")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            string identificador = "";

            string fechaHoy = DateTime.Now.ToString();
            var detalleFecha = fechaHoy.ToString();

            if ((string)req.Method == "POST")
            {

            }

            /****************************
            * GET
            ****************************/
            if ((string)req.Method == "GET")
            {
                log.LogInformation("C# HTTP trigger function processed a request POST.");

                /* **************************
                 * Información no necesaria
                 
                identificador = req.Query["identificador"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                identificador = identificador ?? data?.identificador;
                ****************************/

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
                CloudTable tableSesiones = tableClient.GetTableReference("la0sesiones");



                /****************************
                * OBTENER LA INFORMACIÓN 
                ****************************/

                
                List<Sesion> sesiones = tableSesiones.CreateQuery<Sesion>().AsQueryable<Sesion>().Where(e => e.PartitionKey == "Sesiones" && e.fecha_evento >= DateTime.UtcNow).ToList();
                
                /*
                List<Sesion> sesiones = tableSesiones.CreateQuery<Sesion>().AsQueryable<Sesion>().Where(e => e.PartitionKey == "Sesiones" ).ToList();
                */
                List<Sesion> sesiones_orden = sesiones.OrderBy(c => c.fecha_evento).ToList();

                var sesionVacia = new List<Sesion>() { new Sesion() { titulo = "Sin Próximas Sesiones Planeadas" } };

                if (sesiones.Count >= 3 )
                {
                    string responseMessageS = JsonConvert.SerializeObject(new { Resultado1 = sesiones_orden[0] , Resultado2 = sesiones_orden[1], Resultado3 = sesiones_orden[2] });
                      
                    return new OkObjectResult(responseMessageS);
                }

                if (sesiones.Count == 2)
                {
                    string responseMessageS = JsonConvert.SerializeObject(new { Resultado1 = sesiones_orden[0], Resultado2 = sesiones_orden[1], Resultado3 = sesionVacia[0] });

                    return new OkObjectResult(responseMessageS);
                }

                if (sesiones.Count == 1)
                {
                    string responseMessageS = JsonConvert.SerializeObject(new { Resultado1 = sesiones_orden[0], Resultado2 = sesionVacia[0], Resultado3 = sesionVacia[0] });

                    return new OkObjectResult(responseMessageS);
                }
                else
                {
                    string responseMessageS = JsonConvert.SerializeObject(new { Resultado1 = sesionVacia[0], Resultado2 = sesionVacia[0], Resultado3 = sesionVacia[0] });

                    return new OkObjectResult(responseMessageS);
                }


            }
            string responseMessage = string.IsNullOrEmpty(identificador)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {detalleFecha}. This HTTP triggered function executed successfully.";

            return new OkObjectResult("OK");
        }
        
    }
}
