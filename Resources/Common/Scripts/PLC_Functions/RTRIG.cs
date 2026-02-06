using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resources.Common.Scripts.PLC_Functions
{
    public class RTRIG
    {
        private bool estadoAnterior;

        public RTRIG()
        {
            estadoAnterior = false; // Estado inicial en falso
        }

        // Detecta flanco ascendente (de 0 a 1)
        public bool Detect(bool estadoActual)
        {
            bool flancoAscendente = !estadoAnterior && estadoActual;
            estadoAnterior = estadoActual;
            return flancoAscendente;
        }
    }
}
