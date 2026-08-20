using System;
using System.Windows.Forms;

namespace SuvidhaPosInstaller;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        AppContext.SetSwitch(
            "Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows",
            true);

        ApplicationConfiguration.Initialize();

        var form = new MainForm();
        UiPolish.Apply(form);
        Application.Run(form);
    }
}
