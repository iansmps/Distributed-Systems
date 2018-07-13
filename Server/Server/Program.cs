using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;

namespace Server
{
    class Program
    {
        static readonly object blockLog = new object();

        /// <summary>Mapa</summary>
        static Dictionary<long, String> Mapa = new Dictionary<long, string>();

        static bool desliga = false;
        static int offint;
        static int segundos = 10;
        static int snap = 0;
        static readonly object blockSegundos = new object();

        /// <summary>
        /// Thread principal do servidor, cria as outras threads.
        /// </summary>
        static void Main(string[] args)
        {

            if (!File.Exists("portas.txt"))
            {
                var stream = File.Create("portas.txt");
                stream.Close();
                StreamWriter arq = new StreamWriter("portas.txt");
                arq.WriteLine("0");
                arq.Close();
            }

            StreamReader file = new StreamReader("portas.txt");
            offint = int.Parse(file.ReadLine()); //Recupera offset
            Console.WriteLine(offint);
            file.Close();

            RecuperaMapa();
            SalvaSnapshot();

            //Threads
            Task threadProcessaComando = new Task(ThreadProcessaComando);
            Task threadSnapshot = new Task(ThreadSnapshot);

            threadProcessaComando.Start();
            threadSnapshot.Start();

            while (!desliga)
            {
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        static public void RecuperaMapa()
        {
            Comando comando;
            string resposta = "";

            for (int i = 1; i <= 3; i++)
            {
                if (File.Exists("snapshot" + i + ".txt"))
                {
                    snap = i;
                }
            }

            if (snap == 0)
            {
                var stream = File.Create("snapshot1.txt");
                snap = 1;
                stream.Close();
            }
            else
            {
                if(snap == 3 && File.Exists("snapshot1.txt"))
                {
                    snap = 1;
                }
            }

            using (StreamReader file = new StreamReader("snapshot"+snap+".txt"))
            {
                while(!file.EndOfStream)
                {
                    comando = JsonConvert.DeserializeObject<Comando>(file.ReadLine());
                    ProcessaComando(comando, ref resposta);
                }
                file.Close();
            }

            using (StreamReader file = new StreamReader("json.txt"))
            {
                while (!file.EndOfStream)
                {
                    comando = JsonConvert.DeserializeObject<Comando>(file.ReadLine());
                    ProcessaComando(comando, ref resposta);
                }
                file.Close();
            }
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////////
        static public void ProcessaComando(Comando comando, ref string resposta)
        {
            switch (comando.comand)
            {
                case (int)Comandos.ADD:
                    if (Mapa.ContainsKey(comando.Chave))
                    {
                        resposta = "Não foi possível inserir o item, chave já existente.";
                    }
                    else
                    {
                        resposta = "Inserido com sucesso.";
                        Mapa.Add(comando.Chave, comando.Valor);
                    }
                    break;

                case (int)Comandos.UPDATE:

                    if (Mapa.ContainsKey(comando.Chave))
                    {
                        resposta = "Atualizacao efetuada com sucesso.";
                        Mapa[comando.Chave] = comando.Valor;
                    }
                    else
                    {
                        resposta = "Não foi possível atualizar, elemento inexistente.";
                    }
                    break;

                case (int)Comandos.READ:
                    string data;

                    if (!Mapa.TryGetValue(comando.Chave, out data))
                    {
                        resposta = "Chave nao encontrada, elemento inexistente.";
                    }
                    else
                    {
                        resposta = data;
                    }
                    break;

                case (int)Comandos.DELETE:
                    if (Mapa.ContainsKey(comando.Chave))
                    {
                        Mapa.Remove(comando.Chave);
                        resposta = "Remocao efetuada com sucesso.";
                    }
                    else
                    {
                        resposta = "Não foi possível remover, elemento inexistente.";
                    }
                    break;
                case (int)Comandos.DESLIGAR:
                    resposta = "Servidor Desligado";
                    desliga = true;
                    break;
                case (int)Comandos.SNAPSHOT:
                    lock (blockSegundos)
                    {
                        if (comando.Chave >= 10)
                        {
                            segundos = comando.Chave;
                            resposta = "Tempo do snapshot atualizado com sucesso para o valor de: " + comando.Chave;
                        }
                        else
                        {
                            resposta = "Não é possível gravar o snapshot com tempo menor de 10 segundos.";
                        }
                        
                    }
                    break;
            }
        }


        /// Thread que pega os comandos do topico do kafka os processa e envia o resultado para o cliente.///
        static void ThreadProcessaComando()
        {
            //////////////////////////////////////////////
            string topic = "ComandoTopic4";
            Uri uri = new Uri("http://localhost:9092");
            var options = new KafkaOptions(uri);
            var router = new BrokerRouter(options);

            string topic2 = "RespostaTopic";
            Uri uri2 = new Uri("http://localhost:9092");
            var options2 = new KafkaOptions(uri);
            var router2 = new BrokerRouter(options);
            var produce = new Producer(router2);
            //////////////////////////////////////////////

            //Seta o offset para ler somente mensagens não lidas.
            OffsetPosition[] offsetPositions = new OffsetPosition[]
            {
                new OffsetPosition()
                {
                   Offset = offint,
                    PartitionId = 0
                }
            };
            var consumer = new Consumer(new ConsumerOptions(topic, router),offsetPositions);

            while (true)
            {
                if (!desliga)
                {
                    //Recebe mensagens(comandos) do tópico
                    foreach (var message in consumer.Consume())
                    {
                        string json = Encoding.ASCII.GetString(message.Value);
                        Comando comando = JsonConvert.DeserializeObject<Comando>(json);
                        string resposta = "";

                        if (comando.comand != (int)Comandos.LISTAR)
                        {
                            ProcessaComando(comando, ref resposta);

                            Console.WriteLine("ThreadProcessa: " + resposta);

                            Message msg = new Message(resposta);
                            produce.SendMessageAsync(topic2, new List<Message> { msg }); //Envia resposta do procesamento para cliente.

                            offint = offint + 1;
                        }
                        else if (comando.comand == (int)Comandos.LISTAR)
                        {
                            List<Message> msglist = new List<Message>();
                            int count = Mapa.Count;
                            int i = 1;
                            foreach (KeyValuePair<long, string> entry in Mapa)
                            {
                                resposta = entry.Key + " - " + entry.Value;
                                if (count == i)
                                {
                                    Message msg = new Message(resposta, "2");
                                    msglist.Add(msg);
                                }
                                else {
                                    Message msg = new Message(resposta, "1");
                                    msglist.Add(msg);
                                }
                                
                                i++;
                            }
                            produce.SendMessageAsync(topic2, msglist).Wait();
                            offint = offint + 1;
                        }

                        //Atualiza offset
                        using (StreamWriter file = new StreamWriter("portas.txt", false))
                        {
                            file.WriteLine(offint);
                            file.Close();
                        }

                        using (StreamWriter file = new StreamWriter("json.txt", true))
                        {
                            if (comando.comand == (int)Comandos.READ || comando.comand == (int)Comandos.DESLIGAR || comando.comand == (int)Comandos.LISTAR)
                                continue;
                            string comando2 = JsonConvert.SerializeObject(comando);
                            file.WriteLine(comando2);
                            Console.WriteLine("ThreadLog: " + comando2);
                            file.Close();
                        }

                    }
                }
            }
        }


   
        /////////////////////////////////////////////////////////////////////////////////
        static void ThreadSnapshot()
        {
            int intervalo;
            while (true)
            {
                lock (blockSegundos)
                {
                    intervalo = segundos * 1000;
                }

                Thread.Sleep(intervalo);

                SalvaSnapshot();
            }
        }
    


        ///////////////////////////////////////////////////////////////////////////////
        static void SalvaSnapshot()
        {
            FileStream stream;
            int snapshot = 0;


            for(int i = 1; i <= 3; i++)
            {
                if (!File.Exists("snapshot" + i + ".txt"))
                {
                    snapshot = i;
                    i = 4;
                }
            }

            if (snapshot == 1) { File.Delete("snapshot2.txt"); }
            else
            if (snapshot == 2) { File.Delete("snapshot3.txt"); }
            else
            if (snapshot == 3) { File.Delete("snapshot1.txt"); }

            stream = File.Create("snapshot" + snapshot + ".txt");
            stream.Close();

            using (StreamWriter file = new StreamWriter("snapshot" + snapshot + ".txt", true))
            {
                Comando cmd;
                string comando = "";
                lock (Mapa){
                    foreach(KeyValuePair<long, string> m in Mapa)
                    {
                        cmd = new Comando(Comandos.ADD, (int)m.Key, m.Value);
                        comando = JsonConvert.SerializeObject(cmd);
                        file.WriteLine(comando);
                    }
                }
                
                file.Close();
            }

            lock (blockLog)
            {
                if (File.Exists("json.txt"))
                {
                    File.Delete("json.txt");
                    stream = File.Create("json.txt");
                    stream.Close();
                }
            }
        }
    }
}
