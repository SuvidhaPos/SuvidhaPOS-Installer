using System;
using System.Windows.Forms;

namespace SuvidhaPosInstaller;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Keep the WinForms designer/layout in logical 96-DPI pixels. The Windows
        // shell will scale the complete installer window rather than reflowing the
        // fixed-height wizard cards and clipping their text at 125%/150%/200%.
        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
        AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

        ApplicationConfiguration.Initialize();
        using var form = new MainForm();
        RuntimeFix.Apply(form);
        Application.Run(form);
    }
}
