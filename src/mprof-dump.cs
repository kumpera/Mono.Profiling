using System;
using Mono.Profiling;
using Mono.Options;

class Driver {
	static void DumpHeader (Header header) {
		Console.WriteLine ("Version {0}.{1} format {2}", header.Major, header.Minor, header.Format);
		Console.WriteLine ("Pointer Size {0}", header.PointerSize);
		Console.WriteLine ("Start Time {0} Pointer Overhead {1}", header.StartTime, header.TimerOverhead);
		Console.WriteLine ("Flags {0:X}", header.Flags);
		Console.WriteLine ("Pid {0} Port {1} SysId {2}", header.Pid, header.Port, header.SysId);

	}

	static void DumpBuffer (EventBuffer buffer) {
		Console.WriteLine ("Buffer len {0}", buffer.Data.Length);
		Console.WriteLine ("Base Variables:");
		Console.WriteLine ("\tTime:     {0:X}", buffer.TimeBase);
		Console.WriteLine ("\tPointer:  {0:X}", buffer.PointerBase);
		Console.WriteLine ("\tObject:   {0:X}", buffer.ObjectBase);
		Console.WriteLine ("\tThreadId: {0:X}", buffer.ThreadId);
		Console.WriteLine ("\tMethod:   {0:X}", buffer.MethodBase);
		Console.WriteLine ("----events");
		foreach (var evt in buffer.GetEvents ())
			Console.WriteLine (evt);
	}



	static void Main (string[] args) {
		bool dump_file = false;
		bool lint_file = false;

		var opts = new OptionSet () {
			{ "dump", v => dump_file = v != null },
			{ "lint", v => lint_file = v != null }
		};

		var files = opts.Parse (args);

		if (files.Count == 0) {
			Console.WriteLine ("pass at least one file");
			return;
		}
		if (!dump_file && !lint_file)
			lint_file = true;

		foreach (var f in files) {
			Console.WriteLine ("File: {0}", f);
			var decoder = new Decoder (f);

			if (dump_file) {
				Console.WriteLine ("========HEADER");
				DumpHeader (decoder.Header);
				Console.WriteLine ("========BUFFERS");
				foreach (var buffer in decoder.GetBuffers ())
					DumpBuffer (buffer);
			}

			decoder.Reset ();
			if (lint_file) {
				var l = new MprofLinter (decoder);
				l.Verify ();
				l.PrintSummary ();
			}
		}
	}
}
