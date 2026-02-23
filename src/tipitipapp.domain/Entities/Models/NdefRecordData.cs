using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tipitipapp.domain.Entities.Enums;

namespace tipitipapp.domain.Entities.Models
{
    public class NdefRecordData
    {
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public byte[] RawPayload { get; set; } = Array.Empty<byte>();
        public string LanguageCode { get; set; } = string.Empty;
        public RecordType RecordType { get; set; }
    }
}
