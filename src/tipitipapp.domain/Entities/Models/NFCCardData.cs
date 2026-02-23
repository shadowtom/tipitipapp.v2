using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tipitipapp.domain.Entities.Models
{
    public class NFCCardData
    {
        public string CardUid { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public List<string> Technologies { get; set; } = new();
        public List<NdefRecordData> NdefRecords { get; set; } = new();
        public bool IsWritable { get; set; }
        public int MaxSize { get; set; }
        public DateTime ReadTime { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        // Transaction data
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;

        // Helper properties
        public string ShortUid => CardUid.Length > 8 ? CardUid[..8] : CardUid;
        public string FormattedAmount => $"${Amount:F2}";
        public string FormattedTime => ReadTime.ToString("HH:mm:ss");
        public string FormattedDate => ReadTime.ToString("yyyy-MM-dd");
    }
}
