using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace OpenImageViewer;

public partial class AboutWindow : Window
{
    // Verified as a valid bech32 segwit v0 mainnet address before being shipped:
    // a mistyped address would send donations nowhere recoverable.
    private const string DonationAddress = "bc1qrelrzwd4rm6uxdjk9kkzynzl4ggtnl2c5f2k7f";

    private const string Homepage = "https://github.com/riazhassan-za/openimageviewer";

    public AboutWindow()
    {
        InitializeComponent();
        HomepageLink.NavigateUri = new Uri(Homepage);
        HomepageLink.Inlines.Add(Homepage);
        AddressBox.Text = DonationAddress;
        WalletLink.NavigateUri = new Uri("bitcoin:" + DonationAddress);
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(DonationAddress);

    private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
        catch { MessageBox.Show(this, $"No application is registered to open {e.Uri.Scheme}: links.", "About"); }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
