using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CosmosTableSamples.Model;
using Microsoft.Azure.Cosmos.Table;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Company.Function
{
    public static class asistentes
    {
        [FunctionName("asistentes")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string nomina ="";
            string fecha ="";
            if ((string)req.Method=="POST"){
                
                log.LogInformation("C# HTTP trigger function processed a request.");
                nomina = req.Query["nomina"];
                fecha = req.Query["fecha"];
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                nomina = nomina ?? data?.nomina;
                fecha = fecha ?? data?.fecha;
                nomina=nomina.ToUpper();
                var provider = new CultureInfo("es-MX");

                if (fecha=="" && nomina ==""){
                    return new OkObjectResult(JsonConvert.SerializeObject( new {Resultado="NoNominaOSesion"}));
                }
                else if (!Regex.IsMatch(nomina.Trim(),@"(^L(\d{8})$)|^l(\d{8})$") || !Regex.IsMatch(fecha.Trim(),@"^[0-3]{0,1}[0-9]{1}\/[0-1]{0,1}[0-9]{1}\/20[0-9]{2}$")){
                    return new OkObjectResult(JsonConvert.SerializeObject( new {Resultado="FormatosIncorrectos"}));
                }

                try{
                    DateTime fechaTemp=DateTime.ParseExact(fecha, "d/M/yyyy", provider);
                    if (DateTime.Now>fechaTemp){
                        return new OkObjectResult(JsonConvert.SerializeObject( new {Resultado="SesionExpirada"}));
                    }
                    fecha=fechaTemp.ToString("dd/MM/yyyy");
                }
                catch{
                    log.LogInformation("FALLO EN PARSE");
                    return new OkObjectResult(JsonConvert.SerializeObject( new {Resultado="FormatosIncorrectos"}));
                }
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
                var client = new SecretClient(new Uri("https://kevaultchatbot.vault.azure.net/"), new DefaultAzureCredential(),options);
                KeyVaultSecret secret = client.GetSecret("storageTablas");
                string secretValue = secret.Value;
                CloudStorageAccount storageAccount;
                storageAccount = CloudStorageAccount.Parse(secretValue);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
                CloudTable tableAsistentes = tableClient.GetTableReference("asistentes");
                CloudTable tableUsuarios = tableClient.GetTableReference("la0usuario");
                CloudTable tableSesiones = tableClient.GetTableReference("sesiones");
                List<Usuario>usuario =  tableUsuarios.CreateQuery<Usuario>().AsQueryable<Usuario>().Where(e=>e.PartitionKey=="Empleado" && e.nomina == nomina).ToList();
                List<Sesion>sesion =  tableSesiones.CreateQuery<Sesion>().AsQueryable<Sesion>().Where(e=>e.PartitionKey=="Sesiones" && e.fecha == fecha && e.estatus=="Reservada").ToList();
                if (usuario.Count==1 && sesion.Count==1){
                    List<Asistente>asistentes=tableAsistentes.CreateQuery<Asistente>().AsQueryable<Asistente>().Where(e=>e.PartitionKey==sesion[0].idSesion && e.RowKey == nomina).ToList();
                    if (asistentes.Count>0){
                        return new OkObjectResult(JsonConvert.SerializeObject( new {Resultado="YaRegistrado"}));
                    }
                    try{
                        Asistente entity=new Asistente();
                        entity.idSesion=sesion[0].idSesion;
                        entity.nomina_asistente=nomina;
                        entity.fechaRegistro=DateTime.Now.ToString("dd/MM/yyyy");
                        entity.asistencia="S";
                        entity.PartitionKey=entity.idSesion;
                        entity.RowKey=nomina;
                        // Create the InsertOrReplace table operation
                        TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

                        // Execute the operation.
                        TableResult result = await tableAsistentes.ExecuteAsync(insertOrMergeOperation);
                        
                    }
                    catch (Exception e){
                        log.LogInformation(e.Message);
                        return new OkObjectResult(JsonConvert.SerializeObject( new {Resultado="ErrorInsert"}));
                    }
                }
                else{
                    return new OkObjectResult(JsonConvert.SerializeObject( new {Resultado="NoNominaOSesion"}));
                }
                
            }
            return new OkObjectResult(JsonConvert.SerializeObject( new {Resultado="OK"}));
        }
    }
}
