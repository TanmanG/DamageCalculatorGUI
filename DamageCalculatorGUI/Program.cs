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
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MTM2MjI1NEAzMjMwMmUzNDJlMzBpWTAzYWx5S295WmNxNzlDb21jNDZOSW9rYit1QW1lMGJ6Yjl6U09hZXZ3PQ==;Mgo DSMBaFt/QHRqVVhkVFpHaV1GQmFJfFBmQ2lbe1RyckUmHVdTRHRcQl5iTn9bdEFhX3lfd3c=;Mgo DSMBMAY9C3t2VVhkQlFacldJXnxLekx0RWFab1h6dlFMYlpBJAtUQF1hSn5Qd0JiUX1acnJTQ2JY;Mgo DSMBPh8sVXJ0S0J XE9AflRBQmFNYVF2R2BJeVRycF9EZkwxOX1dQl9gSX1ScURrW3tcdnJTTmM=;MTM2MjI1OEAzMjMwMmUzNDJlMzBnT0ZjNmJRSHV3R2U0N3hDNzdCTEt1WTROUG1TWVBySWYrL09HNVZPRGFFPQ==;NRAiBiAaIQQuGjN/V0Z WE9EaFtKVmBWfFdpR2NbfE52fldCal5XVBYiSV9jS31TdURkWHdcdHZTQWBbUg==;ORg4AjUWIQA/Gnt2VVhkQlFacldJXnxLekx0RWFab1h6dlFMYlpBJAtUQF1hSn5Qd0JiUX1acnJSRGJf;MTM2MjI2MUAzMjMwMmUzNDJlMzBFMnJOMDFIdzJ2Tnp6a2dCc3UzTkxpYmlXWmU4YWV2N1RvMnZmOE8xeGhFPQ==;MTM2MjI2MkAzMjMwMmUzNDJlMzBjcnpqejIyVzZHSGcwdWNuZThScnoxaUl5NTk5QTZNUlpEOW5JcFZhWVE0PQ==;MTM2MjI2M0AzMjMwMmUzNDJlMzBSVFdEeEtQVm5XeDZEZUtJdit4M1BOeHZvZEh3WExzMmZxd2VHZ3BITFY0PQ==;MTM2MjI2NEAzMjMwMmUzNDJlMzBFblIxQmNqcFFDZW8yQ3hpQVRGWXNDQjFFSWIxY0VnNkZrNkxCY0xDd0RZPQ==;MTM2MjI2NUAzMjMwMmUzNDJlMzBpWTAzYWx5S295WmNxNzlDb21jNDZOSW9rYit1QW1lMGJ6Yjl6U09hZXZ3PQ==");
            ApplicationConfiguration.Initialize();
            Application.Run(new CalculatorWindow());
        }
    }
}