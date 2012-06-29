using System;
using System.Collections.Generic;

namespace DBus.Protocol
{
	/* We use these structs when we receive a struct from D-Bus without
	 * knowing which .NET type they are supposed to map to
	 * The case arises basically when a struct is inside a variant object
	 */
	public static class DBusStruct
	{
		public static Type FromInnerTypes (Type[] innerTypes)
		{
			// We only support up to 7 inner types
			if (innerTypes == null || innerTypes.Length == 0 || innerTypes.Length > 7)
				throw new NotSupportedException ("Can't create a valid type for the provided signature");

			Type structType = null;
			switch (innerTypes.Length) {
			case 1:
				structType = typeof (DBusStruct<>);
				break;
			case 2:
				structType = typeof (DBusStruct<,>);
				break;
			case 3:
				structType = typeof (DBusStruct<,,>);
				break;
			case 4:
				structType = typeof (DBusStruct<,,,>);
				break;
			case 5:
				structType = typeof (DBusStruct<,,,,>);
				break;
			case 6:
				structType = typeof (DBusStruct<,,,,,>);
				break;
			case 7:
				structType = typeof (DBusStruct<,,,,,,>);
				break;
			}
			return structType.MakeGenericType (innerTypes);
		}
	}

	public struct DBusStruct<T1>
	{
		public T1 Item1;
	}

	public struct DBusStruct<T1, T2>
	{
		public T1 Item1;
		public T2 Item2;
	}

	public struct DBusStruct<T1, T2, T3>
	{
		public T1 Item1;
		public T2 Item2;
		public T3 Item3;
	}

	public struct DBusStruct<T1, T2, T3, T4>
	{
		public T1 Item1;
		public T2 Item2;
		public T3 Item3;
		public T4 Item4;
	}

	public struct DBusStruct<T1, T2, T3, T4, T5>
	{
		public T1 Item1;
		public T2 Item2;
		public T3 Item3;
		public T4 Item4;
		public T5 Item5;
	}

	public struct DBusStruct<T1, T2, T3, T4, T5, T6>
	{
		public T1 Item1;
		public T2 Item2;
		public T3 Item3;
		public T4 Item4;
		public T5 Item5;
		public T6 Item6;
	}

	public struct DBusStruct<T1, T2, T3, T4, T5, T6, T7>
	{
		public T1 Item1;
		public T2 Item2;
		public T3 Item3;
		public T4 Item4;
		public T5 Item5;
		public T6 Item6;
		public T7 Item7;
	}
}

