using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Ttp.Meteor;


namespace MeteorPrinter
{
    /// <summary>
    /// Object which loads a .bmp file and converts it into an image command for Meteor
    /// </summary>
    public class MeteorBitmapImage : IMeteorImageData
    {
        /// <summary>
        /// Data loaded from the bitmap file
        /// </summary>
        private Bitmap bitmap;
        /// <summary>
        /// Name of the source bitmap file with the path removed
        /// </summary>
        private string basename;

        /// <summary>
        /// Constructor
        /// </summary>
        public MeteorBitmapImage() {
        }

        #region IMeterImageData
        public bool Load(string Path) {
            //try {
                bitmap = new Bitmap(Path);
                basename = Path.Substring(Path.LastIndexOf('\\') + 1);
                return true;
            //}
            //catch (Exception e) {
            //    //MessageBox.Show("Failed to open file " + Path + "\r\n\r\n" + e.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return false;
            //}
        }

        public int[] GetBigImageCommand(int yTop, int trueBpp, int plane = 1, int xLeft = 1) {
            return GetImageCommand(yTop, trueBpp, true, plane, xLeft);
        }

        public string GetBaseName()         { return basename; }
        public Bitmap GetPreviewBitmap()    { return bitmap; }
        public int GetDocWidth()            { return bitmap.Width; } 
        public int GetDocHeight()           { return bitmap.Height; }
        #endregion

