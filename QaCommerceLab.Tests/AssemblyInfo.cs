using NUnit.Framework;

// Feature files run in parallel; scenarios inside one feature run one after another.
// Features tagged @serial (they change global lab settings) run on their own.
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(4)]
