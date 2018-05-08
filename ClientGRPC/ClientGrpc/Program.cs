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

            var client = new RPC.RPCClient(channel);

            String data;
            int comand;
            int chave;
            Comand cmd = null;


            while (true)
            {
                Console.WriteLine("Qual comando(ADD-1/READ-2/UPDATE-3/DELETE-4/LISTAR-5/DESLIGAR-6)");
                if (!int.TryParse(Console.ReadLine(), out comand) || comand <= 0 || comand > 6)
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

                        break;
                    case (int)Comandos.DESLIGAR:
                        break;
                    case (int)Comandos.LISTAR:
                        break;
                }
                if(cmd != null)
                {
                    AsyncUnaryCall<Resposta> resposta = client.ComandoAsync(cmd);
                    Console.WriteLine(resposta.ResponseAsync.Result.Mensagem);
                }
                cmd = null;
            }
        }
    }
}
