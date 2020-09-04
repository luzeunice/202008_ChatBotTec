namespace CosmosTableSamples.Model
{
    using Microsoft.Azure.Cosmos.Table;
    using System;

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

        public string estatus { get; set; }
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
        
        public DateTime fecha_evento { get; set; }
        public string nomina_sesion1 { get; set; }
        public string nomina_sesion2 { get; set; }
        public string titulo { get; set; }
        public string estatus { get; set; }
        public string idSesion { get; set; }
        public string comentarios{ get; set; }
        public string url { get; set; }


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
        public string correo { get; set; }

        public string sexo { get; set; }
    }

        public class Comentario : TableEntity
    {
        public Comentario()
        {
        }

        public Comentario(string partition, string row)
        {
            PartitionKey = partition;
            RowKey = row;
        }
        
        public string nomina { get; set; }
        public DateTime fecha_comentario { get; set; }
        public string comentario_det { get; set; }
        public string estatus { get; set; }
        

    }


}