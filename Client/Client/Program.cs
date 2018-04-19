using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
            ProtocolType.Udp);

            IPAddress broadcast = IPAddress.Parse("127.0.0.1");
            IPEndPoint ep = new IPEndPoint(broadcast, 1500);

            Comando comando = new Comando(Comandos.READ, 1, "TESTANDO");
            byte[] receive = new byte[1400];
            byte[] sendbuf = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(comando));
            string final;
            s.SendTo(sendbuf, ep);
            s.Receive(receive);

            final = Encoding.UTF8.GetString(receive).TrimEnd('\0');

            Console.WriteLine(final);

            comando = new Comando(Comandos.READ, 2, "Batata");

            sendbuf = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(comando));

            s.SendTo(sendbuf, ep);
            receive = new byte[1400];
            s.Receive(receive);
            
            final = Encoding.UTF8.GetString(receive).TrimEnd('\0');

            Console.WriteLine(final);

            comando = new Comando(Comandos.READ, 3, "Limao");

            sendbuf = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(comando));

            s.SendTo(sendbuf, ep);
            receive = new byte[1400];
            s.Receive(receive);

            final = Encoding.UTF8.GetString(receive).TrimEnd('\0');

            Console.WriteLine(final);

            comando = new Comando(Comandos.READ, 5, "Batata");

            sendbuf = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(comando));

            s.SendTo(sendbuf, ep);
            receive = new byte[1400];
            s.Receive(receive);

            final = Encoding.UTF8.GetString(receive).TrimEnd('\0');

            Console.WriteLine(final);

            comando = new Comando(Comandos.READ, 6, "Mudou?");

            sendbuf = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(comando));

            s.SendTo(sendbuf, ep);
            receive = new byte[1400];
            s.Receive(receive);

            final = Encoding.UTF8.GetString(receive).TrimEnd('\0');

            Console.WriteLine(final);
        }
    }
}
