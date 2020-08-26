using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace Microsoft.BotFramework.Composer.CustomAction_AzureTable_Query
{
    /// <summary>
    /// Custom command which takes takes 2 data bound arguments (arg1 and arg2) and multiplies them returning that as a databound result.
    /// </summary>
    public class AzureTable_Query : Dialog
    {
        [JsonConstructor]
        public AzureTable_Query([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("$kind")]
        public const string Kind = "AzureTable_Query";

        [JsonProperty("Columnas")]
        public StringExpression Columnas { get; set; }

        [JsonProperty("Table_Name")]
        public StringExpression Table_Name { get; set; }

        [JsonProperty("Filtro")]
        public StringExpression Filtro { get; set; }

        [JsonProperty("resultProperty")]
        //public ArrayExpression<object> ResultProperty { get; set; }
        public StringExpression ResultProperty { get; set; }

        public override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {

            string AccountName = "stg0la0tablas";
            string AccountKey = "M0GS+KDCUELQ5UbopjaHSy8l89+s8bolqtja5hWmQOdzykCifXJLpaC2E0MVvIdsWSdtWrjkWiZRmVe0xQldTw==";
            string storageconn = $"DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={AccountKey};EndpointSuffix=core.windows.net";

            var _Table_Name = Table_Name.GetValue(dc.State);

            CloudStorageAccount storageAcc = CloudStorageAccount.Parse(storageconn);
            CloudTableClient tblclient = storageAcc.CreateCloudTableClient(new TableClientConfiguration());
            CloudTable table = tblclient.GetTableReference(_Table_Name);

            var _Columnas = Columnas.GetValue(dc.State);
            var _Filtro = Filtro.GetValue(dc.State);

            var result = Quary_to_Table(Cloud_Table: table, Filtro: _Filtro, Columnas: _Columnas);
            //Quary_to_Table(Cloud_Table: _Table_Name, Filtro: "(nomina eq 'L03143140') and(sexo eq 'M')", Columnas: "*");

            //List<object> result = new List<object> { };

            //object x = new { a = "hola", b = "mundo" };
            //object y = new { a = "aaaa", b = "bbbbb" };
            //result.add(x);
            //result.add(y);

            //var result2 = result.ToArray();


            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), result);
                //this.ResultProperty.SetValue(result2);
            }

            return dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }


        public string Quary_to_Table(CloudTable Cloud_Table, string Filtro, string Columnas)
        {
            //TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Empleado")

            // Diferentes filtros pueden ser concatenados y forman un string
            //string filter1 = TableQuery.GenerateFilterCondition("nomina", "eq", "L03143140");
            //string filter2 = TableQuery.GenerateFilterCondition("sexo", "eq", "M");
            //string combinedFilter = TableQuery.CombineFilters(filter1, TableOperators.And, filter2);
            // Un ejemplo del string formado es el siguiente:    (nomina eq 'L03143140') and(sexo eq 'M')

            // El query/string formado al final debe ser usado en la tabla
            TableQuery Custom = new TableQuery().Where(Filtro);   // Filtro = "(nomina eq 'L03143140') and (sexo eq 'M')"

            var itemlist = Cloud_Table.ExecuteQuery(Custom);

            var columna_typo = "";
            var columna_obj = "";
            // Recorriendo los renglones que regrese el Query
            foreach (DynamicTableEntity obj in itemlist)
            {
                var PartitionKey = obj.PartitionKey;
                var RowKey = obj.RowKey;
                
                if (Columnas == "*")
                {
                    // Si quiero que me regrese todas las columnas (*)
                    foreach (KeyValuePair<string, EntityProperty> prop in obj.Properties)
                    {
                        var columna_nombre = prop.Key;
                        columna_typo = prop.Value.PropertyType.ToString();
                        columna_obj = prop.Value.PropertyAsObject.ToString();
                    }
                }
                else
                {
                    // Si quiero que me regrese columnas en específico
                    string[] vec_column = new string[] { Columnas };  //Columnas = "nombre", "sexo"
                    foreach (string column in vec_column)
                    {
                        columna_typo = obj.Properties[column].PropertyType.ToString();
                        columna_obj = obj.Properties[column].PropertyAsObject.ToString();
                    }
                }

            }

            return columna_obj;

        }

    }
}
