using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tipitipapp.domain.Entities.Enums;

namespace tipitipapp.domain.EventArguments
{
    public class TransactionStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Current status message
        /// </summary>
        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Status type (Info, Warning, Error, Success)
        /// </summary>
        public TransactionStatusType StatusType { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// Additional data
        /// </summary>
        public object? Data { get; set; }
    }
}
