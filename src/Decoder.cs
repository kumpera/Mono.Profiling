using System;
using System.Collections.Generic;
using System.IO;

namespace Mono.Profiling
{
	public class Header {
		public const int LOG_HEADER_ID = 0x4D505A01;
		public const int MAJOR = 0;
		public const int MINOR = 4;
		public const int LOG_DATA_VERSION = 11;

		public int Magic { get; private set; }
		public byte Major { get; private set; }
		public byte Minor { get; private set; }
		public byte Format { get; private set; }
		public byte PointerSize { get; private set; }
		public long StartTime { get; private set; }
		public int TimerOverhead { get; private set; }
		public int Flags { get; private set; }
		public int Pid { get; private set; }
		public short Port { get; private set; }
		public short SysId { get; private set; }

		internal Header (BinaryReader reader) {
			Magic = reader.ReadInt32 ();
			Major = reader.ReadByte ();
			Minor = reader.ReadByte ();
			Format = reader.ReadByte ();
			PointerSize = reader.ReadByte ();
			StartTime = reader.ReadInt64 ();
			TimerOverhead = reader.ReadInt32 ();
			Flags = reader.ReadInt32 ();
			Pid = reader.ReadInt32 ();
			Port = reader.ReadInt16 ();
			SysId = reader.ReadInt16 ();

			if (Magic != LOG_HEADER_ID)
				throw new Exception (string.Format ("Invalid header id {0:X}", Magic));
		}
	}

	public class BufferDecoder {
		ulong timeBase;
		long pointerBase, objectBase, methodBase, threadId;
		byte[] data;
		int idx, format;

		public BufferDecoder (EventBuffer buff) {
			this.timeBase = buff.TimeBase;
			this.pointerBase = buff.PointerBase;
			this.objectBase = buff.ObjectBase;
			this.methodBase =  buff.MethodBase;
			this.threadId = buff.ThreadId;
			this.data = buff.Data;
			this.format = buff.FormatVersion;
		}

		public bool HasMoreData {
			get { return idx < data.Length; }
		}

		public int ReadByte () {
			return data [idx++];
		}

		public ulong DecodeUleb ()
		{
			ulong res = 0;
			int shift = 0;
			while (true) {
				int b = data [idx++];
				res |= ((ulong)(b & 0x7F)) << shift;
				if ((b & 0x80) == 0)
					break;
				shift += 7;
			}
			return res;
		}

		public long DecodeSleb ()
		{
			long res = 0;
			int shift = 0;

			while (true) {
				int b = data [idx++];

				res = res | (((long)(b & 0x7f)) << shift);
				shift += 7;
				if ((b & 0x80) == 0) {
					if (shift < sizeof(long) * 8 && (b & 0x40) != 0)
						res |= - (1L << shift);
					break;
				}
			}

			return res;
		}

		public T DecodeEnum<T> ()
		{
			ulong val = DecodeUleb ();
			var o = Enum.ToObject (typeof (T), val);
			if (Enum.IsDefined (typeof (T), o))
				return (T)o;
			throw new Exception (string.Format ("Invalid value {0:X} for enum {1}", val, typeof (T).Name));
		}

		public int DecodeIndex ()
		{
			ulong val = DecodeUleb ();
			if (val > (ulong)int.MaxValue)
				throw new Exception (string.Format ("Invalid value {0} > int.max", val));
			return (int)val;
		}
		public string DecodeString ()
		{
			int i;
			for (i = idx; data[i] != 0; ++i) ;
			string str = System.Text.Encoding.UTF8.GetString (data, idx, i - idx);
			idx = i + 1;
			return str;
		}

		public ulong DecodeTime ()
		{
			timeBase += DecodeUleb ();
			return timeBase;
		}

		public long DecodeMethod ()
		{
			methodBase += DecodeSleb ();
			return methodBase;
		}

		public long DecodePointer ()
		{
			return pointerBase + DecodeSleb ();
		}

		public long DecodeObject ()
		{
			return (objectBase + DecodeSleb ()) << 3;
		}

		public double DecodeDouble () {
			double res = BitConverter.ToDouble (data, idx);
			idx += 8;
			return res;
		}

		public long BufferThreadId { 
			get { return threadId; }
		}

		public int Format {
			get { return format; }
		}
	}

	public class EventBuffer {
		public const int BUF_ID = 0x4D504C01;
		public int Id { get; private set; }
		public ulong TimeBase { get; private set; }
		public long PointerBase { get; private set; }
		public long ObjectBase { get; private set; }
		public long ThreadId { get; private set; }
		public long MethodBase { get; private set; }
		public byte[] Data { get; set; }

		internal int FormatVersion { get; private set; }

		internal EventBuffer (BinaryReader reader, int formatVersion) {
			Id = reader.ReadInt32 ();
			int len = reader.ReadInt32 ();
			TimeBase = (ulong)reader.ReadInt64 ();
			PointerBase = reader.ReadInt64 ();
			ObjectBase = reader.ReadInt64 ();
			ThreadId = reader.ReadInt64 ();
			MethodBase = reader.ReadInt64 ();
			FormatVersion = formatVersion;

			Data = new byte [len];
			if (len > 0)
				reader.Read(Data, 0, len);
			if (Id != BUF_ID)
				throw new Exception (string.Format ("Invalid buffer id {0:X}", Id));
		}

		public IEnumerable<Event> GetEvents () {
			var dec = new BufferDecoder (this);
			while (dec.HasMoreData)
				yield return Event.DecodeOne (dec);
		}
	}

	public class Decoder {
		BinaryReader reader;
		Header header;
		long buffersStart;

		public Decoder (string filename) {
			this.reader = new BinaryReader (new FileStream (filename, FileMode.Open, FileAccess.Read));
			this.header = new Header (this.reader);
			this.buffersStart = this.reader.BaseStream.Position;
		}

		public void Reset () {
			this.reader.BaseStream.Position = this.buffersStart;
		}

		public Header Header {
			get { return header; }
		}

		public IEnumerable<EventBuffer> GetBuffers () {
			while (reader.PeekChar () != -1)
				yield return new EventBuffer (reader, header.Format);
		}
	}

}
