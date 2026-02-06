using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resources.Common.Scripts.PLC_Functions
{
    public class FTRIG
    {
        private bool estadoAnterior;

        public FTRIG()
        {
            estadoAnterior = false; // Estado inicial en verdadero
        }

        // Detecta flanco descendente (de 1 a 0)
        public bool Detect(bool estadoActual)
        {
            bool flancoDescendente = estadoAnterior && !estadoActual;
            estadoAnterior = estadoActual;
            return flancoDescendente;
        }
    }
}
