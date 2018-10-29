using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ttp.Meteor.MeteorMonoPrint
{
    class BitmapImage4bppIndexed : IMeteorImageData
    {
        Bitmap bitmap;
        string basename;
        int maxlevel;

        public void Dispose() {
            if (bitmap != null) {
                bitmap.Dispose();
                bitmap = null;
            }
        }
        public bool Load(string Path) {
            try {
                bitmap = new Bitmap(Path);
                if (bitmap.PixelFormat != PixelFormat.Format4bppIndexed) {
                    return false;
                }
                basename = Path.Substring(Path.LastIndexOf('\\') + 1);
                // Palette starts at white and can map multiple levels to black
                // Assume that anything after the first black entry is unused
                int blackLevels = 0;
                maxlevel = 0;
                bool maxLevelFound = false;
                foreach (Color c in bitmap.Palette.Entries) {
                    if (c.R == 0 && c.G == 0 && c.B == 0) {
                        blackLevels++;
                        maxLevelFound = true;
                    }
                    if (!maxLevelFound) {
                        maxlevel++;
                    }
                }
                basename += " [ " + maxlevel + " grey levels ]";
                return true;
            }
            catch (Exception e) {
                MessageBox.Show("Failed to open file " + Path + "\r\n\r\n" + e.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
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

            // Lock the bitmap data in memory
            BitmapData bData = bitmap.LockBits(
                                    new Rectangle(new Point(), bitmap.Size),
                                    ImageLockMode.ReadOnly,
                                    bitmap.PixelFormat);
            // Line buffer.  Make this up to 32 pixels longer than is necessary, to avoid
            // having to check for the end of a line within the below loops
            int srcstrideBytes = bData.Stride;
            int lineDataBufLen = Math.Max(srcstrideBytes, ((width + 1) / 2) + 16); 
            byte[] lineData = new byte[lineDataBufLen];

            // Copy the image data line by line from the bitmap and write it into the Meteor 
            // command buffer
            for (int y = 0; y < height; y++) {
                // Offset in DWORDS in Meteor command buffer for this line
                int destOffset = dp + y * bufwidthDWORDs;   
                // Copy entire source line into temporary buffer
                IntPtr srcPtr = new IntPtr(bData.Scan0.ToInt32() + y * srcstrideBytes);
                Marshal.Copy(srcPtr, lineData, 0, srcstrideBytes);
                // Assemble the output line
                int ip = 0;
                for (int op = destOffset; op < destOffset + bufwidthDWORDs; op++) {
                    UInt32 val = 0;
                    if (trueBpp == 1) {
                        // 32 pixels per output DWORD, 2 pixels per input byte
                        for (int i = 0; i < 16; i++) {
                            byte px1 = (byte)((lineData[ip] & 0xF0) >> 4);
                            byte px2 = (byte)(lineData[ip++] & 0x0F);
                            val <<= 1;
                            if (px1 != 0) { val |= 1; }
                            val <<= 1;
                            if (px2 != 0) { val |= 1; }
                        }
                    } else if (trueBpp == 2) {
                        const byte max = 3;
                        // 16 pixels per output DWORD, 2 pixels per input byte
                        for (int i = 0; i < 8; i++) {
                            byte px1 = (byte)((lineData[ip] & 0xF0) >> 4);
                            byte px2 = (byte)(lineData[ip++] & 0x0F);
                            val <<= 2;
                            val |= Math.Min(max, px1);
                            val <<= 2;
                            val |= Math.Min(max, px2);
                        }
                    } else {
                        // 8 pixels per output DWORD, 2 pixels per input byte
                        for (int i = 0; i < 4; i++) {
                            byte px1 = (byte)((lineData[ip] & 0xF0) >> 4);
                            byte px2 = (byte)(lineData[ip++] & 0x0F);
                            val <<= 4;
                            val |= px1;
                            val <<= 4;
                            val |= px2;
                        }
                    }
                    buff[op] = (int)val;
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
