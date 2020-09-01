
/****************************
* FUNCION PARA: 
* 
* POST: INCERTAR LOS VALORES EN LA TABLA DE COMMENTS
* UPDATE: MARCA COMO ASISTENCIA "N" EN LA TABLA DE ASISTENTES
* 
* ENTRADA: 
* Identificador ( Nomina o correo) 
* NombreTabla
* 
* SALIDA: 
* Datos Insertados
* Error
* 
* VALIDACIÓN: 
* Si no encuentra información de usuario colocar en nombre: "Sin Datos"
* 
* Creado por: José Cruz /Luz Eunice Angeles Ochoa
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


namespace comentario.usuario
{
    public static class comentario
    {
        [FunctionName("comentario")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string identificador = "";

            string responseMessageS = "Default";

            if ((string)req.Method == "POST")
            {

                log.LogInformation("C# HTTP trigger function processed a request.");



                /*fechaDate = DateTime.Parse(fecha);*/

                /* **************************
                 * Identificador 
                 ****************************/

                identificador = req.Query["identificador"];
                string comentario_det = req.Query["comentario_det"];


                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                identificador = identificador ?? data?.identificador;
                identificador = identificador.ToUpper();

                comentario_det = comentario_det ?? data?.comentario_det;
                comentario_det = comentario_det.ToUpper();


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

                CloudTable tableUsuario = tableClient.GetTableReference("usuario");
                CloudTable tableComentario = tableClient.GetTableReference("comentario");

                List<Usuario> usuario = tableUsuario.CreateQuery<Usuario>().AsQueryable<Usuario>().Where(e => e.PartitionKey == "Empleado" && (e.nomina == identificador || e.correo == identificador)).ToList();

                /*************************************************************
                 * NO EXISTE USUARIO EN BASE DE DATOS DE USUARIO
                 * ***********************************************************/
                if (usuario == null || usuario.Count() == 0)
                {
                    responseMessageS = "UsuarioNoBaseDatos";

                    return new OkObjectResult(JsonConvert.SerializeObject(new { Resultado = "UsuarioNoBaseDatos" }));
                }
                else
                {


                    try
                    {


                        string rowKey = Guid.NewGuid().ToString("N"); //This give you a unique guid with no hyphens.
                        Comentario entity = new Comentario("Comentario", rowKey);

                        entity.nomina = identificador;
                        entity.fecha_comentario = DateTime.Now;
                        entity.comentario_det = comentario_det;
                        entity.estatus = "Nuevo";


                        TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

                        // Execute the operation.
                        TableResult result = await tableComentario.ExecuteAsync(insertOrMergeOperation);
                        Comentario insertedCustomer = result.Result as Comentario;

                        responseMessageS = "Actualizado";

                    }
                    catch (Exception e)
                    {
                        log.LogInformation(e.Message);
                        return new OkObjectResult(JsonConvert.SerializeObject(new { Resultado = "ErrorUpdate" }));
                    }

                    return new OkObjectResult(JsonConvert.SerializeObject(new { Resultado =responseMessageS}));
                }



            }

            return new OkObjectResult(JsonConvert.SerializeObject(new { Resultado =responseMessageS}));
        }
    }
}
