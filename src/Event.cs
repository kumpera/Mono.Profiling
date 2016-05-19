using System;
using System.Collections.Generic;

namespace Mono.Profiling
{
	public enum EventType {
		Alloc,
		Gc,
		Metadata,
		Method,
		Exception,
		Monitor,
		Heap,
		Sample,
		Runtime,
		Coverage,
	}

	public enum AllocEventType {
		NoBacktrace = 0 << 4,
		WithBacktrace = 1 << 4,
	}

	public enum GCEventType {
		GC = 1 << 4,
		Resize = 2 << 4,
		Move = 3 << 4,
		HandleCreated = 4 << 4,
		HandleDestroyed = 5 << 4,
		HandleCreatedBt = 6 << 4,
		HandleDestroyedBt = 7 << 4,
	}

	public enum MetadataEventType {
		Name = 0,
		Load = 2 << 4,
		Unload = 4 << 4,
	}

	public enum MonitorEventType {
		ProfilerMonitorContention = 1,
		ProfilerMonitorDone = 2,
		ProfilerMonitorFail = 3,
		BacktraceBit = 1 << 7
	}

	public enum MetadataKind {
		Class = 1,
		Image = 2,
		Assembly = 3,
		Domain = 4,
		Thread = 5,
		Context = 6,
	}

	public enum GCEventKind {
        Start,
        MarkStart,
        MarkEnd,
        ReclaimStart,
        ReclaimEnd,
        End,
        PreStopWorld,
        PostStopWorld,
        PreStartWorld,
        PostStartWorld,
		Last
	}

	public enum ExceptionEventType {
		Throw = 0 << 4,
		Clause = 1 << 4,
		BacktraceBit = 1 << 7
	}

	public enum GCHandleType {
		Weak,
		WeakTrack,
		Normal,
		Pinned,
		Last
	}

	public enum MethodEventType {
		Leave = 1 << 4,
		Enter = 2 << 4,
		ExceptionLeave = 3 << 4,
		Jit = 4 << 4,
	}

	public enum CodeBufferType {
		Unknown,
		Method,
		MethodTrampoline,
		UnboxTrampoline,
		ImtTrampoline,
		GenericsTramppoline,
		SpecificTrampoline,
		MiscHelper,
		Monitor,
		DelegateInvoke,
		ExceptionHandling,
		Last
	}

	public enum HeapEventType {
		Start = 0 << 4,
		End = 1 << 4,
		Object = 2 << 4,
		Root = 3 << 4,
	}

	public enum SampleEvenType {
		Hit = 0 << 4,
		USym = 1 << 4,
		UBin = 2 << 4,
		CountersDesc = 3 << 4,
		Counter = 4 << 4,
	}

	public enum SampleHitType {
		Cycles = 1,
		Intructions,
		CacheMisses,
		CacheRefs,
		Branches,
		BranchMisses,
	}

	public enum CounterSection {
		JIT = 1 << 8,
		GC = 1 << 9,
		Metadata = 1 << 10,
		Generics = 1 << 11,
		Security = 1 << 12,
		Runtime = 1 << 13,
		System = 1 << 14,
		PerfCounters = 1 << 15,
	}

	public enum CounterType {
		Int,    /* 32 bit int */
		UInt,
		Word,
		Long,
		ULong,
		Double,
		String,
		TimeInterval,
	}

	public enum CounterUnit {
		Raw = 0 << 24,
		Bytes = 1 << 24,
		Time = 2 << 24,
		Count = 3 << 24,
		Percentage = 4 << 24
	}

	public enum CounterVariance {
		Monotonic = 1 << 28,
		Constant = 1 << 29,
		Variable = 1 << 30
	}

	public enum RuntimeEvenType {
		JitHelper = 1 << 4,
	}

	public class CounterDesc {
		public CounterSection Section { get; private set; }
		public string SectionName { get; private set; }
		public string Name { get; private set; }
		public CounterType Type { get; private set; }
		public CounterUnit Unit { get; private set; }
		public CounterVariance Variance { get; private set; }
		public int Index { get; private set; }

		public CounterDesc (CounterSection section, string sectionName, string name, CounterType type, CounterUnit unit, CounterVariance variance, int index)
		{
			Section = section;
			SectionName = sectionName;
			Name = name;
			Type = type;
			Unit = unit;
			Variance = variance;
			Index = index;
		}

		public override string ToString ()
		{
			return string.Format ("{0}:{1} ({2}, {3}, {4}) [{5}]",
				SectionName == "" ? Section.ToString () : SectionName, Name,
				Type, Unit, Variance,
				Index);
		}
	}

	public class CounterSample {
		public int Index { get; private set; }
		public CounterType Type { get; private set; }
		public object Value { get; private set; }

		public CounterSample (int index, CounterType type, object value)
		{
			Index = index;
			Type = type;
			Value = value;
		}

