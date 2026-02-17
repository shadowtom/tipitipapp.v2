using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using tipitipapp.domain.Entities.Models;
using tipitipapp.domain.EventArguments;
using tipitipapp.domain.Interfaces.Services;
using tipitipapp.Interfaces.Services;
using tipitipapp.Interfaces.Services;
using tipitipapp.Interfaces.Services;

namespace tipitipapp.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly INFCCardReaderService _nfcService;
    private readonly INavigationService _navigationService;
    private readonly IPopupService _popupService;

    private string _amount = string.Empty;
    private decimal _currentTipAmount;
    private bool _isProcessing;
    private string _processingMessage = string.Empty;
    private bool _hasTipAmount;
    private bool _isNfcAvailable;
    private string _nfcStatus = string.Empty;
    private List<NFCCardData> _recentTransactions = new();
    private NFCCardData? _lastCardRead;

    public MainPageViewModel(
        INFCCardReaderService nfcService,
        INavigationService navigationService,
        IPopupService popupService)
    {
        _nfcService = nfcService;
        _navigationService = navigationService;
        _popupService = popupService;

        // Initialize commands
        LogoutCommand = new Command(async () => await ExecuteLogoutCommand(), CanExecuteLogout);
        QuickTipCommand = new Command<string>(async (amount) => await ExecuteQuickTipCommand(amount), (s) => CanExecuteTransaction());
        CollectTipCommand = new Command(async () => await ExecuteCollectTipCommand(), CanExecuteTransaction);
        AmountChangedCommand = new Command(OnAmountChanged);
        ClearAmountCommand = new Command(ExecuteClearAmount);
        ViewTransactionsCommand = new Command(async () => await ExecuteViewTransactionsCommand());
        ToggleNfcCommand = new Command(async () => await ExecuteToggleNfcCommand());

        // Initialize properties
        LoadNfcStatusAsync().ConfigureAwait(false);
        LoadRecentTransactions();

        // Subscribe to NFC events
        _nfcService.OnStatusChanged += OnNfcStatusChanged;
        _nfcService.OnError += OnNfcError;
    }

    // Properties
    public string Amount
    {
        get => _amount;
        set
        {
            if (_amount != value)
            {
                _amount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValidAmount));
                OnPropertyChanged(nameof(AmountDisplay));
                ((Command)CollectTipCommand).ChangeCanExecute();
            }
        }
    }

    public string AmountDisplay => string.IsNullOrEmpty(Amount) ? "$0.00" : $"${Amount}";

    public bool IsValidAmount => !string.IsNullOrEmpty(Amount) &&
                                  decimal.TryParse(Amount, out decimal amt) &&
                                  amt > 0;

    public decimal CurrentTipAmount
    {
        get => _currentTipAmount;
        set
        {
            if (_currentTipAmount != value)
            {
                _currentTipAmount = value;
                HasTipAmount = value > 0;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentTipDisplay));
            }
        }
    }

    public string CurrentTipDisplay => $"${_currentTipAmount:F2}";

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            if (_isProcessing != value)
            {
                _isProcessing = value;
                OnPropertyChanged();
                ((Command)LogoutCommand).ChangeCanExecute();
                ((Command)QuickTipCommand).ChangeCanExecute();
                ((Command)CollectTipCommand).ChangeCanExecute();
            }
        }
    }

    public string ProcessingMessage
    {
        get => _processingMessage;
        set
        {
            if (_processingMessage != value)
            {
                _processingMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasTipAmount
    {
        get => _hasTipAmount;
        set
        {
            if (_hasTipAmount != value)
            {
                _hasTipAmount = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsNfcAvailable
    {
        get => _isNfcAvailable;
        set
        {
            if (_isNfcAvailable != value)
            {
                _isNfcAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NfcStatusColor));
                OnPropertyChanged(nameof(NfcStatusText));
            }
        }
    }

    public string NfcStatus
    {
        get => _nfcStatus;
        set
        {
            if (_nfcStatus != value)
            {
                _nfcStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NfcStatusColor));
                OnPropertyChanged(nameof(NfcStatusText));
            }
        }
    }

    public Color NfcStatusColor => IsNfcAvailable ?
        (NfcStatus.Contains("enabled") ? Colors.Green : Colors.Orange) :
        Colors.Red;

    public string NfcStatusText => !IsNfcAvailable ? "NFC Not Available" :
        (NfcStatus.Contains("enabled") ? "NFC Ready" : "NFC Disabled");

    public NFCCardData? LastCardRead
    {
        get => _lastCardRead;
        set
        {
            if (_lastCardRead != value)
            {
                _lastCardRead = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasLastCardRead));
                OnPropertyChanged(nameof(LastCardDisplay));
            }
        }
    }

    public bool HasLastCardRead => _lastCardRead != null;

    public string LastCardDisplay
    {
        get
        {
            if (_lastCardRead == null) return string.Empty;
            return $"Card: {_lastCardRead.CardUid[..8]}... Type: {_lastCardRead.CardType}";
        }
    }

    public List<NFCCardData> RecentTransactions
    {
        get => _recentTransactions;
        set
        {
            if (_recentTransactions != value)
            {
                _recentTransactions = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasRecentTransactions));
                OnPropertyChanged(nameof(RecentTransactionsCount));
            }
        }
    }

    public bool HasRecentTransactions => _recentTransactions.Any();
    public int RecentTransactionsCount => _recentTransactions.Count;

    // Commands
    public ICommand LogoutCommand { get; }
    public ICommand QuickTipCommand { get; }
    public ICommand CollectTipCommand { get; }
    public ICommand AmountChangedCommand { get; }
    public ICommand ClearAmountCommand { get; }
    public ICommand ViewTransactionsCommand { get; }
    public ICommand ToggleNfcCommand { get; }

    // Command execution checks
    private bool CanExecuteLogout() => !IsProcessing;
    private bool CanExecuteTransaction() => !IsProcessing && IsNfcAvailable;

    // Command implementations
    private async Task ExecuteLogoutCommand()
    {
        try
        {
            IsProcessing = true;
            ProcessingMessage = "Logging out...";

            // Stop NFC listening if active
            await _nfcService.StopListeningAsync();

            // Clear any stored session data
            await SecureStorage.Default.SetAsync("is_logged_in", "false");
            await SecureStorage.Default.SetAsync("user_email", string.Empty);

            // Navigate to login page
            await _navigationService.GoToLoginPageAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAlert("Logout failed", ex.Message);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task ExecuteQuickTipCommand(string amountString)
    {
        if (IsProcessing || !IsNfcAvailable) return;

        if (decimal.TryParse(amountString, out decimal amount))
        {
            await ProcessNFCPayment(amount, "Quick Tip");
        }
    }

    private async Task ExecuteCollectTipCommand()
    {
        if (IsProcessing || !IsNfcAvailable) return;

        if (!IsValidAmount)
        {
            await ShowErrorAlert("Invalid Amount", "Please enter a valid tip amount");
            return;
        }

        if (decimal.TryParse(Amount, out decimal amount))
        {
            await ProcessNFCPayment(amount, "Manual Tip");
        }
    }

    private async Task ExecuteToggleNfcCommand()
    {
        try
        {
            if (!IsNfcAvailable)
            {
                await ShowErrorAlert("NFC Not Available", "NFC is not available on this device");
                return;
            }

            var isEnabled = await _nfcService.IsNfcEnabled();

            if (!isEnabled)
            {
                var result = await Application.Current.MainPage.DisplayAlert(
                    "Enable NFC",
                    "NFC is currently disabled. Would you like to enable it?",
                    "Yes", "No");

                if (result)
                {
                    await _nfcService.RequestNfcEnable();

                    // Show instructions
                    await Application.Current.MainPage.DisplayAlert(
                        "NFC Settings",
                        "Please enable NFC in the settings panel that opened.",
                        "OK");
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(
                    "NFC Status",
                    "NFC is already enabled and ready to use.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAlert("NFC Error", ex.Message);
        }
    }

    private async Task ExecuteViewTransactionsCommand()
    {
        if (!RecentTransactions.Any())
        {
            await Application.Current.MainPage.DisplayAlert(
                "No Transactions",
                "No recent transactions found.",
                "OK");
            return;
        }

        // Show transactions in a simple alert for now
        var transactionList = string.Join("\n", RecentTransactions.Select(t =>
            $"${t.Amount:F2} - {t.CardUid[..8]} - {t.ReadTime:HH:mm}"));

        await Application.Current.MainPage.DisplayAlert(
            $"Recent Transactions ({RecentTransactionsCount})",
            transactionList,
            "Close");
    }

    private void ExecuteClearAmount()
    {
        Amount = string.Empty;
    }

    private async Task ProcessNFCPayment(decimal amount, string transactionType)
    {
        try
        {
            IsProcessing = true;
            ProcessingMessage = "Initializing NFC reader...";

            // Check NFC availability
            if (!await _nfcService.IsNfcAvailable())
            {
                await ShowErrorAlert("NFC Not Available", "NFC is not available on this device.");
                return;
            }

            // Check if NFC is enabled
            if (!await _nfcService.IsNfcEnabled())
            {
                var enableNfc = await Application.Current.MainPage.DisplayAlert(
                    "NFC Disabled",
                    "NFC is disabled. Would you like to enable it?",
                    "Yes", "No");

                if (enableNfc)
                {
                    await _nfcService.RequestNfcEnable();

                    // Wait a bit for user to enable NFC
                    for (int i = 0; i < 30; i++)
                    {
                        await Task.Delay(1000);
                        if (await _nfcService.IsNfcEnabled())
                            break;

                        if (i % 5 == 0)
                        {
                            ProcessingMessage = $"Waiting for NFC... {30 - i}s";
                        }
                    }

                    if (!await _nfcService.IsNfcEnabled())
                    {
                        await ShowErrorAlert("NFC Required", "Please enable NFC to process payments.");
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            // Show the NFC popup and wait for result
            ProcessingMessage = "Ready to scan. Place NFC card near reader...";

            var result = await _popupService.ShowNFCPopupAsync(amount, transactionType);

            if (result != null && result.Success && result.CardData != null)
            {
                // Transaction successful
                CurrentTipAmount = amount;

                // Store card data
                result.CardData.Amount = amount;
                result.CardData.TransactionType = transactionType;
                LastCardRead = result.CardData;

                // Add to recent transactions
                RecentTransactions.Insert(0, result.CardData);
                if (RecentTransactions.Count > 10)
                    RecentTransactions.RemoveAt(RecentTransactions.Count - 1);

                // Save to secure storage
                await SaveTransactionAsync(result.CardData);

                // Clear amount input
                Amount = string.Empty;

                // Show success message with card details
                var cardInfo = $"Card UID: {result.CardData.CardUid}\n" +
                              $"Type: {result.CardData.CardType}\n" +
                              $"Transaction ID: {result.TransactionId}";

                await Application.Current.MainPage.DisplayAlert(
                    "✅ Payment Successful",
                    $"Tip of ${amount:F2} processed!\n\n{cardInfo}",
                    "OK");
            }
            else
            {
                // Transaction failed
                await ShowErrorAlert(
                    "Transaction Failed",
                    result.ErrorMessage ?? "Failed to process payment. Please try again.");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAlert("NFC Error", ex.Message);
        }
        finally
        {
            IsProcessing = false;
            ProcessingMessage = string.Empty;
        }
    }

    private async Task LoadNfcStatusAsync()
    {
        try
        {
            IsNfcAvailable = await _nfcService.IsNfcAvailable();

            if (IsNfcAvailable)
            {
                var isEnabled = await _nfcService.IsNfcEnabled();
                NfcStatus = isEnabled ? "NFC is enabled" : "NFC is disabled";
            }
            else
            {
                NfcStatus = "NFC not available";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NFC status: {ex}");
            IsNfcAvailable = false;
            NfcStatus = "Error checking NFC";
        }
    }

    private void LoadRecentTransactions()
    {
        try
        {
            // Load from secure storage or local DB
            // For now, just initialize empty list
            RecentTransactions = new List<NFCCardData>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading transactions: {ex}");
            RecentTransactions = new List<NFCCardData>();
        }
    }

    private async Task SaveTransactionAsync(NFCCardData transaction)
    {
        try
        {
            // Save to secure storage or local DB
            // This is a placeholder - implement actual storage
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving transaction: {ex}");
        }
    }

    private void OnAmountChanged()
    {
        // Real-time validation
        if (!string.IsNullOrEmpty(Amount))
        {
            if (!decimal.TryParse(Amount, out _))
            {
                // Could show validation error
            }
        }
    }

    private void OnNfcStatusChanged(object? sender, NFCStatusChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            NfcStatus = e.Status;

            if (e.IsListening)
            {
                ProcessingMessage = "NFC reader is active. Place card near device...";
            }
        });
    }

    private void OnNfcError(object? sender, NFCErrorEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await ShowErrorAlert("NFC Error", e.ErrorMessage);
        });
    }

    private async Task ShowErrorAlert(string title, string message)
    {
        if (!IsProcessing)
        {
            await Application.Current.MainPage.DisplayAlert($"❌ {title}", message, "OK");
        }
    }

    public void Dispose()
    {
        _nfcService.OnStatusChanged -= OnNfcStatusChanged;
        _nfcService.OnError -= OnNfcError;

        // Clean up any resources
        Task.Run(async () => await _nfcService.StopListeningAsync());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}