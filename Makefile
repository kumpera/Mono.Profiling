SOURCES = src/Event.cs	\
	src/Decoder.cs	\
	src/Profiler.cs	\
	src/MprofLinter.cs	\
	src/EventVistor.cs	\
	src/Options.cs

MPROF_DUMP_SOURCES = src/mprof-dump.cs

TST_SOURCES = tests/DecoderTest.cs	\
	tests/ProfilerTest.cs

#XXX turn this into a generic rule
TST_APPS = test-programs/trivial.exe

all:: Mono.Profiling.dll
all:: mprof-dump.exe

test-programs/trivial.exe: test-programs/trivial.cs
	mcs -out:$@ -debug $<

Mono.Profiling.dll: $(SOURCES)
	mcs -out:$@ -debug -target:library $(SOURCES)

MonoProfiling_Test.dll: Mono.Profiling.dll $(TST_SOURCES) $(TST_APPS)
	mcs -out:$@ -debug -target:library -r:Mono.Profiling.dll -r:nunit.framework.dll $(TST_SOURCES)

check: MonoProfiling_Test.dll
	nunit-console MonoProfiling_Test.dll

mprof-dump.exe: $(MPROF_DUMP_SOURCES) Mono.Profiling.dll
	mcs -out:$@ -debug -r:Mono.Profiling.dll $(MPROF_DUMP_SOURCES)

