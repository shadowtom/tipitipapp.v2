using System;
using System.Threading;
using System.Threading.Tasks;
using tipitipapp.domain.Entities.Models;
using tipitipapp.domain.EventArguments;
using tipitipapp.domain.Interfaces.Services;

namespace tipitipapp.Infrastructure.Services;

public class NFCCardReaderService : INFCCardReaderService, IDisposable
{
    private readonly SemaphoreSlim _nfcSemaphore = new(1, 1);
    private CancellationTokenSource? _cts;
    private bool _isListening;
    private bool _disposed;

#if ANDROID
    // Android-specific implementation using Plugin.NFC
    private readonly Plugin.NFC.INFC _nfc;

    public NFCCardReaderService()
    {
        _nfc = Plugin.NFC.CrossNFC.Current;

        // Do not subscribe directly to plugin events here to avoid delegate signature issues.
        // If needed, subscribe in platform-specific code or adapt handlers to the exact delegate types.
    }
#else
    public NFCCardReaderService()
    {
        // Non-Android platforms: no native NFC support in this implementation
    }
#endif

    // Events
    public event EventHandler<NFCDeviceDetectedEventArgs>? OnDeviceDetected;
    public event EventHandler<NFCErrorEventArgs>? OnError;
    public event EventHandler<NFCStatusChangedEventArgs>? OnStatusChanged;

    public async Task<bool> IsNfcAvailable()
    {
#if ANDROID
        try
        {
            return await Task.FromResult(Plugin.NFC.CrossNFC.IsSupported && _nfc.IsAvailable);
        }
        catch
        {
            return false;
        }
#else
        return await Task.FromResult(false);
#endif
    }

    public async Task<bool> IsNfcEnabled()
    {
#if ANDROID
        try
        {
            return await Task.FromResult(Plugin.NFC.CrossNFC.IsSupported && _nfc.IsEnabled);
        }
        catch
        {
            return false;
        }
#else
        return await Task.FromResult(false);
#endif
    }

    public async Task<bool> RequestNfcEnable()
    {
#if ANDROID
        try
        {
            if (!Plugin.NFC.CrossNFC.IsSupported)
            {
                OnError?.Invoke(this, new NFCErrorEventArgs { ErrorMessage = "NFC is not supported on this device" });
                return false;
            }

            if (!_nfc.IsAvailable)
            {
                OnError?.Invoke(this, new NFCErrorEventArgs { ErrorMessage = "NFC hardware is not available" });
                return false;
            }

            if (!_nfc.IsEnabled)
            {
                try
                {
                    var openMethod = _nfc.GetType().GetMethod("OpenNfcSettings");
                    openMethod?.Invoke(_nfc, null);
                }
                catch { }

                OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs { Status = "Please enable NFC in settings", IsListening = false });
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new NFCErrorEventArgs { ErrorMessage = $"Failed to check NFC: {ex.Message}", Exception = ex });
            return false;
        }
#else
        OnError?.Invoke(this, new NFCErrorEventArgs { ErrorMessage = "NFC not implemented on this platform." });
        return await Task.FromResult(false);
#endif
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        await _nfcSemaphore.WaitAsync(cancellationToken);

        try
        {
            if (_isListening)
                return;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

#if ANDROID
            if (!Plugin.NFC.CrossNFC.IsSupported)
                throw new InvalidOperationException("NFC is not supported on this device");

            if (!_nfc.IsAvailable)
                throw new InvalidOperationException("NFC hardware is not available");

            if (!_nfc.IsEnabled)
            {
                OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs { Status = "NFC is disabled. Please enable it in settings.", IsListening = false });
                try { _nfc.GetType().GetMethod("OpenNfcSettings")?.Invoke(_nfc, null); } catch { }
                return;
            }

            // Start listening - use StartListening if async not available
            try
            {
                var startMethod = _nfc.GetType().GetMethod("StartListeningAsync");
                if (startMethod != null)
                {
                    var task = (Task)startMethod.Invoke(_nfc, null)!;
                    await task.ConfigureAwait(false);
                }
                else
                {
                    var startSync = _nfc.GetType().GetMethod("StartListening");
                    startSync?.Invoke(_nfc, null);
                }
            }
            catch
            {
                // ignore
            }

            _isListening = true;
            OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs { Status = "Listening for NFC cards...", IsListening = true });
#else
            // Non-Android: do nothing
            _isListening = true;
            OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs { Status = "NFC not supported on this platform.", IsListening = false });
#endif
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new NFCErrorEventArgs { ErrorMessage = $"Failed to start listening: {ex.Message}", Exception = ex });
        }
        finally
        {
            _nfcSemaphore.Release();
        }
    }

    public async Task StopListeningAsync()
    {
        await _nfcSemaphore.WaitAsync();

        try
        {
            if (!_isListening)
                return;

#if ANDROID
            try
            {
                var stopMethod = _nfc.GetType().GetMethod("StopListeningAsync");
                if (stopMethod != null)
                {
                    var task = (Task)stopMethod.Invoke(_nfc, null)!;
                    await task.ConfigureAwait(false);
                }
                else
                {
                    var stopSync = _nfc.GetType().GetMethod("StopListening");
                    stopSync?.Invoke(_nfc, null);
                }
            }
            catch { }
#endif

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _isListening = false;

            OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs { Status = "Stopped listening", IsListening = false });
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new NFCErrorEventArgs { ErrorMessage = $"Failed to stop listening: {ex.Message}", Exception = ex });
        }
        finally
        {
            _nfcSemaphore.Release();
        }
    }

    // Android: optional event handlers (adapt to Plugin.NFC version)
#if ANDROID
    private void OnTagDiscovered(object sender, EventArgs e)
    {
        OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs { Status = "NFC tag detected, reading...", IsListening = true });
    }

    private void OnNfcStatusChanged(object sender, EventArgs e)
    {
        OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs { Status = "NFC status changed", IsListening = _isListening });
    }

    private void OnTagListeningStatusChanged(object sender, EventArgs e)
    {
        // best-effort: try to get IsListening property from event args
        try
        {
            var isListeningProp = e.GetType().GetProperty("IsListening");
            if (isListeningProp != null)
            {
                var val = (bool)isListeningProp.GetValue(e)!;
                _isListening = val;
            }
        }
        catch { }

        OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs { Status = _isListening ? "Listening..." : "Not listening", IsListening = _isListening });
    }
#endif

    public void Dispose()
    {
        if (_disposed) return;

        // Do not attempt to unsubscribe plugin events here to avoid delegate signature issues.

        _cts?.Cancel();
        _cts?.Dispose();
        _nfcSemaphore?.Dispose();
        _disposed = true;
    }
}