		public override String ToString ()
		{
			return string.Format ("Sample [{0}] ({1}): {2}", Index, Type, Value);
		}
	}

	public abstract class MethodEvent : Event {
		public ulong Time { get; private set; }
		public long MethodId { get; private set; }

		public override EventType EventType {
			get { return EventType.Method; }
		}

		public MethodEvent (ulong time, long methodId)
		{
			Time = time;
			MethodId = methodId;
		}
	}
	public class MethodSteppingEvent : MethodEvent {
		public MethodEventType Type { get; private set; }

		public MethodSteppingEvent (ulong time, long methodId, MethodEventType type) : base (time, methodId)
		{
			Type = type;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("MethodStepping Time:{0:X} MethodId:{1:X} Type:{2}", Time, MethodId, Type);
		}
	}

	public class CompiledMethodEvent : MethodEvent {
		public long CodeAddress { get; private set; }
		public ulong CodeSize { get; private set; }
		public string Name { get; private set; }

		public CompiledMethodEvent (ulong time, long methodId, long codeAddress, ulong codeSize, string name) : base (time, methodId)
		{
			CodeAddress = codeAddress;
			CodeSize = codeSize;
			Name = name;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("CompiledMethod Time:{0:X} MethodId:{1:X} Address:{2:X} Size:{3} Name:{4}", Time, MethodId, CodeAddress, CodeSize, Name);
		}
	}

	public class AllocEvent : Event {
		public ulong Time { get; private set; }
		public long TypeId { get; private set; }
		public long ObjectId { get; private set; }
		public ulong Size { get; private set; }
		public long[] Frames { get; private set; }
		public override EventType EventType {
			get { return EventType.Alloc; }
		}

		public AllocEvent (ulong time, long typeId, long objectId, ulong size, long[] frames)
		{
			Time = time;
			TypeId = typeId;
			ObjectId = objectId;
			Size = size;
			Frames = frames;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("AllocEvent Time:{0:X} TypeId:{1:X} ObjectId:{2:X} Size:{3} Frames:{4}", Time, TypeId, ObjectId, Size, Frames == null ? -1 : Frames.Length);
		}
	}


	public abstract class ExceptionEvent : Event {
		public ulong Time { get; protected set; }

		public override EventType EventType {
			get { return EventType.Exception; }
		}
	}

	public class ExceptionClauseEvent : ExceptionEvent {
		public ulong ClauseType { get; private set; }
		public ulong ClauseNum { get; private set; }
		public long MethodId { get; private set; }

		public ExceptionClauseEvent (ulong time, ulong clauseType, ulong clauseNum, long methodId)
		{
			Time = time;
			ClauseType = clauseType;
			ClauseNum = clauseNum;
			MethodId = methodId;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("ExceptionClauseEvent Time:{0:X} ClauseType:{1} ClauseNum:{2} MethodId:{3:X}", Time, ClauseType, ClauseNum, MethodId);
		}
	}

	public class ExceptionThrownEvent : ExceptionEvent {
		public long ObjectId { get; private set; }
		public long[] Frames { get; private set; }
		public ExceptionThrownEvent (ulong time, long objectId, long[] frames)
		{
			Time = time;
			ObjectId = objectId;
			Frames = frames;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("ExceptionThrown Time:{0:X} ObjectId:{1:X} Frames{2}", Time, ObjectId, Frames == null ? -1 : Frames.Length);
		}
	}

	public abstract class RuntimeEvent : Event {
		public override EventType EventType {
			get { return EventType.Runtime; }
		}
	}

	public class JitHelperEvent : RuntimeEvent {
		public ulong Time { get; private set; }
		public CodeBufferType Type { get; private set; }
		public long Address { get; private set; }
		public ulong Size { get; private set; }
		public string Name { get; private set; }

		public JitHelperEvent (ulong time, CodeBufferType type, long address, ulong size, string name)
		{
			Time = time;
			Type = type;
			Address = address;
			Size = size;
			Name = name;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("JitHelper Time:{0:X} Type:{1} Address:{2:X} Size:{3} Name:{4}", Time, Type, Address, Size, Name);
		}
	}

	public abstract class MetadataEvent : Event {
		public ulong Time { get; private set; }
		public long Id { get; private set; }
		public MetadataEventType Kind { get; private set; }
		public ulong Flags { get; private set; }

		public override EventType EventType {
			get { return EventType.Metadata; }
		}

		public MetadataEvent (ulong time, long id, MetadataEventType kind, ulong flags)
		{
			Time = time;
			Id = id;
			Kind = kind;
			Flags = flags;
		}

	}

	public class DomainEvent : MetadataEvent {
		public DomainEvent (ulong time, long id, MetadataEventType kind, ulong flags) : base (time, id, kind, flags)
		{
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("Domain Time:{0:X} Id:{1:X} EventKind:{2} Flags:{3:X}", Time, Id, Kind, Flags);
		}
	}

	public class DomainFriendlyNameEvent : DomainEvent {
		public string FriendlyName { get; private set; }

