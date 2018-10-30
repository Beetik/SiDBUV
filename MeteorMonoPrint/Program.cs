using MeteorPrinter;
using System;
using System.Windows.Forms;

namespace Ttp.Meteor.MeteorMonoPrint
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if ( !MeteorPath.LocatePrinterInterface() ) {
                MessageBox.Show("Failed to locate the Meteor Printer Interface", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Application.Run(new FormMeteorMonoPrint());
        }
    }
}
