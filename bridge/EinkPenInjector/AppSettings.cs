namespace EinkPenInjector;

internal enum ButtonAction
{
    None,
    BarrelButton,
    EraserButton,
    RightClick,
    Undo,
    Redo
}

internal sealed class AppSettings
{
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EinkPenInjector", "settings.ini");

    public ButtonAction FrontButton { get; set; } = ButtonAction.BarrelButton;

    public ButtonAction SecondButton { get; set; } = ButtonAction.None;

    public static AppSettings Load()
    {
        var settings = new AppSettings();
        try
        {
            if (File.Exists(SettingsPath))
            {
                foreach (string line in File.ReadAllLines(SettingsPath))
                {
                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2 || !Enum.TryParse(parts[1].Trim(), out ButtonAction action))
                    {
                        continue;
                    }

                    switch (parts[0].Trim())
                    {
                        case "FrontButton": settings.FrontButton = action; break;
                        case "SecondButton": settings.SecondButton = action; break;
                    }
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return settings;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllLines(SettingsPath, new[] { $"FrontButton={FrontButton}", $"SecondButton={SecondButton}" });
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
