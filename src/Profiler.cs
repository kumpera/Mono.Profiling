using System;
using System.Diagnostics;

namespace Mono.Profiling
{
	public class Profiler {
		ProfilerArgs args;
		Process proc;

		internal Profiler (ProfilerArgs args) {
			this.args = args;
		}

		internal void Start ()
		{
			var pi = new ProcessStartInfo ("mono");
			pi.Arguments = String.Format ("{0} {1}", args.GetProfilerArgument(), args.Binary);
			pi.UseShellExecute = false;

			this.proc = Process.Start (pi);
		}

		public void Wait ()
		{
			this.proc.WaitForExit ();
		}

		public int Result
		{
			get { return this.proc.ExitCode; }
		}
	}

	public class ProfilerArgs {
		public string OutputFile { get; set; }
		public bool OverrideOutput { get; set; }
		public String Binary { get; set; }

		string AddArg (string cur_args, string new_arg)
		{
			if (cur_args == "")
				cur_args = "--profile=log:";
			else
				cur_args += ",";
			return cur_args += new_arg;
		}

		public string GetProfilerArgument ()
		{
			String res = "";

			if (OutputFile != null)
				res = AddArg (res, "output=" + (OverrideOutput ? "-" + OutputFile : OutputFile));

			if (res == "")
				res = "--profile=log";
			return res;
		}

		public Profiler Start ()
		{
			var p = new Profiler (this);
			p.Start ();
			return p;
		}

		/*
		printf ("\t[no]alloc            enable/disable recording allocation info\n");
		printf ("\t[no]calls            enable/disable recording enter/leave method events\n");
		printf ("\theapshot[=MODE]      record heap shot info (by default at each major collection)\n");
		printf ("\t                     MODE: every XXms milliseconds, every YYgc collections, ondemand\n");
		printf ("\tcounters             sample counters every 1s\n");
		printf ("\tsample[=TYPE]        use statistical sampling mode (by default cycles/1000)\n");
		printf ("\t                     TYPE: cycles,instr,cacherefs,cachemiss,branches,branchmiss\n");
		printf ("\t                     TYPE can be followed by /FREQUENCY\n");
		printf ("\ttime=fast            use a faster (but more inaccurate) timer\n");
		printf ("\tmaxframes=NUM        collect up to NUM stack frames\n");
		printf ("\tcalldepth=NUM        ignore method events for call chain depth bigger than NUM\n");
		printf ("\toutput=FILENAME      write the data to file FILENAME (-FILENAME to overwrite)\n");
		printf ("\toutput=|PROGRAM      write the data to the stdin of PROGRAM\n");
		printf ("\t                     %%t is subtituted with date and time, %%p with the pid\n");
		printf ("\treport               create a report instead of writing the raw data to a file\n");
		printf ("\tzip                  compress the output data\n");
		printf ("\tport=PORTNUM         use PORTNUM for the listening command server\n");
		printf ("\tcoverage             enable collection of code coverage data\n");
		printf ("\tcovfilter=ASSEMBLY   add an assembly to the code coverage filters\n");
		printf ("\t                     add a + to include the assembly or a - to exclude it\n");
		printf ("\t                     filter=-mscorlib\n");
		printf ("\tcovfilter-file=FILE  use FILE to generate the list of assemblies to be filtered\n");*/
	}

}
