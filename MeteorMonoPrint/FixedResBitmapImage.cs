using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ttp.Meteor.MeteorMonoPrint
{
    class FixedResBitmapImage : IMeteorImageData
    {
        Bitmap bitmap;
        string basename;

        public void Dispose() {
            if (bitmap != null) {
                bitmap.Dispose();
                bitmap = null;
            }
        }
        public bool Load(string Path) {
            try {
                bitmap = new Bitmap(Path);
                basename = Path.Substring(Path.LastIndexOf('\\') + 1);
                return true;
            }
            catch (Exception e) {
                MessageBox.Show("Failed to open file " + Path + "\r\n\r\n" + e.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        public int ReverseBytes(int v) {
            // swap bytes
            v = ((v >> 8) & 0x00FF00FF) | ((v & 0x00FF00FF) << 8);
            // swap words
            v = ((v >> 16) & 0x0000FFFF) | ((v & 0x0000FFFF) << 16);
            return v;
        }
        public int[] GetBigImageCommand(int yTop, int trueBpp) {
            // Bits per pixel in the Meteor buffer / source image
            int outbpp = (trueBpp == 3) ? 4 : trueBpp;
            // Image dimensions
            int width = bitmap.Width;
            int height = bitmap.Height;
            // The width of the image data buffer sent to Meteor must be a multiple
            // of DWORDs
            int bufwidthDWORDs = (((width * outbpp) + 31) >> 5);
            // Meteor image buffer size in DWORDs
            int isize = bufwidthDWORDs * height;
            // PCMD_IMAGE header size 
            int hdrsize = 4;

            // Allocate memory for image + header.  Note that this buffer will be
            // initialised to zero by the framework.
            int[] buff = new int[isize + hdrsize + 2];
            if (null == buff) {
                return null;
            }
            // Fill in the command header
            buff[0] = (int)CtrlCmdIds.PCMD_IMAGE;   // Command
            buff[1] = isize + hdrsize;              // Dword count
            buff[2] = 1;                            // Plane
            buff[3] = 1;                            // Xleft
            buff[4] = yTop;                         // Ytop
            buff[5] = width;                        // Width
            int dp = 2 + hdrsize;                   // Index of first data DWORD

            // 
            if (bitmap.PixelFormat == PixelFormat.Format4bppIndexed) {
                if (trueBpp != 4 && trueBpp != 3) { // TODO - need to make sure Meteor is running at 4bpp before
                    return null;                    //        we get here
                }
            } else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed) {
                if (trueBpp != 1) {                 // TODO - need to make sure Meteor is running at 1bpp before
                    return null;                    //        we get here
                }
            } else {
                return null;
            }

            // Copy the image data line by line from the bitmap and write it into the Meteor 
            // command buffer without changing resolution
            BitmapData bData = bitmap.LockBits(
                                    new Rectangle(new Point(), bitmap.Size),
                                    ImageLockMode.ReadOnly,
                                    bitmap.PixelFormat);
            int srcstrideBytes = bData.Stride;
            // Line buffer - assuming same size source and destination data.
            // This could be longer than the above stride due to rounding up to a DWORD
            // boundary
            byte[] lineData = new byte[bufwidthDWORDs * 4];
            for (int y = 0; y < height; y++) {
                // Copy entire line into temporary buffer
                IntPtr srcPtr = new IntPtr(bData.Scan0.ToInt32() + y * srcstrideBytes);
                Marshal.Copy(srcPtr, lineData, 0, srcstrideBytes);
                // Copy line into Meteor command buffer
                int destOffset = dp + y * bufwidthDWORDs;   // Offset in DWORDS
                Buffer.BlockCopy(lineData, 0, buff, 4 * destOffset, 4 * bufwidthDWORDs /* bytes to copy */);
                // Reverse bytes within each Dword (big endian vs. little endian byte ordering)
                for (int x = destOffset; x < destOffset + bufwidthDWORDs; x++) {
                    buff[x] = ReverseBytes(buff[x]);
                }
            }
            bitmap.UnlockBits(bData);
            return buff;
        }
        public string GetBaseName() {
            return basename;
        }
        public Bitmap GetPreviewBitmap() {
            return bitmap;
        }
        public int GetDocWidth() {
            return bitmap.Width;
        }
        public int GetDocHeight() {
            return bitmap.Height;
        }
    }
}
