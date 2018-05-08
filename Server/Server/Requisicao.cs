using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
namespace Server
{
    class Requisicao
    {
        public EndPoint Remote { get; set; }
        public Comando Comand { get; set; }
        public ServerCallContext context { get; set; }

        public Requisicao(EndPoint Remote, Comando Comand)
        {
            this.Remote = Remote;
            this.Comand = Comand;
            this.context = null;
        }

        public Requisicao(ServerCallContext context, Comando Comand)
        {
            this.Remote = null;
            this.Comand = Comand;
            this.context = context;
        }
    }
}
