// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;

namespace DBus.Protocol
{
	static class ProtocolInformation
	{
		//protocol versions that we support
		public const byte MinVersion = 0;
		public const byte Version = 1;
		public const byte MaxVersion = Version + 1;

		public const uint MaxMessageLength = 134217728; //2 to the 27th power
		public const uint MaxArrayLength = 67108864; //2 to the 26th power
		public const uint MaxSignatureLength = 255;
		public const uint MaxArrayDepth = 32;
		public const uint MaxStructDepth = 32;

		//this is not strictly related to Protocol since names are passed around as strings
		internal const uint MaxNameLength = 255;
		internal const uint MaxMatchRuleLength = 1024;
		internal const uint MaxMatchRuleArgs = 64;

		public static int PadNeeded (int pos, int alignment)
		{
			int pad = pos % alignment;
			return pad == 0 ? 0 : alignment - pad;
		}

		public static int Padded (int pos, int alignment)
		{
			int pad = pos % alignment;
			if (pad != 0)
				pos += alignment - pad;

			return pos;
		}

		public static int GetAlignment (DType dtype)
		{
			switch (dtype) {
				case DType.Byte:
					return 1;
				case DType.Boolean:
					return 4;
				case DType.Int16:
				case DType.UInt16:
					return 2;
				case DType.Int32:
				case DType.UInt32:
					return 4;
				case DType.Int64:
				case DType.UInt64:
					return 8;
#if !DISABLE_SINGLE
				case DType.Single: //Not yet supported!
					return 4;
#endif
				case DType.Double:
					return 8;
				case DType.String:
					return 4;
				case DType.ObjectPath:
					return 4;
				case DType.Signature:
					return 1;
				case DType.Array:
					return 4;
				case DType.StructBegin:
					return 8;
				case DType.Variant:
					return 1;
				case DType.DictEntryBegin:
					return 8;
				case DType.Invalid:
				default:
					throw new Exception ("Cannot determine alignment of " + dtype);
			}
		}

		//this class may not be the best place for Verbose
		public readonly static bool Verbose = !String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("DBUS_VERBOSE"));
	}

#if UNDOCUMENTED_IN_SPEC
/*
"org.freedesktop.DBus.Error.Failed"
"org.freedesktop.DBus.Error.NoMemory"
"org.freedesktop.DBus.Error.ServiceUnknown"
"org.freedesktop.DBus.Error.NameHasNoOwner"
"org.freedesktop.DBus.Error.NoReply"
"org.freedesktop.DBus.Error.IOError"
"org.freedesktop.DBus.Error.BadAddress"
"org.freedesktop.DBus.Error.NotSupported"
"org.freedesktop.DBus.Error.LimitsExceeded"
"org.freedesktop.DBus.Error.AccessDenied"
"org.freedesktop.DBus.Error.AuthFailed"
"org.freedesktop.DBus.Error.NoServer"
"org.freedesktop.DBus.Error.Timeout"
"org.freedesktop.DBus.Error.NoNetwork"
"org.freedesktop.DBus.Error.AddressInUse"
"org.freedesktop.DBus.Error.Disconnected"
"org.freedesktop.DBus.Error.InvalidArgs"
"org.freedesktop.DBus.Error.FileNotFound"
"org.freedesktop.DBus.Error.UnknownMethod"
"org.freedesktop.DBus.Error.TimedOut"
"org.freedesktop.DBus.Error.MatchRuleNotFound"
"org.freedesktop.DBus.Error.MatchRuleInvalid"
"org.freedesktop.DBus.Error.Spawn.ExecFailed"
"org.freedesktop.DBus.Error.Spawn.ForkFailed"
"org.freedesktop.DBus.Error.Spawn.ChildExited"
"org.freedesktop.DBus.Error.Spawn.ChildSignaled"
"org.freedesktop.DBus.Error.Spawn.Failed"
"org.freedesktop.DBus.Error.UnixProcessIdUnknown"
"org.freedesktop.DBus.Error.InvalidSignature"
"org.freedesktop.DBus.Error.SELinuxSecurityContextUnknown"
*/
#endif
}
