using System;
using System.Windows.Forms;

namespace SuvidhaPosInstaller;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Keep SqlClient on managed networking so the installer can start before VC++ is installed.
        AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

        ApplicationConfiguration.Initialize();
        using var form = new MainForm();
        FreshUi.Apply(form);
        Application.Run(form);
    }
}
