using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace Server
{
    class Program
    {
        /// <summary>Objeto para controlar o bloqueio da filaComandos </summary>
        static readonly object blockComandos = new object();
        /// <summary>Objeto para controlar o bloqueio da filaProcessa </summary>
        static readonly object blockProcessa = new object();
        /// <summary>Objeto para controlar o bloqueio da filaLog </summary>
        static readonly object blockLog = new object();
        
        
        /// <summary>Fila de comandos recebidos</summary>
        static Queue<Requisicao> filaComandos = new Queue<Requisicao>();
        /// <summary>Fila de comandos para serem processados</summary>
        static Queue<Requisicao> filaProcessa = new Queue<Requisicao>();
        /// <summary>Fila de comandos que serão gravados no log</summary>
        static Queue<Requisicao> filaLog = new Queue<Requisicao>();

        /// <summary>Mapa</summary>
        static Dictionary<long, String> Comand = new Dictionary<long, string>();

        /// <summary>
        /// Thread principal do servidor, cria as outras threads e é responsável por receber os comandos e os escrever em filaComandos.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //Socket
            int receivedDataLength;
            byte[] data = new byte[1400];
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1500);

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.Bind(ip);

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Remote = (EndPoint)(sender);


            //Threads
            Task threadComandos = new Task(ThreadComandos);
            Task threadProcessaComando = new Task(ThreadProcessaComando);

            threadComandos.Start();
            threadProcessaComando.Start();
            
            while (true)
            {
                data = new byte[1400];
                receivedDataLength = socket.ReceiveFrom(data, ref Remote);

                string json = Encoding.ASCII.GetString(data, 0, receivedDataLength);
                Comando comando = JsonConvert.DeserializeObject<Comando>(json);
                Requisicao req = new Requisicao(Remote, comando);

                lock (blockComandos)
                {
                    filaComandos.Enqueue(req);
                }
                //socket.SendTo(data, receivedDataLength, SocketFlags.None, Remote);
            }
        }

        /// <summary>
        /// Thread que pega os comandos de filaComandos e os escreve em filaProcessa e filaLog.
        /// </summary>
        static void ThreadComandos()
        {
            Requisicao req;
            while (true)
            {
                lock (blockComandos)
                {
                    if (filaComandos.Count > 0)
                    {
                        req = filaComandos.Dequeue();
                        lock (filaProcessa)
                        {
                            filaProcessa.Enqueue(req);
                        }

                        lock (filaLog)
                        {
                            filaLog.Enqueue(req);
                        }
                    }
                }
                
            }
        }

        /// <summary>
        /// Thread que pega os comandos de filaLog e os escreve em disco.
        /// </summary>
        static void ThreadLogaDisco()
        {

        }

        /// <summary>
        /// Thread que pega os comandos de filaProcessa os processa e envia o resultado para o cliente.
        /// </summary>
        static void ThreadProcessaComando()
        {
            Requisicao req;
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1600);

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.Bind(ip);

            while (true)
            {
                lock (blockProcessa)
                {
                    if (filaProcessa.Count > 0)
                    {
                        req = filaProcessa.Dequeue();

                        switch (req.Comand.comand)
                        {
                            case (int)Comandos.ADD:
                                Comand.Add(req.Comand.Chave, req.Comand.Valor);
                                break;

                            case (int)Comandos.UPDATE:
                                if (Comand.ContainsKey(req.Comand.Chave))
                                    Comand[req.Comand.Chave] = req.Comand.Valor;
                                break;

                            case (int)Comandos.READ:
                                string data;
                                
                                if (!Comand.TryGetValue(req.Comand.Chave, out data))
                                    continue;
                                byte[] dados = Encoding.ASCII.GetBytes(data);
                                socket.SendTo(dados, dados.Length, SocketFlags.None, req.Remote);
                                break;

                            case (int)Comandos.DELETE:
                                break;
                        }
                    }
                }
            }
        }
    }
}
