using System.Diagnostics;
using System.Drawing;

namespace EinkPenInjector;

internal sealed class MainForm : Form
{
    private const string ProjectUrl = "https://github.com/rschoene/w11_driver_thinkbook_plus_virtual_pen";

    private readonly PenForwardingService _service = new();
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly ComboBox _frontCombo = CreateCombo();
    private readonly ComboBox _secondCombo = CreateCombo();
    private readonly Button _startButton = new() { AutoSize = true, Text = Loc.Get("Start") };
    private readonly Button _stopButton = new() { AutoSize = true, Text = Loc.Get("Stop"), Enabled = false };
    private readonly Label _statusLabel = new() { AutoSize = true, Text = Loc.Get("StatusStopped") };
    private readonly NotifyIcon _tray = new();
    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private bool _exiting;

    public MainForm()
    {
        Text = Loc.Get("WindowTitle");
        Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        Icon = icon;
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        int textWidth = (int)(420 * DeviceDpi / 96.0);
        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Dock = DockStyle.Fill
        };

        var title = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(textWidth, 0),
            Text = Loc.Get("WindowTitle"),
            Font = new Font(Font.FontFamily, Font.Size * 1.4f, FontStyle.Bold),
            Margin = new Padding(3, 3, 3, 10)
        };
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(textWidth, 0),
            Text = Loc.Get("Description"),
            Margin = new Padding(3, 3, 3, 12)
        };
        var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 8, 0, 8) };
        buttons.Controls.Add(_startButton);
        buttons.Controls.Add(_stopButton);
        _statusLabel.MaximumSize = new Size(textWidth, 0);
        var license = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(textWidth, 0),
            Text = Loc.Get("LicenseText"),
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 20, 3, 0)
        };
        var link = new LinkLabel { AutoSize = true, Text = Loc.Get("LicenseLink") };
        link.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(ProjectUrl) { UseShellExecute = true });

        layout.Controls.Add(title);
        layout.Controls.Add(description);
        layout.Controls.Add(new Label { AutoSize = true, Text = Loc.Get("FrontButton") });
        layout.Controls.Add(_frontCombo);
        layout.Controls.Add(new Label { AutoSize = true, Text = Loc.Get("SecondButton"), Margin = new Padding(3, 10, 3, 3) });
        layout.Controls.Add(_secondCombo);
        layout.Controls.Add(buttons);
        layout.Controls.Add(_statusLabel);
        layout.Controls.Add(license);
        layout.Controls.Add(link);
        Controls.Add(layout);
        DarkTheme.Apply(this);
        title.ForeColor = Color.White;
        license.ForeColor = DarkTheme.TextMuted;

        FillCombo(_frontCombo, _settings.FrontButton);
        FillCombo(_secondCombo, _settings.SecondButton);
        _frontCombo.SelectedIndexChanged += (_, _) =>
        {
            _settings.FrontButton = SelectedAction(_frontCombo);
            _settings.Save();
        };
        _secondCombo.SelectedIndexChanged += (_, _) =>
        {
            _settings.SecondButton = SelectedAction(_secondCombo);
            _settings.Save();
        };

        _startButton.Click += (_, _) => StartForwarding();
        _stopButton.Click += (_, _) => StopForwarding();

        var menu = new ContextMenuStrip
        {
            Renderer = new ToolStripProfessionalRenderer(new DarkTheme.MenuColors()),
            BackColor = DarkTheme.Surface,
            ForeColor = DarkTheme.Text,
            ShowImageMargin = false
        };
        menu.Items.Add(Loc.Get("TrayOpen"), null, (_, _) => ShowFromTray());
        menu.Items.Add(Loc.Get("TrayExit"), null, (_, _) => ExitApp());
        _tray.Icon = icon;
        _tray.Text = Loc.Get("TrayTooltip");
        _tray.ContextMenuStrip = menu;
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowFromTray();
            }
        };
        _tray.Visible = true;

        Shown += (_, _) => StartForwarding();
    }

    private static ComboBox CreateCombo() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 280
    };

    private static void FillCombo(ComboBox combo, ButtonAction selected)
    {
        foreach (ButtonAction action in Enum.GetValues(typeof(ButtonAction)))
        {
            combo.Items.Add(new ActionItem(action));
        }

        combo.SelectedIndex = (int)selected;
    }

    private static ButtonAction SelectedAction(ComboBox combo) => ((ActionItem)combo.SelectedItem!).Action;

    private async void StartForwarding()
    {
        if (_cts is not null)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        _cts = cts;
        _startButton.Enabled = false;
        _stopButton.Enabled = true;
        SetStatus(Loc.Get("StatusStarting"));

        try
        {
            _runTask = Task.Run(() => _service.RunAsync(_settings, SetStatus, cts.Token));
            await _runTask;
            SetStatus(Loc.Get("StatusStopped"));
        }
        catch (Exception exception) when (!cts.IsCancellationRequested)
        {
            SetStatus(Loc.Format("StatusForwarderStopped", exception.Message));
        }
        catch (Exception)
        {
            SetStatus(Loc.Get("StatusStopped"));
        }
        finally
        {
            cts.Dispose();
            _cts = null;
            _runTask = null;
            _startButton.Enabled = true;
            _stopButton.Enabled = false;
        }
    }

    private void StopForwarding()
    {
        SetStatus(Loc.Get("StatusStopping"));
        _cts?.Cancel();
    }

    private void SetStatus(string message)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(() => _statusLabel.Text = message));
            }
            catch (InvalidOperationException)
            {
            }
        }
        else
        {
            _statusLabel.Text = message;
        }
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApp()
    {
        _exiting = true;
        _cts?.Cancel();
        Close();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        DarkTheme.ApplyTitleBar(Handle);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_exiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _cts?.Cancel();
        try
        {
            _runTask?.Wait(2000);
        }
        catch (AggregateException)
        {
        }

        _tray.Visible = false;
        _tray.Dispose();
        base.OnFormClosing(e);
    }

    private sealed class ActionItem
    {
        public ActionItem(ButtonAction action) => Action = action;

        public ButtonAction Action { get; }

        public override string ToString() => Loc.Get("Action_" + Action);
    }
}
