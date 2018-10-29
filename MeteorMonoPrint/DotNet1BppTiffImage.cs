using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Ttp.Meteor.MeteorMonoPrint
{
    class DotNet1BppTiffImage : IMeteorImageData
    {
        /// <summary>
        /// The TIFF file is loaded into a .NET bitmap object
        /// </summary>
        private Bitmap bitmap;
        /// <summary>
        /// Display name (i.e. the filename with the path stripped out)
        /// </summary>
        private string basename;

        // ---- IMeteorImageData ----
        #region IMeteorImageData
        /// <summary>
        /// Load the file.  If this is not a 1bpp file we fail and return false.
        /// </summary>
        public bool Load(string Path) {
            try {
                bitmap = new Bitmap(Path);
                basename = Path.Substring(Path.LastIndexOf('\\') + 1);
                if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format1bppIndexed) {
                    return false;
                }
                return true;
            }
            catch (Exception e) {
                MessageBox.Show("Failed to open file " + Path + "\r\n\r\n" + e.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        /// <summary>
        /// Form the image buffer to send to Meteor
        /// </summary>
        public int[] GetBigImageCommand(int yTop, int trueBpp) {
            // Image dimensions
            int widthPixels = bitmap.Width;
            int heightPixels = bitmap.Height;
            // Buffer line length must be a multiple of (32 bit) DWORDS.
            // Here we are assuming 1bpp
            int bufWidthDWORDs = (widthPixels + 31) / 32;
            // Image buffer size
            int iSizeDWORDS = bufWidthDWORDs * heightPixels;
            // Command buffer size - the image buffer size plus the header (6 DWORDS)
            //
            // -- Despite the method name, the example uses the standard PCMD_IMAGE command --
            // -- as we are assuming a relatively small image size.  For images larger than --
            // -- ~60MB, other mechanisms (such as PCMD_BIGIMAGE) should be used           --
            //
            int cmdSizeDWORDS = iSizeDWORDS + 6;
            // Allocate the command buffer
            int[] buff = new int[cmdSizeDWORDS];
            if (buff == null) {
                return null;
            }
            // Fill in the command header
            buff[0] = (int)CtrlCmdIds.PCMD_IMAGE;   // Command
            buff[1] = 4 + iSizeDWORDS;              // Total number of following DWORDs
            buff[2] = 1;                            // Send data to Plane 1
            buff[3] = 1;                            // Image X left position.
            buff[4] = yTop;                         // Image Y top position.
            buff[5] = widthPixels;                  // Image width in pixels

            // Starting position in the Meteor buffer.  The image data immediately follows
            // on from the command.
            int destPos = 6;

            //
            // Copy the image data line by line write it into the Meteor command buffer.
            //

            // Rectangle covering the entire source image bitmap
            Rectangle bitmapRect = new Rectangle(0, 0, widthPixels, heightPixels);
            // Lock the source bitmap into memory
            BitmapData bmpData = bitmap.LockBits(bitmapRect, ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
            // Stride between lines in the source bitmap
            int srcstrideBytes = bmpData.Stride;
            // Buffer to hold line data [ has to be a whole number of DWORDS so may be longer than the actual line in the bitmap ]
            byte[] lineData = new byte[4 * bufWidthDWORDs];
            // Iterate through each line in the source image
            for (int y = 0; y < heightPixels; y++) {                
                // Pointer to the start of the line in the source bitmap
                IntPtr pLineStart = new IntPtr( bmpData.Scan0.ToInt32() + y * srcstrideBytes );
                // Copy data for the next line into the interim line data buffer
                Marshal.Copy(pLineStart, lineData, 0, srcstrideBytes);
                //
                // Iterate each DWORD (32 pixels) and copy into the Meteor command buffer.
                //
                // The .NET object returns data in the same bit order required by Meteor.
                // However, we need to invert the bits
                //
                // As the temporary line data buffer is a whole number of DWORDS, we do not 
                // need to worry about falling off the end of the pixel data by up to 3 bytes.
                //
                for (int i = 0; i < bufWidthDWORDs; i++) {
                    int dword = (lineData[4*i] << 24) |
                                (lineData[4*i + 1] << 16) |
                                (lineData[4*i + 2] << 8) |
                                (lineData[4*i + 3]);
                    buff[destPos++] = ~dword;
                }
            }
            // Unlock bitmap data
            bitmap.UnlockBits(bmpData);
            // Return buffer ready to be sent to Meteor
            return buff;
        }
        /// <summary>
        /// Display name to use
        /// </summary>
        public string GetBaseName() {
            return basename;
        }
        /// <summary>
        /// Preview bitmap - for simplicity, just use the loaded bitmap.
        /// For larger images a thumbnail approach would be more appropriate.
        /// </summary>
        public Bitmap GetPreviewBitmap() {
            return bitmap;
        }
        /// <summary>
        /// Width in pixels of the image
        /// </summary>
        public int GetDocWidth() {
            return bitmap.Width;
        }
        /// <summary>
        /// Height in pixels of the image
        /// </summary>
        public int GetDocHeight() {
            return bitmap.Height;
        }
        #endregion // IMeteorImageData

        // --- IDisposable ----
        #region IDispoable
        public void Dispose() {
            if (bitmap != null) {
                bitmap.Dispose();
                bitmap = null;
            }
        }
        #endregion // IDisposable
    }
}
