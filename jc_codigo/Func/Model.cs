namespace CosmosTableSamples.Model
{
    using Microsoft.Azure.Cosmos.Table;
    public class Asistente : TableEntity
    {
        public Asistente()
        {
        }

        public Asistente(string partition, string row)
        {
            PartitionKey = partition;
            RowKey = row;
        }
        public string idSesion { get; set; }
        public string nomina_asistente { get; set; }
        public string fechaRegistro { get; set; }
        public string asistencia { get; set; }
    }

    public class Sesion : TableEntity
    {
        public Sesion()
        {
        }

        public Sesion(string partition, string row)
        {
            PartitionKey = partition;
            RowKey = row;
        }
        public string idSesion { get; set; }
        public string fecha { get; set; }
        public string nomina_sesion1 { get; set; }
        public string titulo { get; set; }
        public string estatus { get; set; }
        public string comentarios{get;set;}
    }
    public class Usuario : TableEntity
    {
        public Usuario()
        {
        }

        public Usuario(string partition, string row)
        {
            PartitionKey = partition;
            RowKey = row;
        }
        public string nomina { get; set; }
        public string nombre { get; set; }
        public string email { get; set; }
    }
}