using System;
using System.Collections.Generic;
using System.IO;

namespace Mono.Profiling
{
	/*
	Missing checks:

	CompileMethod
	JitHelper
		Code range overlap

	AllocEvent
		Check for overlapped allocations between 2 GC cycles

	HandleCreatedEvent
		Check if the object is valid

	Ignored events:

	USymEvent
		We don't care about this puppy

	Bugs found:

	missing thread exit events
	invalid counter index

	*/

	struct Allocation {
		public ulong Time;
		public long TypeId;
		public long ObjectId;
		public ulong Size;
		public long[] Frames;

		public Allocation (ulong time, long typeId, long objectId, ulong size, long[] frames)
		{
			Time = time;
			TypeId = typeId;
			ObjectId = objectId;
			Size = size;
			Frames = frames;
		}
	}

	public class MprofLinter {
		Decoder decoder;
		VerificationVisitor visitor;

		public MprofLinter (Decoder decoder)
		{
			this.decoder = decoder;
		}


		void VerifyBuffer (EventBuffer buffer)
		{
			//Not really much to do here
		}

		class VerificationVisitor : EventVisitor {
			//the profiler doesn't report method sizes
			const bool ReportInvalidSampleOffset = false;

			int event_count, ignored_count, errors;

			HashSet<long> loadedTypes = new HashSet <long> ();
			HashSet<long> runningThreads = new HashSet <long> ();
			Dictionary<long, Allocation> allocations = new Dictionary<long, Allocation> ();

			void Fail (Event evt, string reason) {
				Console.WriteLine ("FAIL: {0} on {1}", reason, evt.GetType ().Name);
				++errors;
			}

			void Fail (string reason) {
				Console.WriteLine ("FAIL: {0}", reason);
				++errors;
			}

			void VerifyBacktrace (Event evt, long[] frames)
			{
				if (frames == null || frames.Length == 0)
					return;
				foreach (var frame in frames) {
					if (!loadedMethods.ContainsKey (frame)) {
						Fail (evt, string.Format ("Invalid MethodId {0:X}", frame));
					}
				}
			}

			internal VerificationVisitor () {
			}

			public override void VisitDefault (Event evt) {
				++ignored_count;
				Console.WriteLine (evt);
			}

			//Metadata events
			public override void Visit (ClassEvent cls) {
				if (!loadedImages.Contains (cls.ImageId))
					Fail (cls, "Owning image not found");

				if (cls.Kind == MetadataEventType.Load) {
					if (loadedTypes.Contains (cls.Id))
						Fail (cls, "Type already loaded");
					loadedTypes.Add (cls.Id);
				} else if (cls.Kind == MetadataEventType.Unload) {
					if (!loadedTypes.Contains (cls.Id))
						Fail (cls, "Type not loaded");
					loadedTypes.Remove (cls.Id);
				} else {
					Fail (cls, "Invalid event Kind");
				}
				++event_count;
			}

			HashSet<long> loadedImages = new HashSet <long> ();
			public override void Visit (ImageEvent image) {
				if (image.Kind == MetadataEventType.Load) {
					if (loadedImages.Contains (image.Id))
						Fail (image, "Image already loaded");
					loadedImages.Add (image.Id);
				} else if (image.Kind == MetadataEventType.Unload) {
					if (!loadedImages.Contains (image.Id))
						Fail (image, "Image not loaded");
					loadedImages.Remove (image.Id);
				} else {
					Fail (image, "Invalid event Kind");
				}
				++event_count;
			}

			HashSet<long> loadedAssemblies = new HashSet <long> ();
			public override void Visit (AssemblyEvent assembly) {
				if (assembly.Kind == MetadataEventType.Load) {
					if (loadedAssemblies.Contains (assembly.Id))
						Fail (assembly, "Assembly already loaded");
					loadedAssemblies.Add (assembly.Id);
				} else if (assembly.Kind == MetadataEventType.Unload) {
					if (!loadedAssemblies.Contains (assembly.Id))
						Fail (assembly, "Assembly not loaded");
					loadedAssemblies.Remove (assembly.Id);
				} else {
					Fail (assembly, "Invalid event Kind");
				}
				++event_count;
			}

