using System;
using System.IO;

using NUnit.Framework;

namespace Mono.Profiling
{
	[TestFixture]
	public class ProfilerTest {
		string tempProfilerFile;

		string TempFile {
			get {
				if (tempProfilerFile == null)
					tempProfilerFile = Path.GetTempFileName () + ".mlpd";
				return tempProfilerFile;
			}
		}

		[TearDown]
		public void TearDown ()
		{
			if (tempProfilerFile != null && File.Exists (tempProfilerFile)) {
				File.Delete (TempFile);
				tempProfilerFile = null;
			}
		}


		[Test]
		public void CanProfileTrivial () {
			var args = new ProfilerArgs ();
			args.OutputFile = TempFile;
			args.OverrideOutput = true;
			args.Binary = "test-programs/trivial.exe";

			Assert.IsFalse (File.Exists (args.OutputFile));
			var profiler = args.Start ();
			profiler.Wait ();
			Assert.AreEqual (0, profiler.Result);
			Assert.IsTrue (File.Exists (args.OutputFile));
			Assert.IsTrue (new FileInfo (TempFile).Length > 0);
		}
	}
	
	[TestFixture]
	public class ProfilerArgsTest {
		[Test]
		public void SetOutputFile ()
		{
			var args = new ProfilerArgs ();
			args.OutputFile = "foo.mlpd";
			Assert.AreEqual ("--profile=log:output=foo.mlpd", args.GetProfilerArgument ());

			args.OverrideOutput = true;
			Assert.AreEqual ("--profile=log:output=-foo.mlpd", args.GetProfilerArgument ());
		}

		[Test]
		public void DefaultProfilerArgs ()
		{
			var args = new ProfilerArgs ();
			Assert.AreEqual ("--profile=log", args.GetProfilerArgument ());
		}

	}
}
