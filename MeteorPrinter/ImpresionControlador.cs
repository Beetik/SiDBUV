namespace MeteorPrinter
{
    public class ImpresionControlador
    {
        private readonly Impresora _impresora;

        public ImpresionControlador()
        {
            _impresora = new Impresora();
        }

        public void AbortarImpresion()
        {
            _impresora.AbortarTrabajoEnProgreso();
        }

        public void ImprimirTrabajo(int clockFrequency, Trabajo job)
        {
            _impresora.ImprimirTrabajo(clockFrequency, job);
        }
    }
}
