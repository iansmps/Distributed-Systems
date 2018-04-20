using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.IO;

namespace Client
{
    class Program
    {
        static int porta;
        static readonly object threadBlock = new object();
        static bool threadOn = false;

        static void Main(string[] args)
        {
            if (!File.Exists("portas.txt"))
            {
                var stream = File.Create("portas.txt");
                stream.Close();
                StreamWriter arq = new StreamWriter("portas.txt");
                arq.WriteLine("1500");
                arq.Close();
            }

            StreamReader file = new StreamReader("portas.txt");

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            porta = int.Parse(file.ReadLine());

            IPAddress server = IPAddress.Parse("127.0.0.1");
            IPEndPoint ep = new IPEndPoint(server, porta);

            String data;
            int comand;
            int chave;
            Comando comando;
            byte[] sendbuf = new byte[1450];

            Task threadReceive;
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

                        comando = new Comando(Comandos.ADD, chave, data);
                        sendbuf = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(comando));
                        if(sendbuf.Length > 1450)
                        {
                            Console.WriteLine("Tamanho de string inválida!");
                            continue;
                        }
                        s.SendTo(sendbuf, ep);
                        break;
                    case (int)Comandos.READ:
                        Console.WriteLine("Insira chave");
                        if (!int.TryParse(Console.ReadLine(), out chave))
                        {
                            Console.WriteLine("Chave inválida");
                            continue;
                        }
                            
                        comando = new Comando(Comandos.READ, chave, "a");
                        sendbuf = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(comando));
                        s.SendTo(sendbuf, ep);
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
                        comando = new Comando(Comandos.UPDATE, chave, data);
                        sendbuf = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(comando));
                        if (sendbuf.Length > 1450)
                        {
                            Console.WriteLine("Tamanho de string inválida!");
                            continue;
                        }
                        s.SendTo(sendbuf, ep);
                        break;
                    case (int)Comandos.DELETE:
                        Console.WriteLine("Insira chave");
                        if (!int.TryParse(Console.ReadLine(), out chave))
                        {
                            Console.WriteLine("Chave inválida");
                            continue;
                        }
                        comando = new Comando(Comandos.DELETE, chave, "c");
                        sendbuf = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(comando));
                        s.SendTo(sendbuf, ep);
                        break;
                    case (int)Comandos.DESLIGAR:
                        comando = new Comando(Comandos.DESLIGAR, 0, "c");
                        sendbuf = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(comando));
                        s.SendTo(sendbuf, ep);
                        break;
                    case (int)Comandos.LISTAR:
                        comando = new Comando(Comandos.LISTAR, 0, "c");
                        sendbuf = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(comando));
                        s.SendTo(sendbuf, ep);
                        break;
                }
                lock (threadBlock)
                {
                    if (!threadOn)
                    {
                        threadOn = true;
                        threadReceive = new Task(() => ThreadReceive(s));
                        threadReceive.Start();
                    }
                }
            }
        }

        static void ThreadReceive(Socket s)
        {
            byte[] receive;
            while (threadOn)
            {
                receive = new byte[1400];
                try
                {
                    s.Receive(receive);
                    string recebe = Encoding.UTF8.GetString(receive).TrimEnd('\0');

                    Console.WriteLine(recebe);
                }
                catch(SocketException)
                {
                    lock (threadBlock)
                    {
                        threadOn = false;
                    }
                    Console.WriteLine("Servidor offline");
                }
            }
        }
    }
}
