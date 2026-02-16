using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using tipitipapp.domain.EventArguments;
using tipitipapp.domain.Interfaces.Services;
using tipitipapp.ViewModels;

namespace tipitipapp.Views;

public partial class NFCCardReaderPopup : Popup
{
    private readonly NFCPopupViewModel _viewModel;
    private readonly INFCCardReaderService _nfcService;

    public NFCCardReaderPopup(decimal amount, string transactionType, INFCCardReaderService nfcService)
    {
        InitializeComponent();

        _nfcService = nfcService;
        _viewModel = new NFCPopupViewModel(amount, transactionType, _nfcService);

        BindingContext = _viewModel;

        // Subscribe to viewmodel events
        _viewModel.TransactionCompleted += OnTransactionCompleted;
        _viewModel.CloseRequested += OnCloseRequested;

        // Suscribirse al evento Closed para limpiar recursos
        this.Closed += NFCCardReaderPopup_Closed;
    }

    private void OnTransactionCompleted(object? sender, NFCTransactionEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // You can handle the transaction result here
            System.Diagnostics.Debug.WriteLine($"Transaction {e.TransactionId}: {e.Success}");
        });
    }

    private void OnCloseRequested(object? sender, bool success)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Close(success);
        });
    }

    private void NFCCardReaderPopup_Closed(object? sender,PopupClosedEventArgs e)
    {
        _viewModel.Dispose();
    }
}