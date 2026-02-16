using Android.Nfc;
using Plugin.NFC;
using System.Text;
using tipitipapp.Application.Interfaces;
using tipitipapp.domain.Entities.Enums;
using tipitipapp.domain.Entities.Models;
using tipitipapp.domain.EventArguments;
using tipitipapp.domain.Interfaces.Services;
using tipitipapp.Domain.Models;

namespace tipitipapp.Infrastructure.Services;

public class NFCCardReaderService : INFCCardReaderService, IDisposable
{
    private readonly SemaphoreSlim _nfcSemaphore = new(1, 1);
    private CancellationTokenSource? _cts;
    private bool _isListening;
    private bool _disposed;

    public NFCCardReaderService()
    {
        // Initialize NFC
        CrossNFC.Init();
        CrossNFC.Current.OnMessageReceived += OnMessageReceived;
        CrossNFC.Current.OnTagDiscovered += OnTagDiscovered;
        CrossNFC.Current.OnNfcStatusChanged += OnNfcStatusChanged;
        CrossNFC.Current.OnTagListeningStatusChanged += OnTagListeningStatusChanged;
    }

    // Events
    public event EventHandler<NFCDeviceDetectedEventArgs>? OnDeviceDetected;
    public event EventHandler<NFCErrorEventArgs>? OnError;
    public event EventHandler<NFCStatusChangedEventArgs>? OnStatusChanged;

    public async Task<bool> IsNfcAvailable()
    {
        return await Task.FromResult(CrossNFC.IsSupported && CrossNFC.Current.IsAvailable);
    }

    public async Task<bool> IsNfcEnabled()
    {
        return await Task.FromResult(CrossNFC.IsSupported && CrossNFC.Current.IsEnabled);
    }

