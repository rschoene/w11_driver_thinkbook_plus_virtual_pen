using System.ComponentModel;
using EinkPenBridge;

namespace EinkPenInjector;

internal sealed class PenForwardingService
{
    private const int SourceReportLength = 18;

    public async Task RunAsync(AppSettings settings, Action<string> reportStatus, CancellationToken cancellationToken)
    {
        using var forwarder = new PenInputForwarder(settings);
        try
        {
            byte[] reportBuffer = new byte[SourceReportLength];

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string? path = HidSourceReader.FindSourcePath();
                    if (path is null)
                    {
                        reportStatus(Loc.Get("StatusSearching"));
                    }
                    else
                    {
                        reportStatus(Loc.Get("StatusOpening"));
                        using FileStream input = HidSourceReader.OpenReadOnly(path);
                        // A pending overlapped read does not observe the token; closing the stream aborts it.
                        using CancellationTokenRegistration registration = cancellationToken.Register(input.Dispose);
                        reportStatus(Loc.Get("StatusConnected"));
                        bool invalidReportReported = false;

                        while (!cancellationToken.IsCancellationRequested &&
                            await ReadReportAsync(input, reportBuffer).ConfigureAwait(false))
                        {
                            if (ReportMapping.TryParse(reportBuffer, out VirtualPenReport penReport))
                            {
                                bool secondButton = (penReport.Flags & 0x04) != 0 && (reportBuffer[1] & 0x02) != 0;
                                forwarder.Forward(penReport, secondButton);
                                if (invalidReportReported)
                                {
                                    reportStatus(Loc.Get("StatusConnected"));
                                    invalidReportReported = false;
                                }
                            }
                            else if (!invalidReportReported)
                            {
                                forwarder.Cancel();
                                reportStatus(Loc.Get("StatusInvalidReport"));
                                invalidReportReported = true;
                            }
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            reportStatus(Loc.Get("StatusStreamEnded"));
                        }
                    }
                }
                catch (Exception) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (IOException exception)
                {
                    reportStatus(Loc.Format("StatusHidReadFailed", exception.Message));
                }
                catch (Win32Exception exception)
                {
                    reportStatus(Loc.Format("StatusWin32Failed", exception.Message));
                }
                finally
                {
                    forwarder.Cancel();
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            forwarder.Cancel();
        }
    }

    private static async Task<bool> ReadReportAsync(FileStream input, byte[] buffer)
    {
        int received = 0;
        while (received < buffer.Length)
        {
            int read = await input.ReadAsync(buffer, received, buffer.Length - received).ConfigureAwait(false);
            if (read == 0)
            {
                return false;
            }

            received += read;
        }

        return true;
    }
}
