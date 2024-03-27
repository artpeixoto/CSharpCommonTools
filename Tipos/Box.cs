using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Tools.Tipos
{
    public class Box<T> where T: struct {
        public Box(T valor)
        {
            this.Valor = valor;
        }

        public T Valor { get; set; }
    }
}
