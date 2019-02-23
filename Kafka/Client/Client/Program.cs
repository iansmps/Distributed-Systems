using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;

namespace Client
{
    class Program
    {
        static bool Receive = false;
        static int offint;

        static void Main(string[] args)
        {

            if (!File.Exists("Offset.txt"))
            {
                var stream = File.Create("Offset.txt");
                stream.Close();
                StreamWriter arq = new StreamWriter("Offset.txt");
                arq.WriteLine("0");
                arq.Close();
            }
            StreamReader file2 = new StreamReader("Offset.txt");
            offint = int.Parse(file2.ReadLine());
            file2.Close();

            ////////////////////////////////////////////////////////////////////////////////

            string topic = "ComandoTopic4";
            Uri uri = new Uri("http://localhost:9092");
            var options = new KafkaOptions(uri);
            var router = new BrokerRouter(options);
            var client = new Producer(router);

            /////////////////////////////////////////////////////////////////////////////////

            String data;
            int comand;
            int chave;
            Comando comando;
            Message msg2;

            Task threadReceive;
            threadReceive = new Task(ThreadReceive);
            threadReceive.Start();

            while (true)
            {
                if (!Receive)
                {
                    Console.WriteLine("\nQual comando(ADD-1/READ-2/UPDATE-3/DELETE-4/LISTAR-5/DESLIGAR-6/SNAPSHOT-8)");
                    if (!int.TryParse(Console.ReadLine(), out comand) || comand <= 0 || comand > 8)
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
                            msg2 = new Message(JsonConvert.SerializeObject(comando));

                            client.SendMessageAsync(topic, new List<Message> { msg2 });
                            Receive = true;

                            break;

                        case (int)Comandos.READ:

                            Console.WriteLine("Insira chave");
                            if (!int.TryParse(Console.ReadLine(), out chave))
                            {
                                Console.WriteLine("Chave inválida");
                                continue;
                            }

                            comando = new Comando(Comandos.READ, chave, "a");
                            msg2 = new Message(JsonConvert.SerializeObject(comando));
                            client.SendMessageAsync(topic, new List<Message> { msg2 });
                            Receive = true;

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

                            msg2 = new Message(JsonConvert.SerializeObject(comando));
                            client.SendMessageAsync(topic, new List<Message> { msg2 });
                            Receive = true;

                            break;

                        case (int)Comandos.DELETE:

                            Console.WriteLine("Insira chave");
                            if (!int.TryParse(Console.ReadLine(), out chave))
                            {
                                Console.WriteLine("Chave inválida");
                                continue;
                            }
                            comando = new Comando(Comandos.DELETE, chave, "c");
                            msg2 = new Message(JsonConvert.SerializeObject(comando));
                            client.SendMessageAsync(topic, new List<Message> { msg2 });
                            Receive = true;

                            break;

                        case (int)Comandos.DESLIGAR:

                            comando = new Comando(Comandos.DESLIGAR, 0, "c");
                            msg2 = new Message(JsonConvert.SerializeObject(comando));
                            client.SendMessageAsync(topic, new List<Message> { msg2 }).Wait();
                            Receive = true;

                            break;

                        case (int)Comandos.LISTAR:

                            comando = new Comando(Comandos.LISTAR, 0, "c");
                            msg2 = new Message(JsonConvert.SerializeObject(comando));
                            client.SendMessageAsync(topic, new List<Message> { msg2 });
                            Receive = true;

                            break;

                        case (int)Comandos.SNAPSHOT:

                            Console.WriteLine("Insira o tempo(segundos): ");
                            if (!int.TryParse(Console.ReadLine(), out chave))
                            {
                                Console.WriteLine("Tempo inválido");
                                continue;
                            }
                            comando = new Comando(Comandos.SNAPSHOT, chave, "c");
                            msg2 = new Message(JsonConvert.SerializeObject(comando));
                            client.SendMessageAsync(topic, new List<Message> { msg2 });
                            Receive = true;

                            break;
                    }
                }

             }


        }
        

        static void ThreadReceive()
        {
            string topic2 = "RespostaTopic";
            Uri uri2 = new Uri("http://localhost:9092");
            var options2 = new KafkaOptions(uri2);
            var router2 = new BrokerRouter(options2);

            OffsetPosition[] offsetPositions = new OffsetPosition[]
            {
                new OffsetPosition()
                {
                   Offset = offint,
                    PartitionId = 0
                }
            };

            var consumer = new Consumer(new ConsumerOptions(topic2, router2), offsetPositions);
            string chave = "";
            
            foreach (var message in consumer.Consume())
            {
                if (message.Key!=null)chave = Encoding.ASCII.GetString(message.Key);
                if (Receive)
                {
                    string json = Encoding.ASCII.GetString(message.Value);

                    Console.WriteLine("\n"+json);
                    if (chave != "1") { Receive = false; }
                }
                
                offint = offint + 1;
                using (StreamWriter file2 = new StreamWriter("Offset.txt", false))
                {
                    file2.WriteLine(offint);
                   // Console.WriteLine("Offset:" + offint);
                    file2.Close();
                }

            }
            


            
        }
    }
}

