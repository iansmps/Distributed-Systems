using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proto;
using Grpc.Core;

namespace Server
{
    class ProtoImpl : RPC.RPCBase
    {
        public Queue<Requisicao> filaComandos;
        public object blockComandos = new object();
        public Dictionary<Requisicao, string> listaRespostasGRPC = new Dictionary<Requisicao, string>();
        public object blockRespostasGRPC = new object();

        // Server side handler of the SayHello RPC
        public override Task<Resposta> Comando(Comand comand, ServerCallContext context)
        {
            Comando comando = new Comando((Comandos)comand.Cmd,comand.Chave,comand.Valor);
            Requisicao req = new Requisicao(context,comando);
            lock (blockComandos)
            {
                filaComandos.Enqueue(req);
            }

            return Task.FromResult(ProcessaComando(req));
        }


        public Resposta ProcessaComando(Requisicao req)
        {
            while (true)
            {
                lock (blockRespostasGRPC)
                {
                    if(listaRespostasGRPC.ContainsKey(req))
                    {
                        Resposta r= new Resposta();
                        string rep;
                        listaRespostasGRPC.TryGetValue(req, out rep);
                        r.Mensagem = rep;
                        listaRespostasGRPC.Remove(req);
                        return r;
                    }
                }
            }
        }
    }
}
