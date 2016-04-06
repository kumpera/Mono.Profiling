using System;
using System.IO;

using NUnit.Framework;


namespace Mono.Profiling
{
	[TestFixture]
	public class DecoderTest {
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

		void VerifyHeader (Header header)
		{
			Assert.AreEqual (Header.MAJOR, header.Major);
			Assert.AreEqual (Header.MINOR, header.Minor);
			Assert.AreEqual (Header.LOG_DATA_VERSION, header.Format);
			Assert.IsTrue (header.PointerSize == 4 || header.PointerSize == 8);
			Assert.AreNotEqual (0, header.StartTime);
			Assert.AreNotEqual (0, header.TimerOverhead);
			Assert.AreEqual (0, header.Flags);
			Assert.AreNotEqual (0, header.Pid);
			Assert.AreEqual (0, header.Port); //XXX this will fail when we test profiling with the command interface
			Assert.AreEqual (0, header.SysId);
		}

		[Test]
		public void CanDecodeTrivial () {
			var args = new ProfilerArgs ();
			args.OutputFile = TempFile;
			args.OverrideOutput = true;
			args.Binary = "test-programs/trivial.exe";

			var profiler = args.Start ();
			profiler.Wait ();
			Assert.AreEqual (0, profiler.Result);

			//Ok, now let's try to decode this puppy.
			var decoder = new Decoder (TempFile);
			//header was decoded, lets check it.
			VerifyHeader (decoder.Header);


			int i = 0;
			foreach (var buffer in decoder.GetBuffers ()) {
				++i;
			}
			Assert.IsTrue (i > 0);
		}
	}
}
