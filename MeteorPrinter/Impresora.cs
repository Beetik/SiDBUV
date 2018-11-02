using MeteorPrinter.Enumerados;
using System;
using Ttp.Meteor;

namespace MeteorPrinter
{
    public class Impresora
    {
        /// <summary>
        /// Object handles Meteor connection and status
        /// </summary>
        private PrinterStatusHandler status = new PrinterStatusHandler();

        /// <summary>
        /// ID of the latest meteor print job.  Incremented for each job.
        /// </summary>
        private int jobid = 1;
        /// <summary>
        /// Set after a job has been successfully started when there is Meteor
        /// hardware connected, and cleared when the Meteor status changes to
        /// ready or printing.
        /// The flag is not set if a job is started without hardware (e.g.
        /// to allow the SimPrint output to be checked)
        /// </summary>
        bool bJobStarting = false;
        /// <summary>
        /// Set after we abort a print job and cleared when the Meteor status 
        /// changes to idle.
        /// </summary>
        bool bJobAborting = false;

        /// <summary>
        /// Set up the printer prior to starting a print job.
        ///
        /// PiSetAndValidateParam blocks until the parameters have been successfully set (or have failed to set
        /// - e.g. if there is an out of range value).  
        /// 
        /// This must be used here in preference to the asynchronous method PiSetParam to guarantee that the
        /// values are set in Meteor before the print job is started.
        ///
        /// </summary>
        /// <returns>Success / failure</returns>
        private bool PrepararImpresora(int userPrintClock, int userBitsPerPixel)
        {
            if (PrinterInterfaceCLS.PiSetAndValidateParam((int)eCFGPARAM.CCP_PRINT_CLOCK_HZ, userPrintClock) != eRET.RVAL_OK)
            {
                return false;
            }
            if (PrinterInterfaceCLS.PiSetAndValidateParam((int)eCFGPARAM.CCP_BITS_PER_PIXEL, userBitsPerPixel) != eRET.RVAL_OK)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Abort any in-progress print job
        /// </summary>
        public void AbortarTrabajoEnProgreso()
        {
            // No longer starting a job
            bJobStarting = false;
            bJobAborting = true;

            // Send the abort command to Meteor.  This will halt any in-progress
            // print, and clear out all print buffers
            PrinterInterfaceCLS.PiAbort();
            status.WaitNotBusy();
        }

        /// <summary>
        /// Start a new print job
        /// </summary>
        public void ImprimirTrabajo(int clockFrequency, Trabajo  miTrabajo)
        {
            if (miTrabajo.Imagen == null)
            {
                throw new ArgumentNullException("El trabajo no contiene imagen");
            }
            if (!PrepararImpresora(clockFrequency, miTrabajo.BitsPorPixel))
            {
                throw new ArgumentNullException("Failed to setup printer");
            }

            //PreLoadPrintJob test = new PreLoadPrintJob(miTrabajo.BitsPorPixel, miTrabajo.Imagen, miTrabajo.PosicionY, miTrabajo.NumeroDeCopias, miTrabajo.ModoRepeticion, jobid++);
            eRET rVal = EnviarTrabajoAImpresora(miTrabajo);
            //eRET rVal = test.Start();

            if (rVal != eRET.RVAL_OK)
            {
                throw new Exception("");
            }
        }

        /// <summary>
        /// Send the print job to Meteor.  When the method returns, all print data has been
        /// sent to Meteor - however, it has not necessarily all been sent to the hardware.
        /// </summary>
        /// <returns>Success / failure</returns>
        private eRET EnviarTrabajoAImpresora(Trabajo miTrabajo)
        {
            eRET rVal;
            // Meteor command to start a print job
            int[] StartJobCmd = new int[] {
                (int)CtrlCmdIds.PCMD_STARTJOB,  // Command ID
                4,                              // Number of DWORD parameters
                jobid,                          // Job ID
                (int)eJOBTYPE.JT_PRELOAD,       // This job uses the preload data path
                (int)eRES.RES_HIGH,             // Print at full resolution
                miTrabajo.Imagen.GetDocWidth()+2           // Needed for Left-To-Right printing only
            };

            // A start job command can fail if there is an existing print job
            // ready or printing in Meteor, or if a previous print job is still
            // aborting.  The sequencing of the main form's control enables should 
            // guarantee that thie never happens in this application.
            if ((rVal = PrinterInterfaceCLS.PiSendCommand(StartJobCmd)) != eRET.RVAL_OK)
            {
                return rVal;
            }
            // The start document command specifies the number of discrete copies
            // of the image which are required
            //
            int[] StartDocCmd = new int[] {
                (int)CtrlCmdIds.PCMD_STARTPDOC, // Command ID
                1,                              // DWORD parameter count
                miTrabajo.ModoRepeticion == (RepeatMode.DISCRETE) ? miTrabajo.NumeroDeCopias : 1
            };
            if ((rVal = PrinterInterfaceCLS.PiSendCommand(StartDocCmd)) != eRET.RVAL_OK)
            {
                return rVal;
            }
            // For seamless image repeats using the prelod data path, PCMD_REPEAT
            // must be sent after PCMD_STARTPDOC and before the image data.
            //
            if (miTrabajo.NumeroDeCopias > 1 && miTrabajo.ModoRepeticion == RepeatMode.SEAMLESS)
            {
                int[] RepeatCmd = new int[] {
                    (int)CtrlCmdIds.PCMD_REPEAT,    // Command ID
                    1,                              // DWORD parameter cound
                    miTrabajo.NumeroDeCopias
                };
                if ((rVal = PrinterInterfaceCLS.PiSendCommand(RepeatCmd)) != eRET.RVAL_OK)
                {
                    return rVal;
                }
            }
            // PCMD_BIGIMAGE must be used if the application needs to pass images 
            // which exceed 60MB in size to Meteor as one buffer.  (An alternative
            // is for the application to split up the data into smaller images,
            // each of which can used PCMD_IMAGE).
            //
            // The image data is sent through the Printer Interface to the Meteor
            // Print Engine in chunks.  The application must continually call 
            // PiSendCommand with the same buffer while the Print Engine
            // returns RVAL_FULL.
            //
            // Note that it is necessary to fix the location of the image command
            // in memory while carrying out this sequence, to prevent the garbage
            // collector from relocating the buffer (theoretically possible, but 
            // highly unlikely) between successive PiSendCommand calls.
            //
            int[] ImageCmd = miTrabajo.Imagen.GetBigImageCommand(miTrabajo.PosicionY, miTrabajo.BitsPorPixel);
            unsafe
            {
                fixed (int* pImageCmd = ImageCmd)
                {
                    do
                    {
                        rVal = PrinterInterfaceCLS.PiSendCommand(ImageCmd);
                    } while (rVal == eRET.RVAL_FULL);
                    if (rVal != eRET.RVAL_OK)
                    {
                        return rVal;
                    }
                }
            }
            int[] EndDocCmd = new int[] { (int)CtrlCmdIds.PCMD_ENDDOC, 0 };
            if ((rVal = PrinterInterfaceCLS.PiSendCommand(EndDocCmd)) != eRET.RVAL_OK)
            {
                return rVal;
            }
            int[] EndJobCmd = new int[] { (int)CtrlCmdIds.PCMD_ENDJOB, 0 };
            if ((rVal = PrinterInterfaceCLS.PiSendCommand(EndJobCmd)) != eRET.RVAL_OK)
            {
                return rVal;
            }
            return eRET.RVAL_OK;
        }
    }
}
