using System.Windows;

namespace OpenImageViewer;

public partial class App : Application
{
    private void App_Startup(object sender, StartupEventArgs e)
    {
        new MainWindow(e.Args.FirstOrDefault()).Show();
    }
}