		public DomainFriendlyNameEvent (ulong time, long id, ulong flags, string name) : base (time, id, MetadataEventType.Name, flags)
		{
			FriendlyName = name;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("DomainFriendlyName Time:{0:X} Id:{1:X} Flags:{2:X} Name:{3}", Time, Id, Flags, FriendlyName);
		}
	}

	public class ThreadEvent : MetadataEvent {
		public ThreadEvent (ulong time, long id, MetadataEventType kind, ulong flags) : base (time, id, kind, flags)
		{
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("Thread Time:{0:X} Id:{1:X} EventKind:{2} Flags:{3:X}", Time, Id, Kind, Flags);
		}
	}

	public class ThreadNameEvent : ThreadEvent {
		public string Name { get; private set; }

		public ThreadNameEvent (ulong time, long id, ulong flags, string name) : base (time, id, MetadataEventType.Name, flags)
		{
			Name = name;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("ThreadNameEvent Time:{0:X} Id:{1:X} Flags:{2:X} Name:{3}", Time, Id, Flags, Name);
		}
	}

	public class ImageEvent : MetadataEvent {
		public string Name { get; private set; }

		public ImageEvent (ulong time, long id, MetadataEventType kind, ulong flags, string name) : base (time, id, kind, flags)
		{
			Name = name;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("Image Time:{0:X} Id:{1:X} EventKind:{2} Flags:{3:X} Name:{4}", Time, Id, Kind, Flags, Name);
		}
	}

	public class AssemblyEvent : MetadataEvent {
		public string Name { get; private set; }

		public AssemblyEvent (ulong time, long id, MetadataEventType kind, ulong flags, string name) : base (time, id, kind, flags)
		{
			Name = name;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("Assembly Time:{0:X} Id:{1:X} EventKind:{2} Flags:{3:X} Name:{4}", Time, Id, Kind, Flags, Name);
		}
	}

	public class ClassEvent : MetadataEvent {
		public long ImageId { get; private set; }
		public string Name { get; private set; }

		public ClassEvent (ulong time, long id, MetadataEventType kind, long imageId, ulong flags, string name) : base (time, id, kind, flags)
		{
			ImageId = imageId;
			Name = name;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("Class Time:{0:X} Id:{1:X} EventKind:{2} ImageId:{3:X} Flags:{4:X} Name:{5}", Time, Id, Kind, ImageId, Flags, Name);
		}
	}


	public class ContextEvent : MetadataEvent {
		public long DomainId { get; private set; }

		public ContextEvent (ulong time, long id, MetadataEventType kind, ulong flags, long domainId) : base (time, id, kind, flags)
		{
			DomainId = domainId;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("ContextEvent Time:{0:X} Id:{1:X} EventKind:{2} Flags:{3:X} DomanId:{4:X}", Time, Id, Kind, Flags, DomainId);
		}
	}

	public abstract class GCEvent : Event {
		public override EventType EventType {
			get { return EventType.Gc; }
		}
	}

	public class GCPhaseEvent : GCEvent {
		public ulong Time { get; private set; }
		public GCEventKind Kind { get; private set; }
		public int Generation { get; private set; }

		public GCPhaseEvent (ulong time, GCEventKind kind, int generation)
		{
			Time = time;
			Kind = kind;
			Generation = generation;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("GCEvent Time:{0:X} Kind:{1} Generation:{2}", Time, Kind, Generation);
		}
	}

	public class GCMoveEvent : GCEvent {
		public ulong Time { get; private set; }
		public long[] MovedObjects { get; private set; }

		public GCMoveEvent (ulong time, long[] movedObjects)
		{
			Time = time;
			MovedObjects = movedObjects;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			String str = string.Format ("GCMoves Time:{0:X} Objects:{1}", Time, MovedObjects.Length / 2);
			for (int i = 0; i < MovedObjects.Length; i += 2)
				str += string.Format ("\n\t{0:X} -> {1:X}", MovedObjects [i], MovedObjects [i + 1]);
			return str;

		}
	}

	public enum GCRootType {
		Stack = 0,
		Finalizer = 1,
		Handle = 2,
		Other = 3,
		Misc = 4,
		Mask = 0xFF
	}

	public enum GCRootModifier {
		Normal   = 0 << 8,
		Pinning  = 1 << 8,
		WeakRef  = 2 << 8,
		Interior = 4 << 8,
		Mask = 0xFF00
	}

	public class RootInfo {
		public long Object { get; private set ; }
		public GCRootType Type { get; private set ; }
		public GCRootModifier Modifier { get; private set ; }
		public ulong ExtraInfo { get; private set ; }

		public RootInfo (long obj, GCRootType type, GCRootModifier modifier, ulong extra)
		{
			Object = obj;
			Type = type;
			Modifier = modifier;
			ExtraInfo = extra;
		}

