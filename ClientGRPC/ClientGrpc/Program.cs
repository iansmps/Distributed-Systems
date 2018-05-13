using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grpc.Core;
using Proto;

namespace ClientGrpc
{
    class Program
    {
        static int porta;
        static string endereco = "";
        static Comand cmd = null;
        static RPC.RPCClient client;
        static void Main(string[] args)
        {
            if (!File.Exists("portas.txt"))
            {
                var stream = File.Create("portas.txt");
                stream.Close();
                StreamWriter arq = new StreamWriter("portas.txt");
                arq.WriteLine("1700");
                arq.WriteLine("127.0.0.1");
                arq.Close();
            }

            StreamReader file = new StreamReader("portas.txt");
            porta = int.Parse(file.ReadLine());
            endereco = file.ReadLine();

            Channel channel = new Channel(endereco + ":" + porta, ChannelCredentials.Insecure);

            client = new RPC.RPCClient(channel);

            String data;
            int comand;
            int chave;

            Task task = null;

            while (true)
            {
                Console.WriteLine("Qual comando(ADD-1/READ-2/UPDATE-3/DELETE-4/LISTAR-5/DESLIGAR-6)/MONITORAR-7");
                if (!int.TryParse(Console.ReadLine(), out comand) || comand <= 0 || comand > 7)
                {
                    Console.WriteLine("Comando Inválido!");
                    continue;
                }
                switch (comand)
                {
                    case (int)Comandos.ADD:
                        Console.WriteLine("Insira chave");
                        if (!int.TryParse(Console.ReadLine(), out chave))
                        {
                            Console.WriteLine("Chave inválida");
                            continue;
                        }

                        Console.WriteLine("Insira dado");
                        data = Console.ReadLine();

                        cmd = new Comand();
                        cmd.Chave = chave;
                        cmd.Valor = data;
                        cmd.Cmd = comand;
                        task = new Task(ThreadGRPC);
                        break;
                    case (int)Comandos.READ:
                        Console.WriteLine("Insira chave");
                        if (!int.TryParse(Console.ReadLine(), out chave))
                        {
                            Console.WriteLine("Chave inválida");
                            continue;
                        }
                        cmd = new Comand();
                        cmd.Chave = chave;
                        cmd.Cmd = comand;
                        task = new Task(ThreadGRPC);
                        break;
                    case (int)Comandos.UPDATE:
                        Console.WriteLine("Insira chave");
                        if (!int.TryParse(Console.ReadLine(), out chave))
                        {
                            Console.WriteLine("Chave inválida");
                            continue;
                        }
                        Console.WriteLine("Insira dado");
                        data = Console.ReadLine();

                        cmd = new Comand();
                        cmd.Chave = chave;
                        cmd.Valor = data;
                        cmd.Cmd = comand;
                        task = new Task(ThreadGRPC);
                        break;
                    case (int)Comandos.DELETE:
                        Console.WriteLine("Insira chave");
                        if (!int.TryParse(Console.ReadLine(), out chave))
                        {
                            Console.WriteLine("Chave inválida");
                            continue;
                        }

                        cmd = new Comand();
                        cmd.Chave = chave;
                        cmd.Cmd = comand;
                        task = new Task(ThreadGRPC);
                        break;
                    case (int)Comandos.DESLIGAR:
                        break;
                    case (int)Comandos.LISTAR:
                        cmd = new Comand();
                        cmd.Cmd = comand;
                        task = new Task(ThreadListar);
                        break;
                    case (int)Comandos.MONITORAR:
                        Console.WriteLine("Insira chave");
                        if (!int.TryParse(Console.ReadLine(), out chave))
                        {
                            Console.WriteLine("Chave inválida");
                            continue;
                        }

                        cmd = new Comand();
                        cmd.Cmd = comand;
                        cmd.Chave = chave;
                        task = new Task(ThreadMonitorar);
                        break;
                }
                if(cmd != null && task != null)
                {
                    task.Start();
                }
            }
        }

        static void ThreadGRPC()
        {
            Comand comando;
            lock (cmd)
            {
                comando = cmd;
            }

            Resposta resposta = client.Comando(cmd);
            Console.WriteLine(resposta.Mensagem);
        }

        static async void ThreadListar()
        {
            Comand comando;
            lock (cmd)
            {
                comando = cmd;
            }
            using (var call = client.Listar(comando))
            {
                try
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        Resposta resp = call.ResponseStream.Current;
                        Console.WriteLine(resp.Mensagem);
                    }
                }
                catch (Exception) { Console.WriteLine("Servidor offline"); }
            }
        }

        static async void ThreadMonitorar()
        {
            Comand comando;
            lock (cmd)
            {
                comando = cmd;
            }
            using (var call = client.Monitorar(comando))
            {
                try
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        Resposta resp = call.ResponseStream.Current;
                        Console.WriteLine(resp.Mensagem);
                    }
                }
                catch (Exception) { Console.WriteLine("Servidor offline"); }
            }
        }
    }
}
