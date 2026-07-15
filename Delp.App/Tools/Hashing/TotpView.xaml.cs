using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;
using Delp.Core.Tools.Hashing;

namespace Delp.App.Tools.Hashing;

[Tool("totp", "TOTP Code Generator", ToolCategory.Hashing,
    "Generate time-based one-time passcodes from a Base32 secret or an otpauth:// URI, for testing 2FA flows.",
    Keywords = "totp,otp,2fa,mfa,authenticator,rfc 6238,hotp,code", Order = 60)]
public partial class TotpView : UserControl
{
    private bool _updatingControls;
    private OtpConfig _config = OtpConfig.Default;
    private byte[] _secretBytes = [];
    private string _currentCode = "";

    // Ticks the display every 500 ms while the tool is on screen. Started/stopped from
    // IsVisibleChanged rather than Loaded/Unloaded: this view can stay "loaded" while hidden behind
    // another tool in the flyout's cached content, so Unloaded isn't a reliable stop signal here — a
    // timer left running after that would silently burn CPU forever (a real red-team finding).
    private DispatcherTimer? _timer;

    public TotpView()
    {
        InitializeComponent();
        DigitsBox.SelectedIndex = 0; // 6
        AlgorithmBox.SelectedIndex = 0; // SHA1
        PeriodBox.Text = "30";
        SecretBox.Text = "JBSWY3DPEHPK3PXP"; // demo secret so the tool is immediately useful
    }

    private void TotpView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            StartTimer();
        else
            StopTimer();
    }

    private void StartTimer()
    {
        if (_timer is not null)
            return;

        RebuildConfig();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => RefreshCode();
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
    }

    private void Secret_Changed(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded && !_updatingControls)
            RebuildConfig();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded && !_updatingControls)
            RebuildConfig();
    }

    private int SelectedDigits => ((DigitsBox.SelectedItem as ComboBoxItem)?.Tag as string) switch
    {
        "7" => 7,
        "8" => 8,
        _ => 6,
    };

    private int SelectedPeriod
    {
        get
        {
            if (!int.TryParse(PeriodBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var period) || period < 1)
                throw new FormatException("Period must be a positive whole number of seconds.");
            return period;
        }
    }

    private TotpAlgorithm SelectedAlgorithm => ((AlgorithmBox.SelectedItem as ComboBoxItem)?.Tag as string) switch
    {
        "SHA256" => TotpAlgorithm.Sha256,
        "SHA512" => TotpAlgorithm.Sha512,
        _ => TotpAlgorithm.Sha1,
    };

    /// <summary>Re-parses the secret/options into an <see cref="OtpConfig"/>, re-renders the QR code, and refreshes the current code. Runs on every input change; the 500 ms ticker calls the cheaper <see cref="RefreshCode"/> instead.</summary>
    private void RebuildConfig() => Run(() =>
    {
        var text = SecretBox.Text.Trim();
        if (text.Length == 0)
        {
            _secretBytes = [];
            CurrentCodeText.Text = PrevCodeText.Text = NextCodeText.Text = CountdownText.Text = "";
            QrImage.Source = null;
            throw new FormatException("Enter a Base32 secret or an otpauth:// URI.");
        }

        OtpConfig config;
        if (text.StartsWith("otpauth:", StringComparison.OrdinalIgnoreCase))
        {
            config = TotpTool.ParseOtpAuthUri(text);
            SyncControlsFromConfig(config);
        }
        else
        {
            config = new OtpConfig(text, null, null, SelectedDigits, SelectedPeriod, SelectedAlgorithm);
        }

        _secretBytes = TotpTool.DecodeBase32(config.Secret);
        _config = config;

        RenderQr(config);
        RefreshCode();
    });

    private void SyncControlsFromConfig(OtpConfig config)
    {
        _updatingControls = true;
        try
        {
            DigitsBox.SelectedIndex = config.Digits switch { 7 => 1, 8 => 2, _ => 0 };
            AlgorithmBox.SelectedIndex = config.Algorithm switch { TotpAlgorithm.Sha256 => 1, TotpAlgorithm.Sha512 => 2, _ => 0 };
            PeriodBox.Text = config.PeriodSeconds.ToString(CultureInfo.InvariantCulture);
        }
        finally
        {
            _updatingControls = false;
        }
    }

    private void RenderQr(OtpConfig config)
    {
        var forQr = string.IsNullOrEmpty(config.Account) ? config with { Account = "Delp" } : config;
        var uri = TotpTool.BuildOtpAuthUri(forQr);
        var png = QrTool.CreatePng(uri, pixelsPerModule: 6, QrEccLevel.M);
        QrImage.Source = ToBitmapImage(png);
    }

    /// <summary>Cheap per-tick refresh: recomputes the current/previous/next codes and the countdown bar from the already-validated <see cref="_config"/>/<see cref="_secretBytes"/> — no re-parsing or QR re-render.</summary>
    private void RefreshCode()
    {
        if (_secretBytes.Length == 0)
            return;

        var now = DateTimeOffset.UtcNow;
        _currentCode = TotpTool.TotpCode(_secretBytes, now, _config.Digits, _config.PeriodSeconds, _config.Algorithm);
        var prevCode = TotpTool.TotpCode(_secretBytes, now.AddSeconds(-_config.PeriodSeconds), _config.Digits, _config.PeriodSeconds, _config.Algorithm);
        var nextCode = TotpTool.TotpCode(_secretBytes, now.AddSeconds(_config.PeriodSeconds), _config.Digits, _config.PeriodSeconds, _config.Algorithm);

        CurrentCodeText.Text = GroupCode(_currentCode);
        PrevCodeText.Text = GroupCode(prevCode);
        NextCodeText.Text = GroupCode(nextCode);

        var remaining = TotpTool.SecondsRemaining(now, _config.PeriodSeconds);
        CountdownText.Text = $"{remaining}s remaining in this code's window";
        RemainingCol.Width = new GridLength(remaining, GridUnitType.Star);
        ElapsedCol.Width = new GridLength(Math.Max(_config.PeriodSeconds - remaining, 0), GridUnitType.Star);
    }

    private static string GroupCode(string code) =>
        code.Length < 6 ? code : $"{code[..(code.Length / 2)]} {code[(code.Length / 2)..]}";

    private static BitmapImage ToBitmapImage(byte[] bytes)
    {
        var image = new BitmapImage();
        using var stream = new MemoryStream(bytes);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(_currentCode, CopyBtn);

    /// <summary>Runs a step with inline error reporting (no reentrancy guard needed: this view has no bidirectional pane pair to protect from feedback loops).</summary>
    private void Run(Action action)
    {
        try
        {
            action();
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