			HashSet<long> loadedDomain = new HashSet <long> ();

			public override void Visit (DomainEvent domain) {
				if (domain.Kind == MetadataEventType.Load) {
					if (loadedDomain.Contains (domain.Id))
						Fail (domain, "Domain already loaded");
					loadedDomain.Add (domain.Id);
				} else if (domain.Kind == MetadataEventType.Unload) {
					if (!loadedDomain.Contains (domain.Id))
						Fail (domain, "Domain not loaded");
					loadedDomain.Remove (domain.Id);
				} else {
					Fail (domain, "Invalid event Kind");
				}
				++event_count;
			}

			public override void Visit (DomainFriendlyNameEvent domainName)
			{
				if (!loadedDomain.Contains (domainName.Id))
					Fail (domainName, string.Format ("Invalid domain id {0}", domainName.Id));
				++event_count;
			}

			HashSet<long> loadedContexts = new HashSet <long> ();

			public override void Visit (ContextEvent context) {
				if (context.Kind == MetadataEventType.Load) {
					if (loadedContexts.Contains (context.Id))
						Fail (context, "Context already loaded");
					loadedContexts.Add (context.Id);
				} else if (context.Kind == MetadataEventType.Unload) {
					if (!loadedContexts.Contains (context.Id))
						Fail (context, "Context not loaded");
					loadedContexts.Remove (context.Id);
				} else {
					Fail (context, "Invalid event Kind");
				}
				++event_count;
			}

			Dictionary<long, CompiledMethodEvent> loadedMethods = new Dictionary<long, CompiledMethodEvent> ();
			public override void Visit (CompiledMethodEvent method) {
				if (loadedMethods.ContainsKey (method.MethodId))
					Fail (method, "method already loaded");
				loadedMethods [method.MethodId] = method;

				++event_count;
			}

			public override void Visit (ThreadEvent thread)
			{
				if (thread.Kind == MetadataEventType.Load) {
					if (runningThreads.Contains (thread.Id))
						Fail (thread, "Thread already running");
					runningThreads.Add (thread.Id);
				} else if (thread.Kind == MetadataEventType.Unload) {
					if (!runningThreads.Contains (thread.Id))
						Fail (thread, "Thread not running");
					runningThreads.Remove (thread.Id);
				} else {
					Fail (thread, "Invalid event Kind");
				}
				++event_count;
			}

			public override void Visit (ThreadNameEvent threadName)
			{
				if (!runningThreads.Contains (threadName.Id))
					Fail (threadName, string.Format ("Invalid thread id {0:X}", threadName.Id));
				++event_count;
			}

			public override void Visit (JitHelperEvent thread)
			{
				++event_count;
			}

			public override void Visit (SampleHitEvent sample)
			{
				if (!runningThreads.Contains (sample.ThreadId))
					Fail (sample, string.Format ("Sample points to invalid thread id {0:X}", sample.ThreadId));

				foreach (var frame in sample.ManagedFrames) {
					if (!loadedMethods.ContainsKey (frame.MethodId)) {
						Fail (sample, string.Format ("Invalid MethodId {0:X}", frame.MethodId));
					} else if ((ulong)frame.NativeOffset > loadedMethods [frame.MethodId].CodeSize) {
						if (ReportInvalidSampleOffset)
							Fail (sample, string.Format ("Invalid native offset {0}, method size is {1}",
								frame.NativeOffset,
								loadedMethods [frame.MethodId].CodeSize));
					}
				}
				++event_count;
			}

			//Heapshot related events
			bool heapshotInProgress = false;

			public override void Visit (HeapshotStartEvent evt)
			{
				if (heapshotInProgress)
					Fail (evt, "Heapshot started while another one is in progress");

				heapshotInProgress = true;
				++event_count;
			}

			public override void Visit (HeapshotEndEvent evt)
			{
				if (!heapshotInProgress)
					Fail (evt, "Heapshot ended while no heapshot in progress");
				heapshotInProgress = false;
				++event_count;
			}

