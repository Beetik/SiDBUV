namespace MeteorPrinter.Enumerados
{
    public enum PrinterStatus
    {
        /// <summary>
        /// Meteor is not connected.  This will happen if the Meteor PrintEngine has not
        /// yet been started (e.g. Monitor.exe needs to be run), or if another application
        /// is currently connected to Meteor.
        /// </summary>
        DISCONNECTED,
        /// <summary>
        /// Meteor is connected and waiting for hardware
        /// </summary>
        WAITING_FOR_PCC,
        /// <summary>
        /// Meteor is initialising the PCC hardware
        /// </summary>
        INITIALISING,
        /// <summary>
        /// Meteor is inactive
        /// </summary>
        IDLE,
        /// <summary>
        /// The print data for the next print job is being loaded into the hardware buffers
        /// </summary>
        LOADING,
        /// <summary>
        /// The current print job is aborting
        /// </summary>
        ABORTING,
        /// <summary>
        /// The print data for the next print job is loaded and Meteor is waiting for an external
        /// product detect signal to start printing
        /// </summary>
        READY,
        /// <summary>
        /// Meteor is printing
        /// </summary>
        PRINTING,
        /// <summary>
        /// Meteor is in an error condition
        /// </summary>
        ERROR
    }
}
