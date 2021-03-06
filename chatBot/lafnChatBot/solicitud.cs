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
    public static class solicitud
    {
        [FunctionName("solicitud")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string nomina ="";
            string fecha ="";
            string titulo="";
            string comentarios="";

            if ((string)req.Method=="POST"){
                nomina = req.Query["nomina"];
                fecha = req.Query["fecha"];
                titulo = req.Query["titulo"];
                comentarios = req.Query["comentarios"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                nomina = nomina ?? data?.nomina;
                fecha = fecha ?? data?.fecha;
                titulo = titulo ?? data?.titulo;
                comentarios = comentarios ?? data?.comentarios;
                DateTime fechaTemp=DateTime.Now;

                nomina=nomina.ToUpper();
                var provider = new CultureInfo("es-MX");

                if (fecha=="" && nomina ==""){
                    return new OkObjectResult(JsonConvert.SerializeObject( new {Resultado="NoNominaOFecha"}));
                }
                else if (!Regex.IsMatch(nomina.Trim(),@"(^L(\d{8})$)|^l(\d{8})$") || !Regex.IsMatch(fecha.Trim(),@"^20[0-9]{2}\-[0-1]{0,1}[0-9]{1}\-[0-3]{0,1}[0-9]{1}$")){
                    return new OkObjectResult(JsonConvert.SerializeObject( new {Resultado="FormatosIncorrectos"}));
                }

                try{
                    fechaTemp=DateTime.ParseExact(fecha, "yyyy-M-d", provider);
                    if (DateTime.Now>fechaTemp){
                        return new OkObjectResult(JsonConvert.SerializeObject( new {Resultado="SesionExpirada"}));
                    }
                }
                catch{
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
                DateTime fechaSesion=DateTime.ParseExact(fechaTemp.ToString("yyyy-MM-dd")+" "+"17:00", "yyyy-MM-dd HH:mm", null).ToUniversalTime();
                var client = new SecretClient(new Uri("https://kevaultchatbot.vault.azure.net/"), new DefaultAzureCredential(),options);
                KeyVaultSecret secret = client.GetSecret("storageTablas");
                string secretValue = secret.Value;
                CloudStorageAccount storageAccount;
                storageAccount = CloudStorageAccount.Parse(secretValue);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
                CloudTable tableAsistentes = tableClient.GetTableReference("asistente");
                CloudTable tableUsuarios = tableClient.GetTableReference("usuario");
                CloudTable tableSesiones = tableClient.GetTableReference("sesion");
                

                string rowKey = Guid.NewGuid().ToString("N"); //This give you a unique guid with no hyphens.
                Sesion entity=new Sesion("Sesiones",rowKey);
                entity.titulo=titulo;
                entity.comentarios=comentarios;
                entity.nomina_sesion1=nomina;
                entity.fecha_evento= fechaSesion;
                entity.estatus="Disponible";
                entity.idSesion=rowKey;
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

                // Execute the operation.
                TableResult result = await tableSesiones.ExecuteAsync(insertOrMergeOperation);
                Sesion insertedCustomer = result.Result as Sesion;

            }

            return new OkObjectResult(JsonConvert.SerializeObject( new {Resultado="OK"}));
        }
    }
}
