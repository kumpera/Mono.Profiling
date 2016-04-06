using System;
using System.Collections.Generic;
using System.IO;

namespace Mono.Profiling
{
	public abstract class EventVisitor {
		// public abstract void Visit (Event evt);

		public virtual void Visit (MethodSteppingEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (CompiledMethodEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (AllocEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (JitHelperEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (DomainEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (DomainFriendlyNameEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (ThreadEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (ThreadNameEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (ImageEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (AssemblyEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (ClassEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (ContextEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (GCPhaseEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (GCMoveEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (RootsEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (HandleCreatedEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (CountersDescEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (CounterSampleEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (SampleHitEvent evt) {
			this.VisitDefault (evt);
		}

		public virtual void Visit (USymEvent evt) {
			this.VisitDefault (evt);
		}

		public abstract void VisitDefault (Event evt);
		
	}
}