// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace DBus
{
	public class ILReader2
	{
		public byte[] m_byteArray;
		Int32 m_position;
		MethodBase m_enclosingMethod;

		static OpCode[] s_OneByteOpCodes = new OpCode[0x100];
		static OpCode[] s_TwoByteOpCodes = new OpCode[0x100];
		static ILReader2()
		{
			foreach (FieldInfo fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				OpCode opCode = (OpCode)fi.GetValue(null);
				UInt16 value = (UInt16)opCode.Value;
				if (value < 0x100)
					s_OneByteOpCodes[value] = opCode;
				else if ((value & 0xff00) == 0xfe00)
					s_TwoByteOpCodes[value & 0xff] = opCode;
			}
		}

		/*
			 public ILReader(MethodBase enclosingMethod)
			 {
			 this.m_enclosingMethod = enclosingMethod;
			 MethodBody methodBody = m_enclosingMethod.GetMethodBody();
			 this.m_byteArray = (methodBody == null) ? new Byte[0] : methodBody.GetILAsByteArray();
			 this.m_position = 0;
			 }
			 */

		public ILReader2(MethodBase enclosingMethod)
		{
			this.m_enclosingMethod = enclosingMethod;
			MethodBody methodBody = m_enclosingMethod.GetMethodBody();
			this.m_byteArray = (methodBody == null) ? new Byte[0] : methodBody.GetILAsByteArray();
			this.m_position = 0;
		}

		public ILOp[] Iterate()
		{
			List<ILOp> data = new List<ILOp>();

			while (m_position < m_byteArray.Length) {
				Next();

				if (current.operandType == null)
					current.operandType = String.Empty;

				if (current.operand == null)
					current.operand = String.Empty;

				data.Add(current);
			}

			//Console.WriteLine("dataCount: " + data.Count);
			m_position = 0;

			return data.ToArray();
		}

		public struct ILOp
		{
			/*
			public ILOp(OpCodeType opType, string operandType)
			{
				this.opType = opType;
				this.operandType = operandType;
				this.operand = String.Empty;
			}
			*/

			public OpCode opCode;

			//public OpCodeType opType;
			public string operandType;
			public object operand;

			/*
			public string operandType
			{
				get {
					//return String.Empty;
					return String.Empty;
				} set {
				}
			}
			*/
			public Type operandTypeT
			{
				get {
					//return String.Empty;
					if (operandType == String.Empty)
						return null;
					return Type.GetType("System" + Type.Delimiter + operandType);
				}
			}

			/*
			public string operand
			{
				get {
					return String.Empty;
				} set {
				}
			}
			*/


			/*
			public OpCode _opCode
			{
				get {
					//UInt16 value = (UInt16)opCode.Value;
					UInt16 value = (UInt16)opCodeValue;
					if (value < 0x100)
						s_OneByteOpCodes[value] = opCode;
					else if ((value & 0xff00) == 0xfe00)
						s_TwoByteOpCodes[value & 0xff] = opCode;
				}
			}
			*/

			static Type ilgType = typeof(ILGenerator);

			public bool Emit(ILGenerator ilg)
			{
				Type t = operandTypeT;

				if (t == null) {
						//Console.WriteLine("emitsimple: " + opCode);
						if (ilg != null)
							ilg.Emit(opCode);
						return true;
				}

				MethodInfo emitmi = ilgType.GetMethod("Emit", new Type[] {typeof(OpCode), t});
				//Console.WriteLine("emitmi: " + opCode + " " + emitmi);

				object operandObj = null;

				//if (t == typeof(MethodInfo)) {
				if (typeof(MemberInfo).IsAssignableFrom(t)) {
					string operandStr = (string)operand;
					int idx = operandStr.LastIndexOf(Type.Delimiter);
					if (idx < 0)
						return false;
					string ifaceName = operandStr.Remove(idx);
					string methName = operandStr.Substring(idx + 1);

					//Console.WriteLine("ifacename: " + ifaceName);
					//Console.WriteLine("methname: " + methName);

					Type declType = Type.GetType(ifaceName);
					if (declType == null)
						declType = Assembly.GetExecutingAssembly().GetType(ifaceName, false);
					if (declType == null)
						declType = Assembly.GetCallingAssembly().GetType(ifaceName, false);

					if (declType == null)
						return false;

					//MethodInfo mi = declType.GetMethod(methName);
					//if (mi == null)
					//	return false;
					MemberInfo[] mis = declType.GetMember(methName);
					if (mis.Length == 0)
						return false;

					MemberInfo mi = mis[0];

					//Console.WriteLine("good!: " + mi);

					operandObj = mi;
				} else if (t == typeof(string))
					operandObj = (string)operand;
				else
					operandObj = Convert.ChangeType(operand, t);

				//Console.WriteLine("operandObj: " + operandObj);
				if (ilg != null)
					emitmi.Invoke(ilg, new object[] { opCode, operandObj });

				/*
				switch (operandType) {
					case nu:
						ilg.Emit(opCode);
					return true;
					case "MethodInfo":
						MethodInfo mi = null;
					ilg.Emit(opCode, mi);
					return true;
				}
				*/

				return true;
			}
		}

		public static bool TryGetType(string ifaceName, out Type declType)
		{
			declType = Type.GetType(ifaceName);
			if (declType == null)
				declType = Assembly.GetExecutingAssembly().GetType(ifaceName, false);
			if (declType == null)
				declType = Assembly.GetCallingAssembly().GetType(ifaceName, false);
			return declType != null;
		}


		ILOp current;

		void Next()
		{
			Int32 offset = m_position;
			OpCode opCode = OpCodes.Nop;
			int token = 0;

			// read first 1 or 2 bytes as opCode
			Byte code = ReadByte();
			if (code != 0xFE)
				opCode = s_OneByteOpCodes[code];
			else
			{
				code = ReadByte();
				opCode = s_TwoByteOpCodes[code];
			}

			//Console.WriteLine("opcode: " + opCode);

			current = new ILOp();
			//current.opType = opCode.OpCodeType;
			current.opCode = opCode;
			//current.opType = opCode.OperandType;

			switch (opCode.OperandType)
			{
				case OperandType.InlineNone:
					//ilg.Emit(opCode);
					break;
				case OperandType.ShortInlineBrTarget:
					SByte shortDelta = ReadSByte();
					//ilg.Emit(opCode, shortDelta);
					break;
				case OperandType.InlineBrTarget:
					Int32 delta = ReadInt32();
					//ilg.Emit(opCode, delta);
					break;
				case OperandType.ShortInlineI:
					byte int8 = ReadByte();
					//ilg.Emit(opCode, int8);
					break;
				case OperandType.InlineI:
					Int32 int32 = ReadInt32();
					//ilg.Emit(opCode, int32);
					break;
				case OperandType.InlineI8:
					Int64 int64 = ReadInt64();
					//ilg.Emit(opCode, int64);
					break;
				case OperandType.ShortInlineR:
					Single float32 = ReadSingle();
					//ilg.Emit(opCode, float32);
					break;
				case OperandType.InlineR:
					Double float64 = ReadDouble();
					//ilg.Emit(opCode, float64);
					break;
				case OperandType.ShortInlineVar:
					Byte index8 = ReadByte();
					//ilg.Emit(opCode, index8);
					break;
				case OperandType.InlineVar:
					UInt16 index16 = ReadUInt16();
					//ilg.Emit(opCode, index16);
					break;
				case OperandType.InlineString:
					token = ReadInt32();
					current.operandType = "String";
					current.operand = m_enclosingMethod.Module.ResolveString(token);
					//ilg.Emit(opCode, m_enclosingMethod.Module.ResolveString(token));
					break;
				case OperandType.InlineSig:
					token = ReadInt32();
					//ilg.Emit(opCode, m_enclosingMethod.Module.ResolveSignature(token));
					throw new NotImplementedException();
					break;
				case OperandType.InlineField:
					token = ReadInt32();
					FieldInfo fi = m_enclosingMethod.Module.ResolveField(token);
					current.operandType = "Reflection.FieldInfo";
					current.operand = fi.DeclaringType.FullName + Type.Delimiter + fi.Name;
					//ilg.Emit(opCode, m_enclosingMethod.Module.ResolveField(token));
					break;
				case OperandType.InlineType:
					token = ReadInt32();
					current.operandType = "Type";
					Type t = m_enclosingMethod.Module.ResolveType(token);
					current.operand = t.FullName;
					//ilg.Emit(opCode, m_enclosingMethod.Module.ResolveType(token));
					break;
				case OperandType.InlineTok:
					token = ReadInt32();
					//ilg.Emit(opCode, token);
					break;
				case OperandType.InlineMethod:
					token = ReadInt32();
					MethodInfo mi = (MethodInfo)m_enclosingMethod.Module.ResolveMethod(token);
					current.operandType = "Reflection.MethodInfo";
					current.operand = mi.DeclaringType.FullName + Type.Delimiter + mi.Name;
					//ilg.Emit(opCode, mi);
					break;
				case OperandType.InlineSwitch:

					throw new NotImplementedException();

					Int32 cases = ReadInt32();
					Int32[] deltas = new Int32[cases];
					for (Int32 i = 0; i < cases; i++) deltas[i] = ReadInt32();
					break;

				default:
					throw new BadImageFormatException("unexpected OperandType " + opCode.OperandType);
			}
		}

		Byte ReadByte()
		{
			current.operandType = "Byte";
			Byte ret = (Byte)m_byteArray[m_position++];
			current.operand = ret;
			return ret;
		}

		SByte ReadSByte()
		{
			current.operandType = "SByte";
			SByte ret = (SByte)ReadByte();
			current.operand = ret;
			return ret;
		}

		UInt16 ReadUInt16() {
			current.operandType = "UInt16";
			m_position += 2;
			UInt16 ret = BitConverter.ToUInt16(m_byteArray, m_position - 2);
			current.operand = ret;
			return ret;
		}

		UInt32 ReadUInt32() {
			current.operandType = "UInt32";
			m_position += 4;
			UInt32 ret = BitConverter.ToUInt32(m_byteArray, m_position - 4);
			current.operand = ret;
			return ret;
		}

		UInt64 ReadUInt64() {
			current.operandType = "UInt64";
			m_position += 8;
			UInt64 ret = BitConverter.ToUInt64(m_byteArray, m_position - 8);
			current.operand = ret;
			return ret;
		}

		Int32 ReadInt32() {
			current.operandType = "Int32";
			m_position += 4;
			Int32 ret = BitConverter.ToInt32(m_byteArray, m_position - 4);
			current.operand = ret;
			return ret;
		}

		Int64 ReadInt64() {
			current.operandType = "Int64";
			m_position += 8;
			Int64 ret = BitConverter.ToInt64(m_byteArray, m_position - 8);
			current.operand = ret;
			return ret;
		}

		Single ReadSingle() {
			current.operandType = "UInt16";
			m_position += 4;
			Single ret = BitConverter.ToSingle(m_byteArray, m_position - 4);
			current.operand = ret;
			return ret;
		}

		Double ReadDouble() {
			current.operandType = "Double";
			m_position += 8;
			Double ret = BitConverter.ToDouble(m_byteArray, m_position - 8);
			current.operand = ret;
			return ret;
		}
	}

#if RENTAL_STUFF_FROM_DAEMON

		/*
		public string MyMeth (uint val1, string val2)
		{
			Console.WriteLine ("MyMeth " + val1 + " " + val2);
			return "WEE!";
		}
		*/

		/*
		public struct MethData
		{
			public int A;
			public int B;
			public int C;
		}
		*/

		public class MethDataBase
		{
			public int A;
		}

		public class MethData : MethDataBase
		{
			public int B;
			public int C;
			//public MethData[] Children;
			public long[] Children;
		}

		public void MyMeth0 ()
		{
		}

		public string MyMeth (MethData d, int[] ary, IDictionary<int, string> dict)
		{
			Console.WriteLine ("MyMeth struct " + d.A + " " + d.B + " " + d.C);
			foreach (int i in ary)
				Console.WriteLine (i);
			Console.WriteLine ("Children: " + d.Children.Length);
			Console.WriteLine ("Dict entries: " + dict.Count);
			Console.WriteLine ("321: " + dict[321]);
			return "WEE!";
		}

	// From HandleMessage():
				//if (msg.Signature == new Signature ("u"))
			if (false) {
				System.Reflection.MethodInfo target = typeof (ServerBus).GetMethod ("MyMeth");
				//System.Reflection.MethodInfo target = typeof (ServerBus).GetMethod ("MyMeth0");
				Signature inSig, outSig;
				TypeImplementer.SigsForMethod (target, out inSig, out outSig);
				Console.WriteLine ("inSig: " + inSig);

				if (msg.Signature == inSig) {
					MethodCaller caller = TypeImplementer.GenCaller (target, this);
					//caller (new MessageReader (msg), msg);

					MessageWriter retWriter = new MessageWriter ();
					caller (new MessageReader (msg), msg, retWriter);

					if (msg.ReplyExpected) {
						MethodReturn method_return = new MethodReturn (msg.Header.Serial);
						Message replyMsg = method_return.message;
						replyMsg.Body = retWriter.ToArray ();
						Console.WriteLine ("replyMsg body: " + replyMsg.Body.Length);
						/*
						try {
						replyMsg.Header[FieldCode.Destination] = msg.Header[FieldCode.Sender];
						replyMsg.Header[FieldCode.Sender] = Caller.UniqueName;
						} catch (Exception e) {
							Console.Error.WriteLine (e);
						}
						*/
						replyMsg.Header[FieldCode.Destination] = Caller.UniqueName;
						replyMsg.Header[FieldCode.Sender] = "org.freedesktop.DBus";
						replyMsg.Signature = outSig;
						{
							Caller.Send (replyMsg);

							/*
							replyMsg.Header.Serial = Caller.GenerateSerial ();
							MessageDumper.WriteMessage (replyMsg, Console.Out);
							Caller.SendReal (replyMsg);
							*/
							return;
						}
					}
				}
			}
#endif
}
