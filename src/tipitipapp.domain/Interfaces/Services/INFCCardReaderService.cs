using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tipitipapp.domain.EventArguments;

namespace tipitipapp.domain.Interfaces.Services
{
    public interface INFCCardReaderService
    {
        Task<bool> IsNfcAvailable();
        Task<bool> IsNfcEnabled();
        Task<bool> RequestNfcEnable();
        Task StartListeningAsync(CancellationToken cancellationToken = default);
        Task StopListeningAsync();
        event EventHandler<NFCDeviceDetectedEventArgs> OnDeviceDetected;
        event EventHandler<NFCErrorEventArgs> OnError;
        event EventHandler<NFCStatusChangedEventArgs> OnStatusChanged;
    }
}