    public async Task<bool> RequestNfcEnable()
    {
        try
        {
            if (!CrossNFC.IsSupported)
                return false;

            if (!CrossNFC.Current.IsAvailable)
                return false;

            if (!CrossNFC.Current.IsEnabled)
            {
                // Request user to enable NFC
                CrossNFC.Current.OpenNfcSettings();
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new NFCErrorEventArgs
            {
                ErrorMessage = $"Failed to enable NFC: {ex.Message}",
                Exception = ex
            });
            return false;
        }
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        await _nfcSemaphore.WaitAsync(cancellationToken);

        try
        {
            if (_isListening)
                return;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Check NFC availability
            if (!CrossNFC.IsSupported || !CrossNFC.Current.IsAvailable)
            {
                throw new InvalidOperationException("NFC is not available on this device");
            }

            // Check if NFC is enabled
            if (!CrossNFC.Current.IsEnabled)
            {
                OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs
                {
                    Status = "NFC is disabled. Please enable it in settings.",
                    IsListening = false
                });

                // Optionally request enable
                CrossNFC.Current.OpenNfcSettings();
                return;
            }

            // Start listening for NFC tags
            CrossNFC.Current.StartListening();
            _isListening = true;

            OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs
            {
                Status = "Listening for NFC cards...",
                IsListening = true
            });
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new NFCErrorEventArgs
            {
                ErrorMessage = $"Failed to start listening: {ex.Message}",
                Exception = ex
            });
        }
        finally
        {
            _nfcSemaphore.Release();
        }
    }

    public async Task StopListeningAsync()
    {
        await _nfcSemaphore.WaitAsync();

        try
        {
            if (!_isListening)
                return;

            if (CrossNFC.IsSupported)
            {
                CrossNFC.Current.StopListening();
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _isListening = false;

            OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs
            {
                Status = "Stopped listening",
                IsListening = false
            });
        }
        finally
        {
            _nfcSemaphore.Release();
        }
    }

    private async void OnMessageReceived(ITagInfo tagInfo)
    {
        try
        {
            var cardData = await ParseNfcTagInfo(tagInfo);
            cardData.Success = true;

            OnDeviceDetected?.Invoke(this, new NFCDeviceDetectedEventArgs
            {
                CardData = cardData
            });

            // Auto-stop after successful read (optional)
            await StopListeningAsync();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new NFCErrorEventArgs
            {
                ErrorMessage = $"Error processing NFC tag: {ex.Message}",
                Exception = ex
            });
        }
    }

    private void OnTagDiscovered(ITagInfo tagInfo, bool formatReadable)
    {
        // Sometimes tags are discovered before they're fully read
        OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs
        {
            Status = "NFC tag detected, reading...",
            IsListening = true
        });
    }

    private void OnNfcStatusChanged(bool isEnabled)
    {
        OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs
        {
            Status = isEnabled ? "NFC is enabled" : "NFC is disabled",
            IsListening = _isListening && isEnabled
        });

        if (isEnabled && _isListening)
        {
            // Restart listening if it was enabled while listening
            CrossNFC.Current.StartListening();
        }
    }

    private void OnTagListeningStatusChanged(bool isListening)
    {
        _isListening = isListening;

        OnStatusChanged?.Invoke(this, new NFCStatusChangedEventArgs
        {
            Status = isListening ? "Listening..." : "Not listening",
            IsListening = isListening
        });
    }

    private async Task<NFCCardData> ParseNfcTagInfo(ITagInfo tagInfo)
    {
        var cardData = new NFCCardData
        {
            ReadTime = DateTime.Now
        };

        try
        {
            // Extract card UID (unique identifier)
            if (tagInfo.Identifier != null && tagInfo.Identifier.Length > 0)
            {
                cardData.CardUid = BitConverter.ToString(tagInfo.Identifier).Replace("-", "");
            }

            // Extract technology types
            if (tagInfo.Technologies != null)
            {
                cardData.Technologies.AddRange(tagInfo.Technologies);

                // Determine card type based on technologies
                cardData.CardType = DetermineCardType(tagInfo.Technologies);
            }

            // Check if writable
            cardData.IsWritable = tagInfo.IsWritable;
            cardData.MaxSize = tagInfo.MaxSize;

            // Extract NDEF records
            if (tagInfo.Records != null && tagInfo.Records.Any())
            {
                foreach (var record in tagInfo.Records)
                {
                    var ndefRecord = await ParseNdefRecord(record);
                    cardData.NdefRecords.Add(ndefRecord);
                }
            }

            // If no NDEF records, try to read raw data
            if (!cardData.NdefRecords.Any() && tagInfo.Identifier != null)
            {
                // Add raw identifier as a record
                cardData.NdefRecords.Add(new NdefRecordData
                {
                    Type = "UID",
                    Payload = cardData.CardUid,
                    RawPayload = tagInfo.Identifier,
                    RecordType = RecordType.Unknown
                });
            }
        }
        catch (Exception ex)
        {
            cardData.ErrorMessage = $"Parse error: {ex.Message}";
            cardData.Success = false;
        }

        return cardData;
    }

    private async Task<NdefRecordData> ParseNdefRecord(INdefRecord record)
    {
        var recordData = new NdefRecordData();

        try
        {
            // Get record type
            recordData.Type = record.TypeFormat?.ToString() ?? "Unknown";

            // Get raw payload
            recordData.RawPayload = record.Payload ?? Array.Empty<byte>();

            // Parse based on record type
            switch (record.TypeFormat)
            {
                case NdefTypeFormat.WellKnown:
                    recordData = ParseWellKnownRecord(record);
                    break;

                case NdefTypeFormat.Media:
                    recordData.Payload = Encoding.UTF8.GetString(record.Payload ?? Array.Empty<byte>());
                    recordData.RecordType = RecordType.Unknown;
                    break;

                case NdefTypeFormat.AbsoluteUri:
                    recordData.Payload = Encoding.UTF8.GetString(record.Payload ?? Array.Empty<byte>());
                    recordData.RecordType = RecordType.Uri;
                    break;

                default:
                    if (record.Payload != null)
                    {
                        recordData.Payload = BitConverter.ToString(record.Payload).Replace("-", " ");
                    }
                    recordData.RecordType = RecordType.Unknown;
                    break;
            }
        }
        catch (Exception ex)
        {
            recordData.Payload = $"Error parsing: {ex.Message}";
        }

        return await Task.FromResult(recordData);
    }

    private NdefRecordData ParseWellKnownRecord(INdefRecord record)
    {
        var result = new NdefRecordData();

        try
        {
            var payload = record.Payload ?? Array.Empty<byte>();

            if (payload.Length > 0)
            {
                // Check for text record (RTD_TEXT)
                if (record.RtdType != null && record.RtdType.SequenceEqual(NdefRtdTypes.Text))
                {
                    // Parse text record
                    // First byte: status byte (bit 7 indicates UTF-16, bits 6-0 language code length)
                    if (payload.Length > 0)
                    {
                        byte status = payload[0];
                        bool isUtf16 = (status & 0x80) != 0;
                        int languageCodeLength = status & 0x3F;

                        if (payload.Length > 1 + languageCodeLength)
                        {
                            // Extract language code
                            result.LanguageCode = Encoding.ASCII.GetString(payload, 1, languageCodeLength);

                            // Extract text
                            int textStart = 1 + languageCodeLength;
                            int textLength = payload.Length - textStart;

                            result.Payload = isUtf16
                                ? Encoding.BigEndianUnicode.GetString(payload, textStart, textLength)
                                : Encoding.UTF8.GetString(payload, textStart, textLength);

                            result.RecordType = RecordType.Text;
                        }
                    }
                }
                // Check for URI record (RTD_URI)
                else if (record.RtdType != null && record.RtdType.SequenceEqual(NdefRtdTypes.Uri))
                {
                    if (payload.Length > 0)
                    {
                        // First byte is URI identifier code
                        byte uriCode = payload[0];
                        string prefix = GetUriPrefix(uriCode);

                        if (payload.Length > 1)
                        {
                            string uriSuffix = Encoding.UTF8.GetString(payload, 1, payload.Length - 1);
                            result.Payload = prefix + uriSuffix;
                            result.RecordType = RecordType.Uri;
                        }
                    }
                }
                else
                {
                    // Unknown well-known type, try to decode as text
                    result.Payload = Encoding.UTF8.GetString(payload);
                    result.RecordType = RecordType.Unknown;
                }
            }
        }
        catch (Exception ex)
        {
            result.Payload = $"Parse error: {ex.Message}";
        }

        return result;
    }

    private string GetUriPrefix(byte code)
    {
        return code switch
        {
            0x00 => "",
            0x01 => "http://www.",
            0x02 => "https://www.",
            0x03 => "http://",
            0x04 => "https://",
            0x05 => "tel:",
            0x06 => "mailto:",
            0x07 => "ftp://anonymous:anonymous@",
            0x08 => "ftp://ftp.",
            0x09 => "ftps://",
            0x0A => "sftp://",
            0x0B => "smb://",
            0x0C => "nfs://",
            0x0D => "ftp://",
            0x0E => "dav://",
            0x0F => "news:",
            0x10 => "telnet://",
            0x11 => "imap:",
            0x12 => "rtsp://",
            0x13 => "urn:",
            0x14 => "pop:",
            0x15 => "sip:",
            0x16 => "sips:",
            0x17 => "tftp:",
            0x18 => "btspp://",
            0x19 => "btl2cap://",
            0x1A => "btgoep://",
            0x1B => "tcpobex://",
            0x1C => "irdaobex://",
            0x1D => "file://",
            0x1E => "urn:epc:id:",
            0x1F => "urn:epc:tag:",
            0x20 => "urn:epc:pat:",
            0x21 => "urn:epc:raw:",
            0x22 => "urn:epc:",
            0x23 => "urn:nfc:",
            _ => ""
        };
    }

    private string DetermineCardType(IEnumerable<string> technologies)
    {
        var techList = technologies.ToList();

        if (techList.Contains("android.nfc.tech.IsoDep"))
            return "ISO-DEP (Desfire, ISO 14443-4)";
        if (techList.Contains("android.nfc.tech.MifareClassic"))
            return "MIFARE Classic";
        if (techList.Contains("android.nfc.tech.MifareUltralight"))
            return "MIFARE Ultralight";
        if (techList.Contains("android.nfc.tech.NfcA"))
            return "NFC-A (ISO 14443-3A)";
        if (techList.Contains("android.nfc.tech.NfcB"))
            return "NFC-B (ISO 14443-3B)";
        if (techList.Contains("android.nfc.tech.NfcF"))
            return "NFC-F (JIS 6319-4)";
        if (techList.Contains("android.nfc.tech.NfcV"))
            return "NFC-V (ISO 15693)";
        if (techList.Contains("android.nfc.tech.Ndef"))
            return "NDEF Compatible";

        return "Unknown";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (CrossNFC.IsSupported)
            {
                CrossNFC.Current.OnMessageReceived -= OnMessageReceived;
                CrossNFC.Current.OnTagDiscovered -= OnTagDiscovered;
                CrossNFC.Current.OnNfcStatusChanged -= OnNfcStatusChanged;
                CrossNFC.Current.OnTagListeningStatusChanged -= OnTagListeningStatusChanged;

                if (_isListening)
                {
                    CrossNFC.Current.StopListening();
                }
            }

            _nfcSemaphore?.Dispose();
            _disposed = true;
        }
    }
}