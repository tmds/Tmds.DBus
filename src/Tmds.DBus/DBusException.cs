// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace DBus
{
	class BusException : Exception
	{
		public BusException (string errorName, string errorMessage)
		{
			this.ErrorName = errorName;
			this.ErrorMessage = errorMessage;
		}

		public BusException (string errorName, string format, params object[] args)
		{
			this.ErrorName = errorName;
			this.ErrorMessage = String.Format (format, args);
		}

		public override string Message
		{
			get
			{
				return ErrorName + ": " + ErrorMessage;
			}
		}

		public readonly string ErrorName;

		public readonly string ErrorMessage;
	}
}
