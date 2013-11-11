﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Performance.Comparison.SQLCE;
using Performance.Comparison.SQLite;
using Performance.Comparison.SQLServer;
using Performance.Comparison.Voron;
using Voron.Impl;

namespace Performance.Comparison
{
    using System.Security.Policy;

    using Performance.Comparison.Esent;
    using Performance.Comparison.LMDB;

    class Program
    {
        static void Main(string[] args)
        {
            var random = new Random();
            var buffer = new byte[87 * 1024];
            random.NextBytes(buffer);

            var path = @"D:\temp\";

            var sequentialIds = InitSequentialNumbers(Constants.WriteTransactions * Constants.ItemsPerTransaction, minValueSize: 128, maxValueSize: 128);
            var randomIds = InitRandomNumbers(Constants.WriteTransactions * Constants.ItemsPerTransaction, minValueSize: 128, maxValueSize: 128);

            var sequentialIdsLarge = InitSequentialNumbers(Constants.WriteTransactions * Constants.ItemsPerTransaction, minValueSize: 512, maxValueSize: 87 * 1024);
            var randomIdsLarge = InitRandomNumbers(Constants.WriteTransactions * Constants.ItemsPerTransaction, minValueSize: 512, maxValueSize: 87 * 1024);

        
            var performanceTests = new List<IStoragePerformanceTest>()
				{
					new SqlServerTest(buffer),
					new SqlLiteTest(path, buffer),
					new SqlCeTest(path, buffer),
                    new LmdbTest(path, buffer),
                    new EsentTest(path, buffer),
                    new VoronTest(path, FlushMode.Full, buffer)
				};

            var perfTracker = new PerfTracker();
            for(var i = 0; i < performanceTests.Count; i++)
            {
                var test = performanceTests[i];

                Console.WriteLine("Testing: " + test.StorageName);

                var writeSeq = test.WriteSequential(sequentialIds, perfTracker);
                var items = writeSeq.Sum(x => x.ProcessedItems);
                double totalDuration = writeSeq.Sum(x => x.Duration);
                //WritePerfData("WriteSeq", test, writeSeq);
                OutputResults("Write Seq", items, totalDuration, perfTracker);

                var readSeq = test.ReadSequential(perfTracker);
                items = readSeq.ProcessedItems;
                totalDuration = readSeq.Duration;
                //WritePerfData("ReadSeq", test, readSeq);
                OutputResults("Read Seq", items, totalDuration, perfTracker);

                var writeRandom = test.WriteRandom(randomIds, perfTracker);
                items = writeRandom.Sum(x => x.ProcessedItems);
                totalDuration = writeRandom.Sum(x => x.Duration);
                OutputResults("Write rnd", items, totalDuration, perfTracker);

                var readRandom = test.ReadRandom(randomIds.Select(x => x.Id), perfTracker);
                items = readRandom.ProcessedItems;
                totalDuration = readRandom.Duration;
                OutputResults("Read rnd", items, totalDuration, perfTracker);

                writeSeq = test.WriteSequential(sequentialIdsLarge, perfTracker);
                items = writeSeq.Sum(x => x.ProcessedItems);
                totalDuration = writeSeq.Sum(x => x.Duration);
                //WritePerfData("WriteSeq", test, writeSeq);
                OutputResults("Write lrg seq", items, totalDuration, perfTracker);

                readSeq = test.ReadSequential(perfTracker);
                items = readSeq.ProcessedItems;
                totalDuration = readSeq.Duration;
                //WritePerfData("ReadSeq", test, readSeq);
                OutputResults("Read lrg seq", items, totalDuration, perfTracker);

                writeRandom = test.WriteRandom(randomIdsLarge, perfTracker);
                items = writeRandom.Sum(x => x.ProcessedItems);
                totalDuration = writeRandom.Sum(x => x.Duration);
                OutputResults("Write lrg rnd", items, totalDuration, perfTracker);

                readRandom = test.ReadRandom(randomIdsLarge.Select(x => x.Id), perfTracker);
                items = readRandom.ProcessedItems;
                totalDuration = readRandom.Duration;
                OutputResults("Read lrg rnd", items, totalDuration, perfTracker);
            }
        }

       
        private static void OutputResults(string name, long itemsCount, double duration, PerfTracker perfTracker)
        {
            Console.WriteLine("{0}:\t\t{1,10:#,#;;0} items in {2,10:#,#;;0} sec, {3,10:#,#} ops/s. Mem: {3:0} MB", name, itemsCount / 1000, duration, itemsCount / (duration / 1000));
        }

        private static void WritePerfData(string name, IStoragePerformanceTest test, List<PerformanceRecord> writeSeq)
        {
            using (var file = File.Open(name + "_" + test.GetType().Name + ".csv", FileMode.Create))
            using (var writer = new StreamWriter(file))
            {
                foreach (var p in writeSeq)
                {
                    writer.WriteLine("{0},{1},{2}", p.ProcessedItems, p.Time, p.Duration);
                }
            }
        }

        private static HashSet<TestData> InitRandomNumbers(int count, int minValueSize, int maxValueSize)
        {
            var random = new Random(1337 ^ 13);
            var randomNumbers = new HashSet<int>();

            while (randomNumbers.Count < count)
            {
                randomNumbers.Add(random.Next(0, int.MaxValue));
            }

            return InitValue(randomNumbers, minValueSize, maxValueSize);
        }

        private static HashSet<TestData> InitSequentialNumbers(int count, int minValueSize, int maxValueSize)
        {
            return InitValue(Enumerable.Range(0, count), minValueSize, maxValueSize);
        }

        private static HashSet<TestData> InitValue(IEnumerable<int> ids, int minValueSize, int maxValueSize)
        {
            var data = new HashSet<TestData>();

            var random = new Random(1337 ^ 13);

            foreach (var id in ids)
            {
                data.Add(new TestData { Id = id, ValueSize = random.Next(minValueSize, maxValueSize) });
            }

            return data;
        }
    }
}
