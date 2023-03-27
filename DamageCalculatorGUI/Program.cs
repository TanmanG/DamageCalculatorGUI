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
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Mgo+DSMBaFt+QHFqVkNrXVNbdV5dVGpAd0N3RGlcdlR1fUUmHVdTRHRcQl5hT3xSc0RhWXxeeHc=;Mgo+DSMBPh8sVXJ1S0d+X1RPd11dXmJWd1p/THNYflR1fV9DaUwxOX1dQl9gSX1RcEdiXH5ccHdXRGM=;ORg4AjUWIQA/Gnt2VFhhQlJBfV5AQmBIYVp/TGpJfl96cVxMZVVBJAtUQF1hSn5QdENhWHpfcnRWRWRf;MTQ3NzMyOUAzMjMxMmUzMTJlMzMzNWh3THpxNENqUjg4dXdVYjFaakJXVkVMalZ6U2s4YmpGVWRmelNGTTg1bDQ9;MTQ3NzMzMEAzMjMxMmUzMTJlMzMzNUM0NDMzR3gzSTg5UThEWTlRYS94TmM3Z2ZPTHFqUFBSRFdQMXZhR0FFYXM9;NRAiBiAaIQQuGjN/V0d+XU9Hc1RDX3xKf0x/TGpQb19xflBPallYVBYiSV9jS31TdUdlW35bcXZVQmBcUg==;MTQ3NzMzMkAzMjMxMmUzMTJlMzMzNWxxTnFGK281UU4rNHpWVHlmQXRQVHZUNFd0QXdHdzcwbi9KSUpYOEZ0QkE9;MTQ3NzMzM0AzMjMxMmUzMTJlMzMzNWFPdDZZK1FKQUxXVEZtb1RaN1Q2V01BUVZHQ2tzV2NweDI2UVBNMGovV2s9;Mgo+DSMBMAY9C3t2VFhhQlJBfV5AQmBIYVp/TGpJfl96cVxMZVVBJAtUQF1hSn5QdENhWHpfcnRRQGlf;MTQ3NzMzNUAzMjMxMmUzMTJlMzMzNVYvYW91Myt1UjZoYXNHc0lCRnlUUVNCaStZZGZTYWhKQjhla2ExQlRoQzg9;MTQ3NzMzNkAzMjMxMmUzMTJlMzMzNU9RN2VseERyb0REOEtHTDNreHF0WTNZemo5S0JHZGdXc0JqVzh6eUxXQnM9;MTQ3NzMzN0AzMjMxMmUzMTJlMzMzNWxxTnFGK281UU4rNHpWVHlmQXRQVHZUNFd0QXdHdzcwbi9KSUpYOEZ0QkE9");
            ApplicationConfiguration.Initialize();
            Application.Run(new CalculatorWindow());
        }
    }
}