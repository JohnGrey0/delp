using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("pbkdf2", "PBKDF2 Password Hash", ToolCategory.Hashing,
    "Derive a key from a password using PBKDF2, with configurable salt, iterations, and hash algorithm.",
    Keywords = "pbkdf2,password,kdf,derive,rfc2898", Order = 30)]
public partial class Pbkdf2View : UserControl
{
    private bool _busy;

    public Pbkdf2View()
    {
        InitializeComponent();
        SaltBox.Text = Convert.ToHexString(Pbkdf2Tool.GenerateSalt(16)).ToLowerInvariant();
        UpdateIterationsWarning();
    }

    private string Algorithm => (AlgoCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "SHA-256";

    private void GenerateSalt_Click(object sender, RoutedEventArgs e) =>
        SaltBox.Text = Convert.ToHexString(Pbkdf2Tool.GenerateSalt(16)).ToLowerInvariant();

    private void IterationsBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // The XAML default Text fires this during InitializeComponent,
        // before IterationsWarning (declared later in the XAML) exists.
        if (IsLoaded)
            UpdateIterationsWarning();
    }

    private void UpdateIterationsWarning()
    {
        IterationsWarning.Visibility =
            int.TryParse(IterationsBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var iterations)
            && iterations < Pbkdf2Tool.OwaspMinimumIterations
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private async void Derive_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
            return;

        ErrorText.Visibility = Visibility.Collapsed;

        if (!int.TryParse(IterationsBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var iterations) || iterations < 1)
        {
            ShowError("Iterations must be a positive whole number.");
            return;
        }
        if (!int.TryParse(LengthBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var length) || length < 1)
        {
            ShowError("Length must be a positive whole number of bytes.");
            return;
        }

        byte[] salt;
        try
        {
            var saltText = new string(SaltBox.Text.Where(c => !char.IsWhiteSpace(c)).ToArray());
            salt = saltText.Length == 0 ? [] : Convert.FromHexString(saltText);
        }
        catch (FormatException)
        {
            ShowError("Salt must be valid hexadecimal.");
            return;
        }

        var password = PasswordBox.Text;
        var algorithm = Algorithm;

        _busy = true;
        DeriveBtn.IsEnabled = false;
        DeriveBtn.Content = "Deriving…";
        try
        {
            var hash = await Task.Run(() => Pbkdf2Tool.Derive(password, salt, iterations, algorithm, length));

            HexOutputBox.Text = Convert.ToHexString(hash).ToLowerInvariant();
            Base64OutputBox.Text = Convert.ToBase64String(hash);
            PhcOutputBox.Text = Pbkdf2Tool.FormatPhc(algorithm, iterations, salt, hash);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _busy = false;
            DeriveBtn.IsEnabled = true;
            DeriveBtn.Content = "Derive";
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void CopyHex_Click(object sender, RoutedEventArgs e) => Ui.Copy(HexOutputBox.Text, CopyHexBtn);

    private void CopyBase64_Click(object sender, RoutedEventArgs e) => Ui.Copy(Base64OutputBox.Text, CopyBase64Btn);

    private void CopyPhc_Click(object sender, RoutedEventArgs e) => Ui.Copy(PhcOutputBox.Text, CopyPhcBtn);
}
