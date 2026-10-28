using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Gley.CameraSystem.Dev
{
    public class BenchmarkReportWriter
    {
        private const double FrameBudgetMilliseconds = 0.1d;
        private const double FrameSpikeLimitMilliseconds = 0.5d;
        private const double OperationBudgetMilliseconds = 1d;
        private const double NanosecondsPerMillisecond = 1000000d;
        private const string FolderName = "VehicleCameraBenchmark";
        private const string LatestName = "Latest";

        private readonly List<string> phaseViews = new List<string>();
        private readonly StringBuilder builder = new StringBuilder();
        private readonly BenchmarkStatistics statistics = new BenchmarkStatistics();
        private readonly CultureInfo culture = CultureInfo.InvariantCulture;

        public string WriteFiles(BenchmarkScript script, BenchmarkFrameLog log, BenchmarkMarkerSampler markers, IReadOnlyList<BenchmarkOperation> operations, BenchmarkGarbageCounter garbageCounter)
        {
            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", FolderName);
            Directory.CreateDirectory(folder);
            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss", culture);
            string report = BuildReport(script, log, markers, operations, garbageCounter);
            string frames = BuildFrameTable(log, markers);
            string reportPath = Path.Combine(folder, $"Benchmark_{stamp}.txt");
            File.WriteAllText(reportPath, report);
            File.WriteAllText(Path.Combine(folder, $"Benchmark_{stamp}_frames.csv"), frames);
            File.WriteAllText(Path.Combine(folder, LatestName + ".txt"), report);
            File.WriteAllText(Path.Combine(folder, LatestName + "_frames.csv"), frames);
            return reportPath;
        }

        public string WriteOperationProfile(BenchmarkOperationProfile profile, BenchmarkMarkerSampler markers)
        {
            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", FolderName);
            Directory.CreateDirectory(folder);
            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss", culture);
            string report = BuildOperationProfileReport(profile, markers);
            string reportPath = Path.Combine(folder, $"Benchmark_{stamp}_operations.txt");
            File.WriteAllText(reportPath, report);
            File.WriteAllText(Path.Combine(folder, LatestName + "_operations.txt"), report);
            return reportPath;
        }

        private string BuildReport(BenchmarkScript script, BenchmarkFrameLog log, BenchmarkMarkerSampler markers, IReadOnlyList<BenchmarkOperation> operations, BenchmarkGarbageCounter garbageCounter)
        {
            builder.Length = 0;
            AppendHeader(script, markers, garbageCounter);
            AppendFrameTable(script, log);
            AppendMarkerTable(script, log, markers);
            AppendOperations(operations);
            AppendSummary(log, operations);
            return builder.ToString();
        }

        private void AppendHeader(BenchmarkScript script, BenchmarkMarkerSampler markers, BenchmarkGarbageCounter garbageCounter)
        {
            builder.AppendLine("Vehicle Camera System performance benchmark");
            builder.AppendLine($"Date: {System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", culture)}");
            builder.AppendLine($"Unity: {Application.unityVersion}   Platform: {Application.platform}   Editor: {Application.isEditor}   Development build: {Debug.isDebugBuild}   Code optimization: {GetCodeOptimization()}");
            builder.AppendLine($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} threads)   RAM: {SystemInfo.systemMemorySize} MB   GPU: {SystemInfo.graphicsDeviceName}");
            builder.AppendLine($"Frame step: 1/{script.FrameRate} s (fixed, independent of real time)   Frames: {script.TotalFrames}   Phases: {script.PhaseCount}");
            builder.AppendLine($"GC counter: {DescribeGarbageCounter(garbageCounter)}   Profiler markers: {DescribeMarkers(markers)}");
            builder.AppendLine($"Budgets (design section 11): UpdateCameraFrame P95 <= {Format(FrameBudgetMilliseconds)} ms and max <= {Format(FrameSpikeLimitMilliseconds)} ms per frame, 0 GC allocations per frame, one-off operations <= {Format(OperationBudgetMilliseconds)} ms. Judged on an IL2CPP development build.");
            builder.AppendLine("Per-frame times are Stopwatch measurements around UpdateCameraFrame; with Mono the first use of a code path includes JIT compilation.");
            builder.AppendLine("Every collider's C# wrapper and Transform wrapper is created before the run, so GC allocations count only the camera's own garbage.");
            builder.AppendLine();
        }

        private string GetCodeOptimization()
        {
#if UNITY_EDITOR
            return UnityEditor.Compilation.CompilationPipeline.codeOptimization.ToString();
#else
            return "Player";
#endif
        }

        private string DescribeGarbageCounter(BenchmarkGarbageCounter garbageCounter)
        {
            if (!garbageCounter.IsAvailable)
            {
                return "NOT AVAILABLE (allocation counts below are not valid)";
            }

            if (garbageCounter.IsCumulative)
            {
                return "available (cumulative mode)";
            }

            return "available (reset mode)";
        }

        private string DescribeMarkers(BenchmarkMarkerSampler markers)
        {
            int valid = 0;
            for (int index = 0; index < markers.MarkerCount; index++)
            {
                if (markers.IsValid(index))
                {
                    valid++;
                }
            }

            return $"{valid} of {markers.MarkerCount} recorders valid";
        }

        private string Format(double value)
        {
            return value.ToString("F4", culture);
        }

        private void AppendFrameTable(BenchmarkScript script, BenchmarkFrameLog log)
        {
            builder.AppendLine("== Per-frame camera update by phase (active frames) ==");
            builder.AppendLine("Phase | Frames | Views | Bodies | Median ms | P95 ms | Max ms | Frames over budget | GC allocs | Frames with GC | Max casts / limit");
            for (int phase = 0; phase < script.PhaseCount; phase++)
            {
                int start = script.GetStartFrame(phase);
                AppendFrameRow(((BenchmarkPhaseKind)phase).ToString(), log, start, start + script.GetFrameCount(phase));
            }

            AppendFrameRow("All phases", log, 0, log.FrameCount);
            builder.AppendLine();
        }

        private void AppendFrameRow(string label, BenchmarkFrameLog log, int start, int end)
        {
            statistics.Clear();
            phaseViews.Clear();
            int overBudget = 0;
            int allocationTotal = 0;
            int framesWithAllocations = 0;
            int maximumCasts = 0;
            int castLimit = 0;
            int minimumBodies = int.MaxValue;
            int maximumBodies = 0;
            for (int frame = start; frame < end; frame++)
            {
                if (!log.IsActive(frame))
                {
                    continue;
                }

                double milliseconds = log.GetUpdateMilliseconds(frame);
                statistics.Add(milliseconds);
                if (milliseconds > FrameBudgetMilliseconds)
                {
                    overBudget++;
                }

                int allocations = log.GetAllocations(frame);
                allocationTotal += allocations;
                if (allocations > 0)
                {
                    framesWithAllocations++;
                }

                maximumCasts = Mathf.Max(maximumCasts, log.GetCasts(frame));
                castLimit = Mathf.Max(castLimit, log.GetCastLimit(frame));
                minimumBodies = Mathf.Min(minimumBodies, log.GetBodyCount(frame));
                maximumBodies = Mathf.Max(maximumBodies, log.GetBodyCount(frame));
                AddView(log.GetViewName(frame));
            }

            statistics.Compute();
            if (statistics.Count == 0)
            {
                builder.AppendLine($"{label} | 0 | - | - | - | - | - | - | - | - | -");
                return;
            }

            builder.AppendLine($"{label} | {statistics.Count} | {JoinViews()} | {DescribeRange(minimumBodies, maximumBodies)} | {Format(statistics.Median)} | {Format(statistics.Percentile95)} | {Format(statistics.Maximum)} | {overBudget} | {allocationTotal} | {framesWithAllocations} | {maximumCasts} / {castLimit}");
        }

        private void AddView(string viewName)
        {
            if (string.IsNullOrEmpty(viewName) || phaseViews.Contains(viewName))
            {
                return;
            }

            phaseViews.Add(viewName);
        }

        private string JoinViews()
        {
            if (phaseViews.Count == 0)
            {
                return "-";
            }

            return string.Join(", ", phaseViews);
        }

        private string DescribeRange(int minimum, int maximum)
        {
            if (minimum == maximum)
            {
                return minimum.ToString(culture);
            }

            return $"{minimum}-{maximum}";
        }

        private void AppendMarkerTable(BenchmarkScript script, BenchmarkFrameLog log, BenchmarkMarkerSampler markers)
        {
            builder.AppendLine("== Profiler markers per frame by phase (ms, frames where the marker ran) ==");
            builder.AppendLine("Phase | Marker | Frames | Median ms | P95 ms | Max ms");
            for (int phase = 0; phase < script.PhaseCount; phase++)
            {
                int start = script.GetStartFrame(phase);
                int end = start + script.GetFrameCount(phase);
                for (int marker = 0; marker < markers.MarkerCount; marker++)
                {
                    AppendMarkerRow(((BenchmarkPhaseKind)phase).ToString(), markers.GetName(marker), log, marker, start, end);
                }
            }

            for (int marker = 0; marker < markers.MarkerCount; marker++)
            {
                AppendMarkerRow("All phases", markers.GetName(marker), log, marker, 0, log.FrameCount);
            }

            builder.AppendLine();
        }

        private void AppendMarkerRow(string label, string markerName, BenchmarkFrameLog log, int marker, int start, int end)
        {
            statistics.Clear();
            for (int frame = start; frame < end; frame++)
            {
                long nanoseconds = log.GetMarkerNanoseconds(marker, frame);
                if (nanoseconds > 0)
                {
                    statistics.Add(nanoseconds / NanosecondsPerMillisecond);
                }
            }

            if (statistics.Count == 0)
            {
                return;
            }

            statistics.Compute();
            builder.AppendLine($"{label} | {markerName} | {statistics.Count} | {Format(statistics.Median)} | {Format(statistics.Percentile95)} | {Format(statistics.Maximum)}");
        }

        private void AppendOperations(IReadOnlyList<BenchmarkOperation> operations)
        {
            builder.AppendLine("== One-off operations (Stopwatch around the API call) ==");
            builder.AppendLine("Frame | Phase | Operation | Result | ms | GC allocs | Within budget");
            for (int index = 0; index < operations.Count; index++)
            {
                BenchmarkOperation operation = operations[index];
                builder.AppendLine($"{operation.Frame} | {DescribePhase(operation.Phase)} | {operation.Name} | {operation.Result} | {Format(operation.Milliseconds)} | {operation.Allocations} | {DescribeBudget(operation.Milliseconds <= OperationBudgetMilliseconds)}");
            }

            builder.AppendLine();
        }

        private string DescribePhase(int phase)
        {
            if (phase < 0)
            {
                return "Start";
            }

            return ((BenchmarkPhaseKind)phase).ToString();
        }

        private string DescribeBudget(bool isWithinBudget)
        {
            if (isWithinBudget)
            {
                return "PASS";
            }

            return "FAIL";
        }

        private void AppendSummary(BenchmarkFrameLog log, IReadOnlyList<BenchmarkOperation> operations)
        {
            statistics.Clear();
            int allocationTotal = 0;
            int castViolations = 0;
            int maximumFrame = -1;
            double maximumMilliseconds = 0d;
            for (int frame = 0; frame < log.FrameCount; frame++)
            {
                if (!log.IsActive(frame))
                {
                    continue;
                }

                double milliseconds = log.GetUpdateMilliseconds(frame);
                statistics.Add(milliseconds);
                allocationTotal += log.GetAllocations(frame);
                if (log.GetCasts(frame) > log.GetCastLimit(frame))
                {
                    castViolations++;
                }

                if (milliseconds > maximumMilliseconds)
                {
                    maximumMilliseconds = milliseconds;
                    maximumFrame = frame;
                }
            }

            statistics.Compute();
            int operationsOverBudget = 0;
            int worstOperation = -1;
            for (int index = 0; index < operations.Count; index++)
            {
                if (operations[index].Milliseconds > OperationBudgetMilliseconds)
                {
                    operationsOverBudget++;
                }

                if (worstOperation < 0 || operations[index].Milliseconds > operations[worstOperation].Milliseconds)
                {
                    worstOperation = index;
                }
            }

            builder.AppendLine("== Budget summary ==");
            builder.AppendLine($"Per-frame UpdateCameraFrame: median {Format(statistics.Median)} ms, P95 {Format(statistics.Percentile95)} ms, max {Format(maximumMilliseconds)} ms at frame {maximumFrame} -> P95 {DescribeBudget(statistics.Percentile95 <= FrameBudgetMilliseconds)}, max {DescribeBudget(maximumMilliseconds <= FrameSpikeLimitMilliseconds)}");
            builder.AppendLine($"GC allocations in camera updates: {allocationTotal} -> {DescribeBudget(allocationTotal == 0)}");
            builder.AppendLine($"Frames with more casts than the preset maximum: {castViolations} -> {DescribeBudget(castViolations == 0)}");
            if (worstOperation >= 0)
            {
                BenchmarkOperation worst = operations[worstOperation];
                builder.AppendLine($"One-off operations over {Format(OperationBudgetMilliseconds)} ms: {operationsOverBudget} of {operations.Count}; worst: {worst.Name} at frame {worst.Frame}, {Format(worst.Milliseconds)} ms -> {DescribeBudget(operationsOverBudget == 0)}");
            }
        }

        private string BuildFrameTable(BenchmarkFrameLog log, BenchmarkMarkerSampler markers)
        {
            builder.Length = 0;
            builder.Append("frame,phase,active,view,bodies,update_ms,gc_allocs,casts,cast_limit");
            for (int marker = 0; marker < markers.MarkerCount; marker++)
            {
                builder.Append(',');
                builder.Append(markers.GetName(marker));
                builder.Append("_ms");
            }

            builder.AppendLine();
            for (int frame = 0; frame < log.FrameCount; frame++)
            {
                builder.Append($"{frame},{(BenchmarkPhaseKind)log.GetPhase(frame)},{log.IsActive(frame)},{log.GetViewName(frame)},{log.GetBodyCount(frame)},{Format(log.GetUpdateMilliseconds(frame))},{log.GetAllocations(frame)},{log.GetCasts(frame)},{log.GetCastLimit(frame)}");
                for (int marker = 0; marker < markers.MarkerCount; marker++)
                {
                    builder.Append(',');
                    long nanoseconds = log.GetMarkerNanoseconds(marker, frame);
                    if (nanoseconds >= 0)
                    {
                        builder.Append(Format(nanoseconds / NanosecondsPerMillisecond));
                    }
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private string BuildOperationProfileReport(BenchmarkOperationProfile profile, BenchmarkMarkerSampler markers)
        {
            builder.Length = 0;
            builder.AppendLine("Vehicle Camera System one-off operation profile");
            builder.AppendLine($"Date: {System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", culture)}");
            builder.AppendLine($"Unity: {Application.unityVersion}   Platform: {Application.platform}   Editor: {Application.isEditor}   Development build: {Debug.isDebugBuild}   Code optimization: {GetCodeOptimization()}");
            builder.AppendLine($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} threads)");
            builder.AppendLine($"Each operation runs once per frame, {profile.RunCapacity} times, on the benchmark truck with four trailers (five bodies) unless named otherwise. Budget: {Format(OperationBudgetMilliseconds)} ms per operation.");
            builder.AppendLine();
            builder.AppendLine("== Operations (Stopwatch around the API call) ==");
            builder.AppendLine("Operation | Runs | Rejected | Median ms | P95 ms | Max ms | Median GC allocs | Median within budget");
            for (int operation = 0; operation < profile.OperationCount; operation++)
            {
                AppendProfiledOperationRow(profile, operation);
            }

            builder.AppendLine();
            builder.AppendLine("== Stages per operation (profiler markers, ms, runs where the marker ran) ==");
            builder.AppendLine("Operation | Marker | Runs | Median ms | P95 ms | Max ms");
            for (int operation = 0; operation < profile.OperationCount; operation++)
            {
                string operationName = ((BenchmarkProfiledOperation)operation).ToString();
                for (int marker = 0; marker < markers.MarkerCount; marker++)
                {
                    AppendProfiledMarkerRow(profile, operation, operationName, markers.GetName(marker), marker);
                }
            }

            return builder.ToString();
        }

        private void AppendProfiledOperationRow(BenchmarkOperationProfile profile, int operation)
        {
            int runs = profile.GetRunCount(operation);
            statistics.Clear();
            for (int run = 0; run < runs; run++)
            {
                statistics.Add(profile.GetAllocations(operation, run));
            }

            statistics.Compute();
            double allocationMedian = statistics.Median;
            statistics.Clear();
            for (int run = 0; run < runs; run++)
            {
                statistics.Add(profile.GetMilliseconds(operation, run));
            }

            statistics.Compute();
            builder.AppendLine($"{(BenchmarkProfiledOperation)operation} | {runs} | {profile.GetRejectedCount(operation)} | {Format(statistics.Median)} | {Format(statistics.Percentile95)} | {Format(statistics.Maximum)} | {allocationMedian.ToString("0", culture)} | {DescribeBudget(runs > 0 && statistics.Median <= OperationBudgetMilliseconds)}");
        }

        private void AppendProfiledMarkerRow(BenchmarkOperationProfile profile, int operation, string operationName, string markerName, int marker)
        {
            statistics.Clear();
            int runs = profile.GetRunCount(operation);
            for (int run = 0; run < runs; run++)
            {
                double value = profile.GetMarkerMilliseconds(operation, run, marker);
                if (value > 0d)
                {
                    statistics.Add(value);
                }
            }

            if (statistics.Count == 0)
            {
                return;
            }

            statistics.Compute();
            builder.AppendLine($"{operationName} | {markerName} | {statistics.Count} | {Format(statistics.Median)} | {Format(statistics.Percentile95)} | {Format(statistics.Maximum)}");
        }
    }
}