		public override string ToString ()
		{
			return string.Format ("Root Object:{0:X} Type:{1} Modifier:{2}, Extra:{3:X}", Object, Type, Modifier, ExtraInfo);
		}
	}

	public abstract class HeapEvent : Event {
		public override EventType EventType {
			get { return EventType.Heap; }
		}
	}

	public class HeapshotStartEvent : HeapEvent
	{
		public ulong Time { get; private set; }

		public HeapshotStartEvent (ulong time)
		{
			Time = time;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("HeapshotStarted Time:{0:X}", Time);
		}
	}

	public class HeapshotEndEvent : HeapEvent
	{
		public ulong Time { get; private set; }

		public HeapshotEndEvent (ulong time)
		{
			Time = time;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("HeapshotEnded Time:{0:X}", Time);
		}
	}

	public class HeapshotObjectEvent : HeapEvent
	{
		public long ObjectId { get; private set; }
		public long TypeId { get; private set; }
		public ulong Size { get; private set; }
		public long[] References { get; private set; }

		public HeapshotObjectEvent (long address, long type, ulong size, long [] references)
		{
			ObjectId = address;
			TypeId = type;
			Size = size;
			References = references;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("HeapshotObject ObjectId:{0:X} TypeId:{1:X} Size:{2} References:{3}", ObjectId, TypeId, Size, References == null ? 0 : References.Length);
		}
	}

	public class RootsEvent : HeapEvent {
		public int GCCount { get; private set ; }
		public RootInfo[] Roots { get; private set ; }

		public RootsEvent (int gccount, RootInfo[] roots)
		{
			GCCount = gccount;
			Roots = roots;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			String str = string.Format ("Roots gcCount:{0} count{1}", GCCount, Roots.Length);
			foreach (var root in Roots) {
				str += "\n\t" + root.ToString ();
			}
			return str;
		}
	}

	public class HandleCreatedEvent : GCEvent {
		public ulong Time { get; private set; }
		public GCHandleType Type { get; private set; }
		public ulong HandleId { get; private set; }
		public long ObjectId { get; private set; }
		public long[] Frames { get; private set; }

		public HandleCreatedEvent (ulong time, GCHandleType type, ulong handleId, long objectId, long[] frames)
		{
			Time = time;
			Type = type;
			HandleId = handleId;
			ObjectId = objectId;
			Frames = frames;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("HandleCreatedEvent Time:{0:X} Type:{1} HandleId:{2:X} ObjectId:{3:X} Frames:{4}", Time, Type, HandleId, ObjectId, Frames == null ? -1 : Frames.Length);
		}
	}

	public class HandleDestroyedEvent : GCEvent
	{
		public ulong Time { get; private set; }
		public GCHandleType Type { get; private set; }
		public ulong HandleId { get; private set; }
		public long[] Frames { get; private set; }

		public HandleDestroyedEvent (ulong time, GCHandleType type, ulong handleId, long[] frames)
		{
			Time = time;
			Type = type;
			HandleId = handleId;
			Frames = frames;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("HandleDestroyedEvent Time:{0:X} Type:{1} HandleId:{2:X} Frames{3}", Time, Type, HandleId, Frames == null ? -1 : Frames.Length);
		}
	}

	public class CountersDescEvent : SamplingEvent {
		public CounterDesc[] Counters { get; private set; }

		public CountersDescEvent (CounterDesc[] counters)
		{
			this.Counters = counters;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			String str = string.Format ("CountersDesc Counters:{0}", Counters.Length);
			foreach (var cd in Counters) {
				str += "\n\t" + cd.ToString ();
			}
			return str;
		}
	}

	public class CounterSampleEvent : SamplingEvent {
		public ulong Time { get; private set; }

		public CounterSample[] Samples { get; private set; }

		public CounterSampleEvent (ulong time, CounterSample[] samples)
		{
			this.Time = time;
			this.Samples = samples;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			String str = string.Format ("CounterSampleEvent Time:{0:x} Samples:{1}", Time, Samples.Length);
			foreach (var sample in Samples) {
				str += "\n\t" + sample.ToString ();
			}
			return str;
		}
	}

	public class ManagedHit {
		public long MethodId { get; private set; }
		public int IlOffset { get; private set; }
		public int NativeOffset { get; private set; }

		public ManagedHit (long methodId, int ilOffset, int nativeOffset)
		{
			MethodId = methodId;
			IlOffset = ilOffset;
			NativeOffset = nativeOffset;
		}

		public override string ToString ()
		{
			return string.Format ("ManagedHit MethodId:{0:X} IlOffset:{1} NativeOffset:{2}", MethodId, IlOffset, NativeOffset);
		}
	}

	public abstract class SamplingEvent : Event {
		public override EventType EventType {
			get { return EventType.Sample; }
		}
	}

	public class SampleHitEvent : SamplingEvent {
		public ulong Time { get; private set; }
		public SampleHitType Type { get; private set; }
		public long ThreadId { get; private set; }
		public long[] NativeFrames { get; private set; }
		public ManagedHit[] ManagedFrames { get; private set; }

