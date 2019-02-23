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
        public bool HaRespostas { get; set; }
        public readonly object block = new object();

        public Queue<string> respostas = new Queue<string>();
        public int monitora;

        public Requisicao(EndPoint Remote, Comando Comand)
        {
            this.Remote = Remote;
            this.Comand = Comand;
            this.context = null;
            this.HaRespostas = true;
            this.monitora = 0;
        }

        public Requisicao(ServerCallContext context, Comando Comand)
        {
            this.Remote = null;
            this.Comand = Comand;
            this.context = context;
            this.HaRespostas = true;
            this.monitora = 0;
        }

        public bool EstaMonitorando(int chave)
        {
            if (this.monitora == chave)
                return true;
            return false;
        }
    }
}
