
/****************************
* FUNCION PARA: 
* 
* POST: INSERTA UN REGISTRO EN LA TABLA CON LA INFORMACIÓN DE LA SESION Y EL USUARIO PARA ASISTENCIA
* UPDATE: MARCA COMO ASISTENCIA "N" EN LA TABLA DE ASISTENTES
* 
* ENTRADA: 
* Identificador ( Nomina o correo) 
* 
* SALIDA: 
* Resultado: Información del usuario como nombre, nomina, correo, etc. 
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



namespace laasistentes_cancel
{
    public static class asistente_cancel
    {
        [FunctionName("asistente_cancel")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string identificador = "";

            string fecha_evento_inicio_hora = "T00:00:00.000Z";
            string fecha_evento_fin_hora = "T23:59:59.000Z";

            string fecha_evento = "2020-08-27";

            string fecha_evento_inicio = fecha_evento + fecha_evento_inicio_hora;
            string fecha_evento_fin = fecha_evento + fecha_evento_fin_hora;

            string responseMessageS = "Default";




            if ((string)req.Method == "POST")
            {

                log.LogInformation("C# HTTP trigger function processed a request.");
            


                /*fechaDate = DateTime.Parse(fecha);*/

                /* **************************
                 * Identificador 
                 ****************************/

                identificador = req.Query["identificador"];
                fecha_evento = req.Query["fecha_evento"];


                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                identificador = identificador ?? data?.identificador;
                identificador = identificador.ToUpper();

                fecha_evento = fecha_evento ?? data?.fecha_evento;

                fecha_evento_inicio = fecha_evento + fecha_evento_inicio_hora;
                fecha_evento_fin = fecha_evento + fecha_evento_fin_hora;

                var fecha_evento_inicio_date = DateTime.Parse(fecha_evento_inicio);
                var fecha_evento_fin_date    = DateTime.Parse(fecha_evento_fin);




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
                CloudTable tableAsistente = tableClient.GetTableReference("asistente");
                CloudTable tableUsuario = tableClient.GetTableReference("usuario");
                CloudTable tableSesion = tableClient.GetTableReference("sesion");

                List<Usuario> usuario = tableUsuario.CreateQuery<Usuario>().AsQueryable<Usuario>().Where(e => e.PartitionKey == "Empleado" && (e.nomina == identificador || e.correo == identificador)).ToList();

                /*************************************************************
                 * NO EXISTE USUARIO EN BASE DE DATOS DE USUARIO
                 * ***********************************************************/
                if (usuario == null || usuario.Count() == 0) 
                {
                    responseMessageS = JsonConvert.SerializeObject(new { Resultado = "UsuarioNoBaseDatos" });

                    return new OkObjectResult(responseMessageS);
                }
                else
                {
                    List<Sesion> sesion = tableSesion.CreateQuery<Sesion>().AsQueryable<Sesion>().Where(e => e.PartitionKey == "Sesiones" && e.fecha_evento>=fecha_evento_inicio_date && e.fecha_evento <= fecha_evento_fin_date).ToList();



                    /*Date(e.fecha_evento) >= fecha_evento_date.ToUniversalTime()).ToList();*/


                    /*************************************************************
                    * NO EXISTE USUARIO EN BASE DE DATOS DE USUARIO
                * ***********************************************************/

                    if (sesion == null || sesion.Count() == 0)
                    {
                        responseMessageS = JsonConvert.SerializeObject(new { Resultado = "SesionNovalida" });

                        return new OkObjectResult(responseMessageS);

                    }
                    else
                    {
                        List<Sesion> sesion_orden = sesion.OrderBy(c => c.fecha_evento).ToList();
                        List<Asistente> asistente = tableAsistente.CreateQuery<Asistente>().AsQueryable<Asistente>().Where(e => e.PartitionKey == "0" && e.idSesion == sesion_orden[0].idSesion && e.nomina_asistente == usuario[0].nomina).ToList();

                        if (asistente == null || asistente.Count() == 0)
                        {
                            responseMessageS = JsonConvert.SerializeObject(new { Resultado = "NohayInformacion" });

                            return new OkObjectResult(responseMessageS);

                        }
                        else
                        {

                            responseMessageS = JsonConvert.SerializeObject(new { Resultado = "Cencelando" });



                            try
                            {
                                Asistente entity = new Asistente();
                                entity.idSesion = asistente[0].idSesion;
                                entity.nomina_asistente = asistente[0].nomina_asistente;
                                entity.fechaRegistro = asistente[0].fechaRegistro;
                                entity.asistencia = "N";
                                entity.PartitionKey = asistente[0].PartitionKey;
                                entity.RowKey = asistente[0].RowKey;

                                // Create the InsertOrReplace table operation
                                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

                                // Execute the operation.
                                TableResult result = await tableAsistente.ExecuteAsync(insertOrMergeOperation);

                            }
                            catch (Exception e)
                            {
                                log.LogInformation(e.Message);
                                return new OkObjectResult(JsonConvert.SerializeObject(new { Resultado = "ErrorUpdate" }));
                            }

                            return new OkObjectResult(responseMessageS);
                        }



                    }



                }


            }

            return new OkObjectResult("OK");
        }
    }
}
