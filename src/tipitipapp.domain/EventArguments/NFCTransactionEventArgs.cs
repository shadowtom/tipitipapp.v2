using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tipitipapp.domain.Entities.Models;

namespace tipitipapp.domain.EventArguments
{
    /// <summary>
    /// Event arguments for NFC transaction completion events
    /// </summary>
    public class NFCTransactionEventArgs : EventArgs
    {
        /// <summary>
        /// Indicates whether the transaction was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The amount processed in the transaction
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Unique transaction identifier
        /// </summary>
        public string? TransactionId { get; set; }

        /// <summary>
        /// Error message if transaction failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Card data read during the transaction
        /// </summary>
        public NFCCardData? CardData { get; set; }

        /// <summary>
        /// Timestamp when the transaction was completed
        /// </summary>
        public DateTime TransactionTime { get; set; }

        /// <summary>
        /// Type of transaction (e.g., "Quick Tip", "Manual Tip")
        /// </summary>
        public string? TransactionType { get; set; }

        /// <summary>
        /// Device information where transaction occurred
        /// </summary>
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Raw NFC data if available
        /// </summary>
        public byte[]? RawData { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public NFCTransactionEventArgs()
        {
            TransactionTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Constructor with basic transaction info
        /// </summary>
        public NFCTransactionEventArgs(bool success, decimal amount, string? transactionId = null, string? errorMessage = null)
        {
            Success = success;
            Amount = amount;
            TransactionId = transactionId;
            ErrorMessage = errorMessage;
            TransactionTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Constructor with full transaction details
        /// </summary>
        public NFCTransactionEventArgs(bool success, decimal amount, NFCCardData cardData, string transactionId, string transactionType)
        {
            Success = success;
            Amount = amount;
            CardData = cardData;
            TransactionId = transactionId;
            TransactionType = transactionType;
            TransactionTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns a formatted summary of the transaction
        /// </summary>
        public string GetSummary()
        {
            if (Success)
            {
                return $"✅ Transaction {TransactionId}: ${Amount:F2} - Card: {CardData?.ShortUid ?? "Unknown"}";
            }
            else
            {
                return $"❌ Transaction Failed: {ErrorMessage ?? "Unknown error"}";
            }
        }

        /// <summary>
        /// Returns detailed information about the transaction
        /// </summary>
        public string GetDetails()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"Transaction Details:");
            sb.AppendLine($"  Success: {Success}");
            sb.AppendLine($"  Amount: ${Amount:F2}");
            sb.AppendLine($"  Transaction ID: {TransactionId ?? "N/A"}");
            sb.AppendLine($"  Time: {TransactionTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  Type: {TransactionType ?? "N/A"}");

            if (Success && CardData != null)
            {
                sb.AppendLine($"  Card UID: {CardData.CardUid}");
                sb.AppendLine($"  Card Type: {CardData.CardType}");
                sb.AppendLine($"  Technologies: {string.Join(", ", CardData.Technologies)}");
                sb.AppendLine($"  Is Writable: {CardData.IsWritable}");

                if (CardData.NdefRecords.Any())
                {
                    sb.AppendLine($"  NDEF Records: {CardData.NdefRecords.Count}");
                    foreach (var record in CardData.NdefRecords)
                    {
                        if (!string.IsNullOrEmpty(record.Payload))
                        {
                            sb.AppendLine($"    - {record.Type}: {record.Payload}");
                        }
                    }
                }
            }
            else if (!Success && !string.IsNullOrEmpty(ErrorMessage))
            {
                sb.AppendLine($"  Error: {ErrorMessage}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Creates a successful transaction event
        /// </summary>
        public static NFCTransactionEventArgs CreateSuccess(decimal amount, NFCCardData cardData, string transactionId, string transactionType)
        {
            return new NFCTransactionEventArgs
            {
                Success = true,
                Amount = amount,
                CardData = cardData,
                TransactionId = transactionId,
                TransactionType = transactionType,
                TransactionTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a failed transaction event
        /// </summary>
        public static NFCTransactionEventArgs CreateFailure(decimal amount, string errorMessage, string? transactionType = null)
        {
            return new NFCTransactionEventArgs
            {
                Success = false,
                Amount = amount,
                ErrorMessage = errorMessage,
                TransactionType = transactionType,
                TransactionTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a timeout transaction event
        /// </summary>
        public static NFCTransactionEventArgs CreateTimeout(decimal amount, string transactionType)
        {
            return new NFCTransactionEventArgs
            {
                Success = false,
                Amount = amount,
                ErrorMessage = "Transaction timeout - No card detected",
                TransactionType = transactionType,
                TransactionTime = DateTime.UtcNow
            };
        }
    }
}
