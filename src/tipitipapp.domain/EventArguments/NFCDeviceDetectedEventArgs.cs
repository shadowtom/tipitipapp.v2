using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tipitipapp.domain.Entities.Models;

namespace tipitipapp.domain.EventArguments
{
    public class NFCDeviceDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// Card data read from the NFC device
        /// </summary>
        public NFCCardData CardData { get; set; } = new();

        /// <summary>
        /// Indicates whether the detection was successful
        /// </summary>
        public bool Success => CardData.Success;

        /// <summary>
        /// Signal strength of the NFC connection
        /// </summary>
        public int SignalStrength { get; set; }

        /// <summary>
        /// Time taken to detect the card in milliseconds
        /// </summary>
        public int DetectionTimeMs { get; set; }
    }
}
