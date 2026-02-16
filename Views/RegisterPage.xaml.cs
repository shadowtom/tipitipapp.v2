using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tipitipapp.ViewModels;

namespace tipitipapp.Views
{
    public partial class RegisterPage : ContentPage
    {
        private readonly RegisterViewModel _viewModel;

        public RegisterPage(RegisterViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        private void OnEmailCompleted(object sender, EventArgs e)
        {
            FirstNameEntry.Focus();
        }

        private void OnFirstNameCompleted(object sender, EventArgs e)
        {
            LastNameEntry.Focus();
        }

        private void OnLastNameCompleted(object sender, EventArgs e)
        {
            PhoneNumberEntry?.Focus();
        }

        private void OnPhoneCompleted(object sender, EventArgs e)
        {
            PasswordEntry.Focus();
        }

        private void OnPasswordCompleted(object sender, EventArgs e)
        {
            ConfirmPasswordEntry.Focus();
        }

        private void OnConfirmPasswordCompleted(object sender, EventArgs e)
        {
            _viewModel.RegisterCommand.Execute(null);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Clear form when page appears
            EmailEntry.Text = string.Empty;
            FirstNameEntry.Text = string.Empty;
            LastNameEntry.Text = string.Empty;
            PhoneNumberEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;
        }
    }
}
