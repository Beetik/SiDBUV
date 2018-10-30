using System;

namespace MeteorPrinter
{
    /// <summary>
    /// Factory for creating an object that implements the IMeteorImageData interface
    /// from a file path.  The type of object to create is based on the file
    /// extension.
    /// </summary>
    public class ImageDataFactory
    {
        public static IMeteorImageData Create(string FileName) {
            IMeteorImageData image = null;
            int ExtensionIndex = FileName.LastIndexOf('.');
            if (ExtensionIndex != -1) {
                string FileExt = FileName.Substring(ExtensionIndex + 1).ToLower();
                switch (FileExt) {
                    case "bmp":
                    case "jpg":
                    case "tif":
                        image = new MeteorBitmapImage();
                        break;
                    default:
                        break;
                }
            }
            if (image != null) {
                if ( image.Load(FileName) ) {
                    return image;
                }
            } else {
                var mensaje = "Unrecognised file type " + FileName;
                throw new Exception(mensaje);
            }
            return null;
        }
    }
}
