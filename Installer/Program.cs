using System;
using System.Windows.Forms;

namespace SuvidhaPosInstaller;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Microsoft.Data.SqlClient uses native SNI on Windows by default.
        // Use managed networking so the installer itself does not depend on
        // the VC++ runtime before the optional VC++ component is installed.
        AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

        ApplicationConfiguration.Initialize();
        using var form = new MainForm();
        ResponsiveLayout.Apply(form);
        Application.Run(form);
    }
}
