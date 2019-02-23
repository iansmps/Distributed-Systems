﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Comando
    {
        public int Chave { get; set; }
        public string Valor { get; set; }
        public int comand { get; set; }

        public Comando(Comandos comando, int chave, string valor)
        {
            this.Chave = chave;
            this.Valor = valor;
            this.comand = (int)comando;
        }
    }

    public enum Comandos
    {
        ADD = 1,
        READ = 2,
        UPDATE = 3,
        DELETE = 4,
        LISTAR = 5,
        DESLIGAR = 6,
        MONITORAR = 7,
        SNAPSHOT = 8
    }
}