        /// <summary>
        /// Allocates and fills an image command buffer to be sent to Meteor via
        /// PiSendCommand.  The image data in the buffer comes from the previously
        /// loaded bitmap.
        /// 
        /// The standard command for sending image data to Meteor is PCMD_IMAGE.
        /// If a large image (> 60MB) needs to be sent in one command then
        /// PCMD_BIGIMAGE command must be used.
        /// </summary>
        /// <param name="yTop">Y position in pixels of the image</param>
        /// <param name="trueBpp">Bits per pixel</param>
        /// <param name="useBigImage">Use the standard PCMD_IMAGE or PCMD_BIGIMAGE</param>
        /// <returns>Image command to send to Meteor.  null if memory allocation fails</returns>
        private int[] GetImageCommand(int yTop, int trueBpp, bool useBigImage, int plane, int xLeft) {
            // Meteor sends print data to the hardware as 1,2 or 4bpp.
            // Some heads accept 3bpp data; some accept 4bpp.
            // For 3bpp heads we need to use the least significant 3 bits in the 4bpp data.
            int outbpp = (trueBpp == 3) ? 4 : trueBpp;
            // Image dimensions
            int width = bitmap.Width;
            int height = bitmap.Height;
            // The width of the image data buffer sent to Meteor must be a multiple
            // of DWORDs
            int bufwidthDWORDs = (((width * outbpp) + 31) >> 5);
            // Meteor image buffer size in DWORDs
            int isize = bufwidthDWORDs * height;
            // Header size is different for PCMD_IMAGE and PCMD_BIGIMAGE
            int hdrsize = useBigImage ? 5 : 4;

            // Allocate memory for image + header.  Note that this buffer will be
            // initialised to zero by the framework.
            int[] buff = new int[isize + 7];
            if(null == buff) {
                return null;
            }
            // Fill in the command header
            buff[0] = useBigImage ? (int)CtrlCmdIds.PCMD_BIGIMAGE 
                                  : (int)CtrlCmdIds.PCMD_IMAGE;   // Command
            buff[1] = isize + hdrsize;              // Dword count
            buff[2] = plane;                        // Plane
            buff[3] = xLeft;                        // Xleft
            buff[4] = yTop;                         // Ytop
            buff[5] = width;                        // Width
            if (useBigImage) {
                buff[6] = height;                   // Height (PCMD_BIGIMAGE only)
            }
            int dp = 2 + hdrsize;                   // Index of first data DWORD

            // Copy the image data line by line from the bitmap and write it into the Meteor 
            // command buffer at the requested resolution
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int srcstrideBytes = bmpData.Stride;
            byte[] lineData = new byte[4 * bufwidthDWORDs * (32 / outbpp)]; // 32 bits per pixel for the maximum Meteor buffer line length
                                                                            // to protect against falling off the end of actual pixel data
                                                                            // in the below loop
            for(int y = 0; y < height; y++) {
                Marshal.Copy(new IntPtr(bmpData.Scan0.ToInt64() + y * srcstrideBytes), lineData, 0, srcstrideBytes);
                if(trueBpp == 1) {
                    for(int x = 0; x < width; x += 4) {
                        int byteoff = x >> 2;
                        int shift = 4 * (7 - (byteoff & 7));	// Bit count to shift left

                        // Get first source pixel
                        byte b = ArgbToGrey(lineData, x * 4);
                        b ^= 0xFF;      // Invert for printer
                        b >>= 4;        // Only interested in 1 msbs

                        // Get second source pixel
                        byte b2 = ArgbToGrey(lineData, (x + 1) * 4);
                        b2 ^= 0xFF;     // Invert for printer
                        b2 >>= 5;       // Only interested in 1 msbs

                        // Get third source pixel
                        byte b3 = ArgbToGrey(lineData, (x + 2) * 4);
                        b3 ^= 0xFF;     // Invert for printer
                        b3 >>= 6;       // Only interested in 1 msbs

                        // Get fourth source pixel
                        byte b4 = ArgbToGrey(lineData, (x + 3) * 4);
                        b4 ^= 0xFF;     // Invert for printer
                        b4 >>= 7;       // Only interested in 1 msbs

                        b = (byte)((b & 0x08) | (b2 & 0x4) | (b3 & 0x02) | (b4 & 0x01));    // Align pixels

                        buff[dp] |= ((int)b) << shift;          // Write both pixels

                        if((x >= width - 4) || ((byteoff & 7) == 7)) {
                            dp++;
                        }
                    }
                } else if(trueBpp == 2) {
                    int lbuff = bmpData.Stride * y;
                    for(int x = 0; x < width; x += 2) {
                        int byteoff = x >> 1;
                        int shift = 4 * (7 - (byteoff & 7));	// Bit count to shift left

                        // Get first source pixel
                        byte b = ArgbToGrey(lineData, x * 4);
                        b ^= 0xFF;      // Invert for printer
                        b >>= 4;        // Only interested in 2 msbs

                        // Get second source pixel
                        byte b2 = ArgbToGrey(lineData, (x + 1) * 4);
                        b2 ^= 0xFF;     // Invert for printer
                        b2 >>= 6;       // Only interested in 2 msbs

                        b = (byte)((b & 0x0C) | (b2 & 0x3));    // Align pixels
                        buff[dp] |= ((int)b) << shift;          // Write both pixels
                        if((x >= width - 2) || ((byteoff & 7) == 7)) {
                            dp++;
                        }
                    }
                } else {	// trueBpp ==  3 or 4
                    byte bppMask = (byte)((1 << trueBpp) - 1);  // Make to suit the true bpp
                    int lbuff = bmpData.Stride * y;
                    for(int x = 0; x < width; x += 1) {
                        int byteoff = x;
                        int shift = 4 * (7 - (byteoff & 7));	// Bit count to shift left

                        // Get first source pixel
                        byte b = ArgbToGrey(lineData, x * 4);
                        b ^= 0xFF;      // Invert for printer
                        b >>= 4;        // Only interested in 4 msbs

                        if (trueBpp == 3) { //we are dealing with a smaller amount of bits at the head
                            b >>= 1;
                        }

                        b &= bppMask;      // Mask upper nibble
                        buff[dp] |= ((int)b) << shift;          // Write both pixels
                        if((x >= width - 1) || ((byteoff & 7) == 7)) {
                            dp++;
                        }
                    }
                }
            }
            bitmap.UnlockBits(bmpData);
            return buff;
        }

        /// <summary>
        /// Converts an ARGB value to greyscale
        /// </summary>
        /// <param name="image">Byte array of pixel data</param>
        /// <param name="offset">Byte offset of ARGB value</param>
        /// <returns>8-bit greyscale value</returns>
        private byte ArgbToGrey(byte[] image, int offset) {
            return (byte)((float)image[offset + 2] * 0.3 + (float)image[offset + 1] * 0.59 + (float)image[offset + 0] * 0.11);
        }

        #region IDisposable
        public void Dispose()
        {
            if (bitmap != null) { bitmap.Dispose(); }
        }
        #endregion
    }
}