		public SampleHitEvent (ulong time, SampleHitType type, long threadId, long[] nativeFrames, ManagedHit[] managedFrames)
		{
			Time = time;
			Type = type;
			ThreadId = threadId;
			NativeFrames = nativeFrames;
			ManagedFrames = managedFrames;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			String str = string.Format ("SampleHit Time:{0:X} Type:{1} ThreadId:{2:X} NativeFrames:{3} ManagedFrames:{4}", Time, Type, ThreadId, NativeFrames.Length, ManagedFrames.Length);
			foreach (var frame in NativeFrames)
				str += "\n\t native: " + frame.ToString ();
			foreach (var frame in ManagedFrames)
				str += "\n\t managed: " + frame.ToString ();

			return str;
		}
	}

	public class USymEvent : SamplingEvent {
		public long Address { get; private set; }
		public int Size { get; private set; }
		public string Name { get; private set; }

		public USymEvent (long address, int size, string name)
		{
			Address = address;
			Size = size;
			Name = name;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("USymEvent Adress:{0:X} Size:{1} Name:{2}", Address, Size, Name);
		}
	}

	public class MonitorEvent : Event {
		public ulong Time { get; private set; }
		public long ObjectId { get; private set; }
		public long[] Frames { get; private set; }

		public override EventType EventType {
			get { return EventType.Monitor; }
		}

		public MonitorEvent (ulong time, long objectId, long[] frames)
		{
			Time = time;
			ObjectId = objectId;
			Frames = frames;
		}

		public override void Visit (EventVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString ()
		{
			return string.Format ("MonitorEvent Time:{0:X} ObjectId:{1:X} Frames:{2}", Time, ObjectId, Frames == null ? -1 : Frames.Length);
		}
	}


	public abstract class Event {
		public const int MAX_FRAMES = 32;
		public const int MAX_GENERATION = 4;

		public abstract EventType EventType { get; }
		public abstract void Visit (EventVisitor visitor);


		static long[] DecodeBacktrace (BufferDecoder dec)
		{
			ulong flags = dec.DecodeUleb ();
			if (flags != 0)
				throw new Exception (string.Format ("Backtrace with non zero flag {0}", flags));

			ulong num_frames = dec.DecodeUleb ();
			if (num_frames > MAX_FRAMES)
				throw new Exception (string.Format ("Backtrace with more frames than allowed {0}", num_frames));
			var frames = new long [(int)num_frames];
			for (int i = 0; i < frames.Length; ++i)
				frames [i] = dec.DecodePointer ();

			return frames;
		}

		static Event DecodeAllocEvent (BufferDecoder dec, AllocEventType type)
		{
			ulong time = dec.DecodeTime ();
			long typeId = dec.DecodePointer ();
			long objectId = dec.DecodeObject ();
			ulong size = dec.DecodeUleb ();
			long[] frames = null;

			if (type == AllocEventType.WithBacktrace)
				frames = DecodeBacktrace (dec);

			return new AllocEvent (time, typeId, objectId, size, frames);
		}

		static Event DecodeGCEvent (BufferDecoder dec, GCEventType type)
		{
			ulong time = dec.DecodeTime ();
			ulong handle_type;
			ulong handle;
			long[] frames = null;

			switch (type) {
			case GCEventType.GC: {
				ulong event_type = dec.DecodeUleb ();
				ulong generation = dec.DecodeUleb ();
				if (event_type >= (ulong)GCEventKind.Last)
					throw new Exception (string.Format ("Invalid event_type {0}", event_type));
				if (generation > MAX_GENERATION)
					throw new Exception (string.Format ("Invalid generation {0}", generation));
				return new GCPhaseEvent (time, (GCEventKind)event_type, (int)generation);
			}
			case GCEventType.Resize:
				throw new Exception (string.Format ("Invalid GC event type {0}", type));

			case GCEventType.Move: {
				int count = dec.DecodeIndex ();
				if ((count & 1) == 1)
					throw new Exception ("count must be an even number");
				long[] moved_objects = new long [count];
				for (int i = 0; i < count; ++i)
					moved_objects [i] = dec.DecodeObject ();
				return new GCMoveEvent (time, moved_objects);
			}

			case GCEventType.HandleCreatedBt:
			case GCEventType.HandleCreated: {
				handle_type = dec.DecodeUleb ();
				handle = dec.DecodeUleb ();
				long obj = dec.DecodeObject ();
				if (type == GCEventType.HandleCreatedBt)
					frames = DecodeBacktrace (dec);
				if (handle_type >= (ulong)GCHandleType.Last)
					throw new Exception (string.Format ("Invalid GC handle type {0}", handle_type));

				return new HandleCreatedEvent (time, (GCHandleType)handle_type, handle, obj, frames);
			}

			case GCEventType.HandleDestroyed:
			case GCEventType.HandleDestroyedBt:
				handle_type = dec.DecodeUleb ();
				handle = dec.DecodeUleb ();
				if (type == GCEventType.HandleDestroyedBt)
					frames = DecodeBacktrace (dec);
				if (handle_type >= (ulong)GCHandleType.Last)
					throw new Exception (string.Format ("Invalid GC handle type {0}", handle_type));

				return new HandleDestroyedEvent (time, (GCHandleType)handle_type, handle, frames);

			default:
				throw new Exception (string.Format ("Invalid GC event type {0}", type));

			}
/*
 * type GC format:
 * type: TYPE_GC
 * exinfo: one of TYPE_GC_EVENT, TYPE_GC_RESIZE, TYPE_GC_MOVE, TYPE_GC_HANDLE_CREATED[_BT],
 * TYPE_GC_HANDLE_DESTROYED[_BT]
 * [time diff: uleb128] nanoseconds since last timing
 * if exinfo == TYPE_GC_RESIZE
 *	[heap_size: uleb128] new heap size

 * if exinfo == TYPE_GC_HANDLE_DESTROYED[_BT]
 *	[handle_type: uleb128] GC handle type (System.Runtime.InteropServices.GCHandleType)
 *	upper bits reserved as flags
 *	[handle: uleb128] GC handle value
 * 	If exinfo == TYPE_GC_HANDLE_DESTROYED_BT, a backtrace follows.
 */
		}

