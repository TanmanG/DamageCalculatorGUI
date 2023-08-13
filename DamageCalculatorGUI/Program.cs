namespace DamageCalculatorGUI
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Insert license here!");
            ApplicationConfiguration.Initialize();
            Application.Run(new CalculatorWindow());
        }
    }
}