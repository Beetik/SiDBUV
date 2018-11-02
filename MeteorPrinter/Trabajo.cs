using MeteorPrinter.Enumerados;

namespace MeteorPrinter
{
    public class Trabajo
    {
        public Trabajo(int bpp, int posY, int copias, string filename, RepeatMode modo)
        {
            BitsPorPixel = bpp;
            PosicionY = posY;
            NumeroDeCopias = copias;
            ModoRepeticion = modo;
            CargaImagenDesdeArchivo(filename);
        }

        public int BitsPorPixel { get; set; }

        /// <summary>
        /// Currently loaded print image
        /// </summary>
        public IMeteorImageData Imagen { get; set; }
        public int PosicionY { get; set; }
        public int NumeroDeCopias { get; set; }
        public RepeatMode ModoRepeticion { get; set; }
        public int Id { get; set; }

        /// <summary>
        /// Load the data for a new print image
        /// </summary>
        private void CargaImagenDesdeArchivo(string fileName)
        {
            this.Imagen = ImageDataFactory.Create(fileName);
        }
    }
}