		static Event DecodeMetadataEvent (BufferDecoder dec, MetadataEventType loadType)
		{
			ulong time = dec.DecodeTime ();
			MetadataKind type = (MetadataKind)dec.ReadByte ();
			long id = dec.DecodePointer ();

			switch (type) {
			case MetadataKind.Class: {
				long image = dec.DecodePointer ();
				ulong flags = dec.DecodeUleb ();
				string name = dec.DecodeString ();
				return new ClassEvent (time, id, loadType, image, flags, name);
			}

			case MetadataKind.Image: {
				ulong flags = dec.DecodeUleb ();
				string name = dec.DecodeString ();
				return new ImageEvent (time, id, loadType, flags, name);
			}

			case MetadataKind.Assembly: {
				ulong flags = dec.DecodeUleb ();
				string name = dec.DecodeString ();
				return new AssemblyEvent (time, id, loadType, flags, name);
			}

			case MetadataKind.Domain: {
				ulong flags = dec.DecodeUleb ();

				if (loadType == MetadataEventType.Name)
					return new DomainFriendlyNameEvent (time, id, flags, dec.DecodeString ());
				else
					return new DomainEvent (time, id, loadType, flags);
			}

			case MetadataKind.Thread: {
				ulong flags = dec.DecodeUleb ();

				if (loadType == MetadataEventType.Name)
					return new ThreadNameEvent (time, id, flags, dec.DecodeString ());
				else
					return new ThreadEvent (time, id, loadType, flags);
			}

			case MetadataKind.Context: {
				ulong flags = dec.DecodeUleb ();
				long domainId = dec.DecodePointer ();
				return new ContextEvent (time, id, loadType, flags, domainId);
			}
			default:
				throw new Exception (string.Format ("Invalid Metadata event type {0}", type));
			}
		}

		static Event DecodeMethodEvent (BufferDecoder dec, MethodEventType type)
		{
			ulong time = dec.DecodeTime ();
			long methodId = dec.DecodeMethod ();

			switch (type) {
			case MethodEventType.Leave:
			case MethodEventType.Enter:
			case MethodEventType.ExceptionLeave:
				return new MethodSteppingEvent (time, methodId, type);
			case MethodEventType.Jit:
				long codeAddress = dec.DecodePointer ();
				ulong codeSize = dec.DecodeUleb ();
				string name = dec.DecodeString ();
				return new CompiledMethodEvent (time, methodId, codeAddress, codeSize, name);
			default:
				throw new Exception (string.Format ("Don't know how to decode method event of type {0}", type));
			}
		}

		static Event DecodeMonitorEvent (BufferDecoder dec, MonitorEventType type)
		{
			ulong time = dec.DecodeTime ();
			long objectId = dec.DecodeObject ();
			long[] frames = null;
			byte ev = (byte) (((int) type >> 4) & 0x3);
			if (ev == (byte) MonitorEventType.ProfilerMonitorContention && type.HasFlag (MonitorEventType.BacktraceBit))
				frames = DecodeBacktrace (dec);

			return new MonitorEvent (time, objectId, frames);
		}

