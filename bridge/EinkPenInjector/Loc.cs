using System.Globalization;

namespace EinkPenInjector;

// Strings are in code (not .resx) so the app stays a single executable without satellite assemblies.
internal static class Loc
{
    private static readonly bool German =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";

    private static readonly Dictionary<string, (string En, string De)> Strings = new()
    {
        ["WindowTitle"] = ("ThinkBook E-Ink pen forwarder", "ThinkBook E-Ink-Stift-Weiterleitung"),
        ["Description"] = (
            "Makes the ThinkBook E-Ink pen work as a regular Windows pen. Closing the window keeps it running in the notification area.",
            "Macht den ThinkBook E-Ink-Stift als normalen Windows-Stift nutzbar. Beim Schließen des Fensters läuft die App im Infobereich weiter."),
        ["FrontButton"] = ("Front pen button", "Vordere Stifttaste"),
        ["SecondButton"] = ("Second pen button", "Zweite Stifttaste"),
        ["Start"] = ("Start", "Starten"),
        ["Stop"] = ("Stop", "Beenden"),
        ["StatusStopped"] = ("Stopped.", "Angehalten."),
        ["StatusStarting"] = ("Starting...", "Wird gestartet..."),
        ["StatusStopping"] = ("Stopping...", "Wird beendet..."),
        ["StatusForwarderStopped"] = ("Forwarder stopped: {0}", "Weiterleitung beendet: {0}"),
        ["StatusSearching"] = ("Waiting for the pen...", "Warte auf den Stift..."),
        ["StatusOpening"] = ("Pen found, connecting...", "Stift gefunden, verbinde..."),
        ["StatusConnected"] = ("Pen connected and active.", "Stift verbunden und aktiv."),
        ["StatusInvalidReport"] = (
            "Received unexpected data from the pen; ignoring it.",
            "Unerwartete Daten vom Stift erhalten; werden ignoriert."),
        ["StatusStreamEnded"] = (
            "Connection to the pen lost; reconnecting...",
            "Verbindung zum Stift verloren; verbinde neu..."),
        ["StatusHidReadFailed"] = (
            "Could not read from the pen ({0}). Retrying...",
            "Stift konnte nicht gelesen werden ({0}). Neuer Versuch..."),
        ["StatusWin32Failed"] = (
            "Pen input failed ({0}). Retrying...",
            "Stifteingabe fehlgeschlagen ({0}). Neuer Versuch..."),
        ["ErrorDisplaySize"] = (
            "Could not determine primary-display dimensions.",
            "Die Abmessungen des Hauptdisplays konnten nicht ermittelt werden."),
        ["ErrorCreatePointer"] = (
            "Could not create a synthetic pen pointer.",
            "Es konnte kein synthetischer Stiftzeiger erstellt werden."),
        ["ErrorInjectRejected"] = (
            "Windows rejected synthetic pen input.",
            "Windows hat die synthetische Stifteingabe abgelehnt."),
        ["ErrorSendKeys"] = (
            "Could not send the keyboard shortcut.",
            "Das Tastaturkürzel konnte nicht gesendet werden."),
        ["Action_None"] = ("Do nothing", "Nichts tun"),
        ["Action_BarrelButton"] = ("Pen barrel button", "Stift-Seitentaste"),
        ["Action_EraserButton"] = ("Eraser", "Radierer"),
        ["Action_RightClick"] = ("Right click", "Rechtsklick"),
        ["Action_Undo"] = ("Undo (Ctrl+Z)", "Rückgängig (Strg+Z)"),
        ["Action_Redo"] = ("Redo (Ctrl+Y)", "Wiederholen (Strg+Y)"),
        ["TrayTooltip"] = ("ThinkBook E-Ink pen", "ThinkBook E-Ink-Stift"),
        ["TrayOpen"] = ("Open", "Öffnen"),
        ["TrayExit"] = ("Exit", "Beenden"),
        ["LicenseText"] = (
            "Free software under the GNU GPL v3, without any warranty.",
            "Freie Software unter der GNU GPL v3, ohne jegliche Gewährleistung."),
        ["LicenseLink"] = ("Source code, license and notices", "Quellcode, Lizenz und Hinweise"),
    };

    public static string Get(string key) =>
        Strings.TryGetValue(key, out var value) ? (German ? value.De : value.En) : key;

    public static string Format(string key, params object[] args) =>
        string.Format(Get(key), args);
}
