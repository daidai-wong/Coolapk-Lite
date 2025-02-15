// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Microsoft.Toolkit.Uwp.Connectivity
{
    /// <summary>
    /// Helper function when working with <see cref="GattProtocolError" />
    /// </summary>
    public static class GattProtocolErrorParser
    {
        /// <summary>
        /// Helper to convert an Gatt error value into a string
        /// </summary>
        /// <param name="errorValue"> the byte error value.</param>
        /// <returns>String representation of the error</returns>
        public static string GetErrorString(this byte? errorValue)
        {
            string errorString = "Protocol Error";

            if (errorValue.HasValue == false)
            {
                return errorString;
            }

            if (errorValue == GattProtocolError.AttributeNotFound)
            {
                return "Attribute Not Found";
            }

            if (errorValue == GattProtocolError.AttributeNotLong)
            {
                return "Attribute Not Long";
            }

            if (errorValue == GattProtocolError.InsufficientAuthentication)
            {
                return "Insufficient Authentication";
            }

            if (errorValue == GattProtocolError.InsufficientAuthorization)
            {
                return "Insufficient Authorization";
            }

            if (errorValue == GattProtocolError.InsufficientEncryption)
            {
                return "Insufficient Encryption";
            }

            if (errorValue == GattProtocolError.InsufficientEncryptionKeySize)
            {
                return "Insufficient Encryption Key Size";
            }

            if (errorValue == GattProtocolError.InsufficientResources)
            {
                return "Insufficient Resources";
            }

            if (errorValue == GattProtocolError.InvalidAttributeValueLength)
            {
                return "Invalid Attribute Value Length";
            }

            if (errorValue == GattProtocolError.InvalidHandle)
            {
                return "Invalid Handle";
            }

            return errorValue == GattProtocolError.InvalidOffset
                ? "Invalid Offset"
                : errorValue == GattProtocolError.InvalidPdu
                ? "Invalid Pdu"
                : errorValue == GattProtocolError.PrepareQueueFull
                ? "Prepare Queue Full"
                : errorValue == GattProtocolError.ReadNotPermitted
                ? "Read Not Permitted"
                : errorValue == GattProtocolError.RequestNotSupported
                ? "Request Not Supported"
                : errorValue == GattProtocolError.UnlikelyError
                ? "UnlikelyError"
                : errorValue == GattProtocolError.UnsupportedGroupType
                ? "Unsupported Group Type"
                : errorValue == GattProtocolError.WriteNotPermitted ? "Write Not Permitted" : errorString;
        }
    }
}