		static Event DecodeHeapEvent (BufferDecoder dec, HeapEventType type)
		{
			ulong time;

			switch (type) {
			case HeapEventType.Start:
				time = dec.DecodeTime ();
				return new HeapshotStartEvent (time);

			case HeapEventType.End:
				time = dec.DecodeTime ();
				return new HeapshotEndEvent (time);

			case HeapEventType.Object:
				long objectId = dec.DecodeObject ();
				long typeId = dec.DecodePointer ();
				ulong size = dec.DecodeUleb ();
				ulong numRefs = dec.DecodeUleb ();
				long[] references = new long [numRefs];
				for (ulong i = 0; i < numRefs; i++) {
					dec.DecodeSleb (); //this read unused RelOffset
					references [i] = dec.DecodeSleb ();
				}
				return new HeapshotObjectEvent (objectId, typeId, size, references);

			case HeapEventType.Root: {
				int count = dec.DecodeIndex ();
				int gcCount = dec.DecodeIndex ();
				var roots = new RootInfo [count];
				for (int i = 0; i < roots.Length; ++i) {
					var obj = dec.DecodeObject ();
					var root_type_encoded = dec.DecodeIndex ();
					ulong extra_info = dec.DecodeUleb ();

					GCRootType root_type = (GCRootType)(root_type_encoded & (int)GCRootType.Mask);
					GCRootModifier root_modifier = (GCRootModifier)(root_type_encoded & (int)GCRootModifier.Mask);
					roots [i] = new RootInfo (obj, root_type, root_modifier, extra_info);
				}
				return new RootsEvent (gcCount, roots);
			}
			default:
				throw new Exception (string.Format ("Don't know how to decode heap event of type {0}", type));
			}
		}

		/*
 * type heap format
 * type: TYPE_HEAP
 * exinfo: one of TYPE_HEAP_START, TYPE_HEAP_END, TYPE_HEAP_OBJECT, TYPE_HEAP_ROOT
 * if exinfo == TYPE_HEAP_START
 * 	[time diff: uleb128] nanoseconds since last timing
 * if exinfo == TYPE_HEAP_END
 * 	[time diff: uleb128] nanoseconds since last timing
 * if exinfo == TYPE_HEAP_OBJECT
 * 	[object: sleb128] the object as a difference from obj_base
 * 	[class: sleb128] the object MonoClass* as a difference from ptr_base
 * 	[size: uleb128] size of the object on the heap
 * 	[num_refs: uleb128] number of object references
 * 	if (format version > 1) each referenced objref is preceded by a
 *	uleb128 encoded offset: the first offset is from the object address
 *	and each next offset is relative to the previous one
 * 	[objrefs: sleb128]+ object referenced as a difference from obj_base
 * 	The same object can appear multiple times, but only the first time
 * 	with size != 0: in the other cases this data will only be used to
 * 	provide additional referenced objects.
 */

		static Event DecodeSampleEvent (BufferDecoder dec, SampleEvenType type)
		{
			switch (type) {
	   		case SampleEvenType.Hit: {
	   			var hit = dec.DecodeEnum <SampleHitType> ();
				ulong time = dec.DecodeUleb (); //WTF
				long threadId = dec.BufferThreadId;
				if (dec.Format > 10)
					threadId = (long)dec.DecodeUleb ();
				int native_frames_count = dec.DecodeIndex ();
				long[] native_frames = new long [native_frames_count];
				for (int i = 0; i < native_frames.Length; ++i)
					native_frames [i] = dec.DecodePointer ();
				ManagedHit[] managed_frames = null;
				if (dec.Format > 5) {
					int managed_count = dec.DecodeIndex ();
					managed_frames = new ManagedHit [managed_count];
					for (int i = 0; i < managed_frames.Length; ++i) {
						long method = dec.DecodeMethod ();
						int il_offset = dec.DecodeIndex ();
						int native_offset = dec.DecodeIndex ();
						managed_frames [i] = new ManagedHit (method, il_offset, native_offset);
					}
				} else {
					managed_frames = new ManagedHit [0];
				}
				return new SampleHitEvent (time, hit, threadId, native_frames, managed_frames);
	   		}
		    /*  * if exinfo == TYPE_SAMPLE_USYM
 * 	[address: sleb128] symbol address as a difference from ptr_base
 * 	[size: uleb128] symbol size (may be 0 if unknown)
 * 	[name: string] symbol name
 * if exinfo == TYPE_SAMPLE_UBIN
 * 	[time diff: uleb128] nanoseconds since last timing
 * 	[address: sleb128] address where binary has been loaded
 * 	[offset: uleb128] file offset of mapping (the same file can be mapped multiple times)
 * 	[size: uleb128] memory size
 * 	[name: string] binary name*/
	   		case SampleEvenType.USym: {
	   			long address = dec.DecodePointer ();
				int size = dec.DecodeIndex ();
				string name = dec.DecodeString ();
				return new USymEvent (address, size, name);
	   		}
	   		case SampleEvenType.UBin:
				throw new Exception (string.Format ("Don't know how to decode sample event of type {0}", type));
	   		case SampleEvenType.CountersDesc: {
	   			ulong len = dec.DecodeUleb ();
				var counters = new CounterDesc [checked ((int)len)];
				for (int i = 0; i < counters.Length; ++i) {
					var section = dec.DecodeEnum <CounterSection> ();
					string section_name = "";
					if (section == CounterSection.PerfCounters)
						section_name = dec.DecodeString ();
					string name = dec.DecodeString ();
					var counter_type = dec.DecodeEnum<CounterType> ();
					var unit = dec.DecodeEnum<CounterUnit> ();
					var variance = dec.DecodeEnum<CounterVariance> ();
					var index = dec.DecodeIndex ();
					counters [i] = new CounterDesc (section, section_name, name, counter_type, unit, variance, index);
				}
				return new CountersDescEvent (counters);
	   		}

	   		case SampleEvenType.Counter: {
   				ulong time = dec.DecodeTime ();
				var samples = new List<CounterSample> ();
				while (true) {
					var idx = dec.DecodeIndex ();
					if (idx == 0)
						break;
					var counter_type = dec.DecodeEnum<CounterType> ();
					object value = null;
					switch (counter_type) {
					case CounterType.Int:
					case CounterType.Word:
					case CounterType.Long:
					case CounterType.TimeInterval:
						value = dec.DecodeSleb ();
						break;
					case CounterType.UInt:
					case CounterType.ULong:
						value = dec.DecodeUleb ();
						break;
					case CounterType.Double:
						value = dec.DecodeDouble ();
						break;
					case CounterType.String:
						if (dec.ReadByte () == 0)
							value = null;
						else
							value = dec.DecodeString ();
						break;
					default:
						throw new Exception (string.Format ("Don't know how to decode {0}", counter_type));
					}
					samples.Add (new CounterSample (idx, counter_type, value));
				}
				return new CounterSampleEvent (time, samples.ToArray ());
	   		}

			default:
				throw new Exception (string.Format ("Don't know how to decode sample event of type {0}", type));
			}
		}

