using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using tipitipapp.domain.Entities.Models;
using tipitipapp.domain.EventArguments;
using tipitipapp.domain.Interfaces.Services;

namespace tipitipapp.ViewModels;

public class NFCPopupViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly INFCCardReaderService _nfcService;
    private readonly decimal _amount;
    private readonly string _transactionType;
    private CancellationTokenSource? _cts;

    private string _statusMessage = "Ready to scan NFC card...";
    private bool _isReading = true;
    private bool _isComplete;
    private string _resultIcon = string.Empty;
    private bool _isSuccess;
    private NFCCardData? _cardData;
    private string _transactionId = string.Empty;
    private int _timeoutSeconds = 30;

    public event EventHandler<NFCTransactionEventArgs>? TransactionCompleted;
    public event EventHandler<bool>? CloseRequested;

    public NFCPopupViewModel(decimal amount, string transactionType, INFCCardReaderService nfcService)
    {
        _amount = amount;
        _transactionType = transactionType;
        _nfcService = nfcService;

        CancelCommand = new Command(ExecuteCancelCommand);
        CloseCommand = new Command(ExecuteCloseCommand);
        RetryCommand = new Command(ExecuteRetryCommand);

        // Subscribe to NFC events
        _nfcService.OnDeviceDetected += OnNfcDeviceDetected;
        _nfcService.OnError += OnNfcError;
        _nfcService.OnStatusChanged += OnNfcStatusChanged;

        // Start listening and timeout timer
        Task.Run(StartNfcListening);
        Task.Run(StartTimeoutTimer);
    }

    public string Amount => _amount.ToString("F2");
    public string TransactionType => _transactionType;

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsReading
    {
        get => _isReading;
        set
        {
            if (_isReading != value)
            {
                _isReading = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsComplete
    {
        get => _isComplete;
        set
        {
            if (_isComplete != value)
            {
                _isComplete = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsSuccess
    {
        get => _isSuccess;
        set
        {
            if (_isSuccess != value)
            {
                _isSuccess = value;
                OnPropertyChanged();
            }
        }
    }

    public string ResultIcon
    {
        get => _resultIcon;
        set
        {
            if (_resultIcon != value)
            {
                _resultIcon = value;
                OnPropertyChanged();
            }
        }
    }

    public NFCCardData? CardData
    {
        get => _cardData;
        set
        {
            if (_cardData != value)
            {
                _cardData = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CardInfoDisplay));
                OnPropertyChanged(nameof(HasCardData));
            }
        }
    }

    public bool HasCardData => _cardData != null;

    public string CardInfoDisplay
    {
        get
        {
            if (CardData == null) return string.Empty;

            var info = new System.Text.StringBuilder();
            info.AppendLine($"📱 UID: {CardData.CardUid}");
            info.AppendLine($"💳 Type: {CardData.CardType}");
            info.AppendLine($"🔧 Technologies: {CardData.Technologies.Count}");
            info.AppendLine($"📝 Writable: {(CardData.IsWritable ? "Yes" : "No")}");

            if (CardData.NdefRecords.Any())
            {
                info.AppendLine("\n📄 Data on card:");
                foreach (var record in CardData.NdefRecords)
                {
                    if (!string.IsNullOrEmpty(record.Payload))
                    {
                        info.AppendLine($"   • {record.Payload}");
                    }
                }
            }

            return info.ToString();
        }
    }

    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set
        {
            if (_timeoutSeconds != value)
            {
                _timeoutSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimeoutDisplay));
            }
        }
    }

    public string TimeoutDisplay => $"Timeout in: {_timeoutSeconds}s";

    public ICommand CancelCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand RetryCommand { get; }

    private async Task StartNfcListening()
    {
        try
        {
            _cts = new CancellationTokenSource();

            // Check NFC availability
            if (!await _nfcService.IsNfcAvailable())
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    StatusMessage = "❌ NFC is not available";
                    IsReading = false;
                    IsComplete = true;
                    IsSuccess = false;
                    ResultIcon = "error_icon.png";
                });
                return;
            }

            // Check if NFC is enabled
            if (!await _nfcService.IsNfcEnabled())
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    StatusMessage = "⚠️ Please enable NFC";
                    IsReading = false;
                    IsComplete = true;
                    IsSuccess = false;
                    ResultIcon = "warning_icon.png";
                });
                return;
            }

            // Start listening
            await _nfcService.StartListeningAsync(_cts.Token);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusMessage = "📱 Place NFC card near reader...";
            });
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                IsReading = false;
                IsComplete = true;
                IsSuccess = false;
                ResultIcon = "error_icon.png";
            });
        }
    }

    private async Task StartTimeoutTimer()
    {
        while (_isReading && _timeoutSeconds > 0 && !_cts?.IsCancellationRequested == true)
        {
            await Task.Delay(1000);
            if (_isReading)
            {
                _timeoutSeconds--;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnPropertyChanged(nameof(TimeoutSeconds));
                    OnPropertyChanged(nameof(TimeoutDisplay));
                });

                if (_timeoutSeconds == 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        StatusMessage = "⏰ Timeout - No card detected";
                        IsReading = false;
                        IsComplete = true;
                        IsSuccess = false;
                        ResultIcon = "timeout_icon.png";
                    });

                    await _nfcService.StopListeningAsync();
                    break;
                }
            }
        }
    }

    private void OnNfcDeviceDetected(object? sender, NFCDeviceDetectedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            CardData = e.CardData;
            _transactionId = GenerateTransactionId();

            if (e.Success)
            {
                StatusMessage = "✅ Card read successfully!";
                IsSuccess = true;
                ResultIcon = "success_icon.png";

                // Add transaction metadata
                e.CardData.Amount = _amount;
                e.CardData.TransactionType = _transactionType;
                e.CardData.TransactionId = _transactionId;
            }
            else
            {
                StatusMessage = $"❌ Failed: {e.CardData.ErrorMessage}";
                IsSuccess = false;
                ResultIcon = "error_icon.png";
            }

            IsReading = false;
            IsComplete = true;

            // Stop listening
            await _nfcService.StopListeningAsync();

            // Raise completion event
            TransactionCompleted?.Invoke(this, new NFCTransactionEventArgs
            {
                Success = IsSuccess,
                Amount = _amount,
                TransactionId = _transactionId,
                ErrorMessage = IsSuccess ? null : e.CardData.ErrorMessage,
                CardData = e.CardData
            });
        });
    }

    private void OnNfcError(object? sender, NFCErrorEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            StatusMessage = $"❌ Error: {e.ErrorMessage}";

            if (!IsComplete)
            {
                IsReading = false;
                IsComplete = true;
                IsSuccess = false;
                ResultIcon = "error_icon.png";

                await _nfcService.StopListeningAsync();

                TransactionCompleted?.Invoke(this, NFCTransactionEventArgs.CreateFailure(_amount, e.ErrorMessage, _transactionType));
            }
        });
    }

    private void OnNfcStatusChanged(object? sender, NFCStatusChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!IsComplete && IsReading)
            {
                StatusMessage = e.Status;
            }
        });
    }

    private string GenerateTransactionId()
    {
        return $"TXN{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
    }

    private async void ExecuteRetryCommand()
    {
        // Reset state
        IsReading = true;
        IsComplete = false;
        IsSuccess = false;
        CardData = null;
        _timeoutSeconds = 30;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        // Start again
        await StartNfcListening();
        Task.Run(StartTimeoutTimer);
    }

    private async void ExecuteCancelCommand()
    {
        await _nfcService.StopListeningAsync();
        CloseRequested?.Invoke(this, false);
    }

    private async void ExecuteCloseCommand()
    {
        await _nfcService.StopListeningAsync();
        CloseRequested?.Invoke(this, IsSuccess);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();

        _nfcService.OnDeviceDetected -= OnNfcDeviceDetected;
        _nfcService.OnError -= OnNfcError;
        _nfcService.OnStatusChanged -= OnNfcStatusChanged;

        Task.Run(async () => await _nfcService.StopListeningAsync());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}