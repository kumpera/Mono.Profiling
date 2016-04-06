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
			}

			//GC related events
			public override void Visit (AllocEvent evt)
			{
				if (!loadedTypes.Contains (evt.TypeId))
					Fail (evt, "Allocation for unreported type");
				VerifyBacktrace (evt, evt.Frames);
			}

			HashSet<ulong> gchandles = new HashSet <ulong> ();
			public override void Visit (HandleCreatedEvent handle)
			{
				if (gchandles.Contains (handle.HandleId))
					Fail (handle, string.Format ("Duplicate handle id {0}", handle.HandleId));
				gchandles.Add (handle.HandleId);

				VerifyBacktrace (handle, handle.Frames);
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
			}

			public override void Visit (CounterSampleEvent evt)
			{
				foreach (var sample in evt.Samples) {
					if (!counters.ContainsKey (sample.Index))
						Fail (evt, string.Format ("Invalid index {0}", sample.Index));
					else if (sample.Type != counters [sample.Index])
						Fail (evt, string.Format ("Invalid type {0} for sample {0}", sample.Type, sample.Index));
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