			public override void Visit (HeapshotObjectEvent evt)
			{
				if (!heapshotInProgress)
					Fail ("Heapshot object received while no heapshot in progress");
				if (!loadedTypes.Contains (evt.TypeId))
					Fail (evt, "Allocation for unreported type");

				Allocation alloc;
				if (!allocations.TryGetValue (evt.ObjectId, out alloc))
					Fail (evt, "Object in heapshot not known at this time");

				++event_count;
			}

			//GC related events
			public override void Visit (AllocEvent evt)
			{
				if (!loadedTypes.Contains (evt.TypeId))
					Fail (evt, "Allocation for unreported type");
				VerifyBacktrace (evt, evt.Frames);

				Allocation alloc;
				if (allocations.TryGetValue (evt.ObjectId, out alloc))
					Fail (evt, "Allocation at address already in use");
				alloc = new Allocation (evt.Time, evt.TypeId, evt.ObjectId, evt.Size, evt.Frames);
				allocations [evt.ObjectId] = alloc;

				++event_count;
			}

			HashSet<ulong> gchandles = new HashSet <ulong> ();
			public override void Visit (HandleCreatedEvent handle)
			{
				if (gchandles.Contains (handle.HandleId))
					Fail (handle, string.Format ("Duplicate handle id {0}", handle.HandleId));
				gchandles.Add (handle.HandleId);

				VerifyBacktrace (handle, handle.Frames);
				++event_count;
			}

			public override void Visit (HandleDestroyedEvent evt)
			{
				if (!gchandles.Contains (evt.HandleId))
					Fail (evt, string.Format ("Unknown handle id {0}", evt.HandleId));
				gchandles.Remove (evt.HandleId);

				VerifyBacktrace (evt, evt.Frames);
				++event_count;
			}

			//counters
			Dictionary<int, CounterType> counters = new Dictionary <int, CounterType> ();
			public override void Visit (CountersDescEvent evt)
			{
				foreach (var counter in evt.Counters) {
					if (counters.ContainsKey (counter.Index))
						Fail (evt, string.Format ("Duplicate counter index {0}", counter.Index));
					counters [counter.Index] = counter.Type;
				}

				++event_count;
			}

			public override void Visit (CounterSampleEvent evt)
			{
				foreach (var sample in evt.Samples) {
					if (!counters.ContainsKey (sample.Index))
						Fail (evt, string.Format ("Invalid index {0}", sample.Index));
					else if (sample.Type != counters [sample.Index])
						Fail (evt, string.Format ("Invalid type {0} for sample {0}", sample.Type, sample.Index));
				}

				++event_count;
			}

			public override void Visit (GCMoveEvent evt)
			{
				for (int i = 0; i < evt.MovedObjects.Length; i += 2) {
					Allocation alloc;
					if (allocations.TryGetValue (evt.MovedObjects [i], out alloc)) {
						long fromId = evt.MovedObjects [i];
						long toId = evt.MovedObjects [i + 1];
						alloc.ObjectId = toId;
						allocations.Remove (fromId);
						allocations [toId] = alloc;
					} else {
						Fail (evt, "GC Move event for an allocation we know nothing about");
					}
				}
			}

			//misc events
			public override void Visit (USymEvent evt)
			{
				//XXX we ignore this event
			}

			internal void PrintSummary ()
			{
				Console.WriteLine ("events:{0} ignored:{1} errors: {2}", event_count, ignored_count, errors);
			}

			internal void VerifyEndOfFileState ()
			{
				foreach (var t in runningThreads)
					Fail (string.Format ("Missing thread exit event for tid {0:X}", t));
			}
		}

		public void Verify ()
		{
			this.visitor = new VerificationVisitor ();
			foreach (var buffer in decoder.GetBuffers ()) {
				VerifyBuffer (buffer);
				foreach (var evt in buffer.GetEvents ())
					evt.Visit (visitor);
			}

			visitor.VerifyEndOfFileState ();
		}

		public void PrintSummary ()
		{
			visitor.PrintSummary ();
		}
	}
}
