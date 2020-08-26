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

            if ((string)req.Method == "POST")
            {

            }

            if ((string)req.Method == "POST")
            {
                log.LogInformation("C# HTTP trigger function processed a request POST.");
                identificador = req.Query["identificador"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                identificador = identificador ?? data?.identificador;


                var account = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=stg0la0tablas;AccountKey=M0GS+KDCUELQ5UbopjaHSy8l89+s8bolqtja5hWmQOdzykCifXJLpaC2E0MVvIdsWSdtWrjkWiZRmVe0xQldTw==;EndpointSuffix=core.windows.net");
                var client = account.CreateCloudTableClient();
                var tableUsuarios = client.GetTableReference("la0sesiones");

                List<Sesion> sesiones = tableUsuarios.CreateQuery<Sesion>().AsQueryable<Sesion>().Where(e => e.PartitionKey == "Sesiones" && e.fecha_evento>= DateTime.UtcNow ).ToList();
                List<Sesion> sesiones_orden = sesiones.OrderBy(c => c.fecha_evento).ToList();

                if (sesiones.Count >= 3 )
                {
                    string responseMessageS = JsonConvert.SerializeObject(new { Resultado1 = sesiones_orden[0] , Resultado2 = sesiones_orden[1], Resultado3 = sesiones_orden[2] });
                      
                    return new OkObjectResult(responseMessageS);
                }

                if (sesiones.Count == 2)
                {
                    string responseMessageS = JsonConvert.SerializeObject(new { Resultado1 = sesiones_orden[0], Resultado2 = sesiones_orden[0] });

                    return new OkObjectResult(responseMessageS);
                }

                if (sesiones.Count == 1)
                {
                    string responseMessageS = JsonConvert.SerializeObject(new { Resultado1 = sesiones_orden[0] });

                    return new OkObjectResult(responseMessageS);
                }



            }
            string responseMessage = string.IsNullOrEmpty(identificador)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {(string)req.Method}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
