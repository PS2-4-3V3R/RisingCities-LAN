using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resources.Common.Scripts.PLC_Functions
{
    public class TON
    {
        private Stopwatch reloj = new Stopwatch();
        private int tiempoObjetivo;

        public TON(int tiempoMs)
        {
            tiempoObjetivo = tiempoMs;
        }

        public void ReStart()
        {
            reloj.Restart(); // Inicia o reinicia el temporizador
        }

        public bool Q()
        {
            return reloj.ElapsedMilliseconds >= tiempoObjetivo;
        }

        public void Reset()
        {
            reloj.Stop();
        }
    }
}
