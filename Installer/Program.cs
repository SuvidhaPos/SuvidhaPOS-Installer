using System.Windows.Forms;
using SuvidhaPOSInstaller.DynamicUI;

namespace SuvidhaPosInstaller;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var form = new InstallerForm();
        Application.Run(form);
    }
}