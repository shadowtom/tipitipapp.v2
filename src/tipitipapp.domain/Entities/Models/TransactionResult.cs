using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tipitipapp.domain.EventArguments;

namespace tipitipapp.domain.Entities.Models
{
    /// <summary>
    /// Represents the result of a transaction
    /// </summary>
    public class TransactionResult
    {
        /// <summary>
        /// Whether the transaction was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Transaction amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Unique transaction ID
        /// </summary>
        public string? TransactionId { get; set; }

        /// <summary>
        /// Error message if transaction failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Card data from the transaction
        /// </summary>
        public NFCCardData? CardData { get; set; }

        /// <summary>
        /// Transaction timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Transaction type
        /// </summary>
        public string? TransactionType { get; set; }

        /// <summary>
        /// Creates a successful transaction result
        /// </summary>
        public static TransactionResult Ok(decimal amount, NFCCardData cardData, string transactionId, string transactionType)
        {
            return new TransactionResult
            {
                Success = true,
                Amount = amount,
                CardData = cardData,
                TransactionId = transactionId,
                TransactionType = transactionType,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a failed transaction result
        /// </summary>
        public static TransactionResult Fail(decimal amount, string errorMessage, string? transactionType = null)
        {
            return new TransactionResult
            {
                Success = false,
                Amount = amount,
                ErrorMessage = errorMessage,
                TransactionType = transactionType,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Converts to event args
        /// </summary>
        public NFCTransactionEventArgs ToEventArgs()
        {
            return new NFCTransactionEventArgs
            {
                Success = Success,
                Amount = Amount,
                TransactionId = TransactionId,
                ErrorMessage = ErrorMessage,
                CardData = CardData,
                TransactionType = TransactionType,
                TransactionTime = Timestamp
            };
        }
    }
}