		static Event DecodeRuntimeEvent (BufferDecoder dec, RuntimeEvenType type)
		{
			ulong time = dec.DecodeTime ();
			switch (type) {
			case RuntimeEvenType.JitHelper:
				ulong buffer_type = dec.DecodeUleb ();
				long address = dec.DecodePointer ();
				ulong size = dec.DecodeUleb ();
				string name = "";

				if (buffer_type >= (ulong)CodeBufferType.Last)
					throw new Exception (string.Format ("Invalid buffer_type {0}", buffer_type));
				if ((CodeBufferType)buffer_type == CodeBufferType.SpecificTrampoline)
					name = dec.DecodeString ();

				return new JitHelperEvent (time, (CodeBufferType)buffer_type, address, size, name);
			default:
				throw new Exception (string.Format ("Don't know how to decode runtime event of type {0}", type));
			}
		}

		static Event DecodeExceptionEvent (BufferDecoder dec, ExceptionEventType type)
		{
			ulong time = dec.DecodeTime ();
			switch (type & (ExceptionEventType.BacktraceBit - 1)) {
			case ExceptionEventType.Clause:
				ulong clauseType = dec.DecodeUleb ();
				ulong clauseNum = dec.DecodeUleb ();
				long methodBase = dec.DecodeSleb ();
				return new ExceptionClauseEvent (time, clauseType, clauseNum, methodBase);

			case ExceptionEventType.Throw:
				long objectId = dec.DecodePointer ();
				long[] frames = null;
				if ((type & ExceptionEventType.BacktraceBit) == ExceptionEventType.BacktraceBit) {
					frames = DecodeBacktrace (dec);
				}

				return new ExceptionThrownEvent (time, objectId, frames);
			default:
				throw new Exception (string.Format ("Don't know how to decode runtime event of type {0}", type));
			}
		}

		internal static Event DecodeOne (BufferDecoder dec)
		{
			int event_id = dec.ReadByte ();
			EventType type = (EventType)(event_id & 0xF);
			int extended_type = event_id & 0xF0;

			switch (type) {
			case EventType.Alloc:
				return DecodeAllocEvent (dec, (AllocEventType)extended_type);
			case EventType.Exception:
				return DecodeExceptionEvent (dec, (ExceptionEventType)extended_type);
			case EventType.Gc:
				return DecodeGCEvent (dec, (GCEventType)extended_type);
			case EventType.Metadata:
				return DecodeMetadataEvent (dec, (MetadataEventType)extended_type);
			case EventType.Method:
				return DecodeMethodEvent (dec, (MethodEventType)extended_type);
			case EventType.Monitor:
				return DecodeMonitorEvent (dec, (MonitorEventType) extended_type);
			case EventType.Heap:
				return DecodeHeapEvent (dec, (HeapEventType)extended_type);
			case EventType.Sample:
				return DecodeSampleEvent (dec, (SampleEvenType)extended_type);
			case EventType.Runtime:
				return DecodeRuntimeEvent (dec, (RuntimeEvenType)extended_type);
			default:
				throw new Exception (string.Format ("Don't know how to decode event of type {0}", type));
			}
		}
	}
}
