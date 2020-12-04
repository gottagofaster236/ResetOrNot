using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ResetOrNot.UI.Components
{
    class ResetOrNotCalculator
    {
        private List<TimeSpan?>[] segments;
        private TimeSpan[] minSplitTimes;
        private TimeSpan[] maxSplitTimes;
        private TimeSpan[] resetTimes;  // worst acceptable time (from the beginning of the run) at the end of each split
        private TimeSpan PB;

        private static readonly TimeSpan infiniteTimeSpan = TimeSpan.MaxValue - TimeSpan.FromSeconds(1);
        public bool HasDoneCalculatingBefore { get; set; } = false;
        private bool isRecalculating = false;
        private string category = null;

        private LiveSplitState state;
        private ResetOrNotSettings settings;
        

        public ResetOrNotCalculator(LiveSplitState state, ResetOrNotSettings settings)
        {
            this.state = state;
            this.settings = settings;
            #if DEBUG
            AllocConsole();
            #endif
        }

        // answer to the question if we should reset
        public class ResetResult
        {
            public ResetAction ResetAction { get; set; }
            public TimeSpan TimeBeforeReset { get; set; }

            public static implicit operator ResetResult(ResetAction resetAction) => new ResetResult
            {
                ResetAction = resetAction
            };
            
        }

        public enum ResetAction
        {
            CALCULATING,
            RUN_NOT_STARTED,
            CONTINUE_RUN,
            RESET,
            NOT_APPLICABLE  // e.g. missing splits information
        }

        public ResetResult ShouldReset()
        {
            if (resetTimes == null)
            {
                if (isRecalculating)
                    return ResetAction.CALCULATING;
                else  // we finished our calculation but did not get the reset times - something went wrong
                    return ResetAction.NOT_APPLICABLE;
            }
            if (state.CurrentSplitIndex == -1)
                return ResetAction.RUN_NOT_STARTED;

            TimeSpan currentTime = state.CurrentTime[state.CurrentTimingMethod].Value;
            TimeSpan timeBeforeReset = resetTimes[state.CurrentSplitIndex] - currentTime;
            if (timeBeforeReset > TimeSpan.Zero) {
                return new ResetResult
                {
                    ResetAction = ResetAction.CONTINUE_RUN, 
                    TimeBeforeReset = timeBeforeReset 
                };
            }
            else {
                return ResetAction.RESET;
            }
        }

        public void CalculateResetTimes()
        {
            Task.Run(() => {
                try
                {
                    CalculateResetTimesSync();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Reset times calculation failed! {e}\nStack trace:\n" + e.StackTrace);
                }
            });
        }

        private void CalculateResetTimesSync()
        {
            lock (this)
            {
                isRecalculating = true;
                HasDoneCalculatingBefore = true;

                if (!settings.SettingsLoaded)  // we have to wait before the settings get loaded
                {
                    isRecalculating = false;
                    return;
                }

                segments = GetSegmentTimes();
                if (segments == null)
                {  
                    // Data for some of the splits is missing
                    resetTimes = null;
                    isRecalculating = false;
                    return;
                }
                CalculateMinMaxSplitTimes();

                // Get the current Personal Best, if it exists
                PB = state.Run.Last().PersonalBestSplitTime[state.CurrentTimingMethod].Value;

                if (PB == TimeSpan.Zero)
                {
                    // No personal best, so any run will PB. Don't reset!
                    resetTimes = Enumerable.Repeat(infiniteTimeSpan, segments.Length).ToArray();
                    isRecalculating = false;
                    return;
                }
                else if (PB < minSplitTimes.Last())
                {
                    // we can't PB
                    resetTimes = null;
                    return;
                }

                string categoryWhenCalculationStarted = category;

                // finding the reset times configuration with the best (average) time to PB
                // targetPBTime is estimated time needed to PB (sum of unsuccessful runs + the successful one)
                TimeSpan resetTimesPBTime = TimeSpan.MaxValue;
                TimeSpan targetPBTime = infiniteTimeSpan;
                
                for (int iteration = 0; iteration < 10; iteration++)
                {
                    TimeSpan[] resultResetTimes = CalculateResetTimes(targetPBTime);

                    SimulationResult result = GetSimulationResult(-1, TimeSpan.Zero);
                    double resetProbability = 1 - result.PbProbability;
                    TimeSpan averageTimeBeforeReset = Divide(result.AverageResetTimeSave, resetProbability);

                    // We assume that a PB happens in (1 / pbProbability) attempts. Thus, the average time to PB is this formula:
                    targetPBTime = Divide(averageTimeBeforeReset, result.PbProbability) + PB;

                    if (categoryWhenCalculationStarted != category)  // category was changed during calculation
                        return;

                    if (targetPBTime < resetTimesPBTime)
                    {
                        resetTimesPBTime = targetPBTime;
                        resetTimes = resultResetTimes;
                    }

                    // Debug prints
                    for (int i = 0; i < segments.Length; i++)
                    {
                        TimeSpan time = resetTimes[i];
                        Console.WriteLine($"Reset time: +{time - state.Run[i].PersonalBestSplitTime[state.CurrentTimingMethod]}");
                    }
                    Console.WriteLine("PB probability: " + result.PbProbability);
                    Console.WriteLine("average reset time: " + averageTimeBeforeReset);
                    Console.WriteLine("targetPBTime: " + targetPBTime);
                }

                isRecalculating = false;
            }
        }

        // Calculate reset times, if we assume it's possible to achieve a PB in targetPBTime (on average)
        private TimeSpan[] CalculateResetTimes(TimeSpan targetPBTime)
        {
            TimeSpan[] resetTimes = new TimeSpan[segments.Length]; 
            resetTimes[segments.Length - 1] = PB;  // we want to PB at the end of last split
            UpdateSimulationResults(segments.Length - 1, PB);

            for (int segment = segments.Length - 2; segment >= 0; segment--)
            {
                // do a binary search to find the reset time for this split
                TimeSpan minimumResetTime = minSplitTimes[segment];
                TimeSpan maximumResetTime = resetTimes[segment + 1];
                if (maximumResetTime > maxSplitTimes[segment])
                {
                    maximumResetTime = maxSplitTimes[segment];
                }

                for (int iteration = 0; iteration < 10; iteration++)
                {
                    TimeSpan medium = Divide(minimumResetTime + maximumResetTime, 2);
                    if (ShouldReset(segment, medium, targetPBTime))
                        maximumResetTime = medium;
                    else
                        minimumResetTime = medium;
                }

                resetTimes[segment] = minimumResetTime;
                UpdateSimulationResults(segment, resetTimes[segment]);
            }
            return resetTimes;
        }

        // Answers whether you should reset, assuming that reset times for the next segments were calculated already
        private bool ShouldReset(int segment, TimeSpan currentTime, TimeSpan targetPBTime)
        {
            SimulationResult result = GetSimulationResult(segment, currentTime);
            // If we play a no-reset run, then with a probability of (1 - pbProbability) it would reset.
            // If we reset now, we will save (on average) AverageResetTimeSave.
            // But we can lose the time to get a PB (if we reset a run that would otherwise PB).
            TimeSpan resetTimeSave = result.AverageResetTimeSave;
            TimeSpan pbTimeLoss = Multiply(targetPBTime, result.PbProbability);
            bool shouldReset = (resetTimeSave > pbTimeLoss);
            return shouldReset;
        }

        private class SimulationResult
        {
            public TimeSpan AverageResetTimeSave { get; set; } = TimeSpan.Zero;
            public double PbProbability { get; set; } = 0;
        }

        private void UpdateSimulationResults(int segment, TimeSpan newResetTime)  // Call when we calculated a reset time for a split
        {
            // calculate the probabilities for each possible case
            int attemptsCount = segments[segment].Count;
            TimeSpan minTime = minSplitTimes.ElementAtOrDefault(segment - 1);
            TimeSpan maxTime = maxSplitTimes.ElementAtOrDefault(segment - 1);

            for (TimeSpan curTime = minTime; curTime <= maxTime; curTime += TimeSpan.FromMilliseconds(100))
            {
                // calculating the probabilities: if at the end of segment {segment - 1} the timer shows {curTime}, what are our chances?

                SimulationResult simulationResult = new SimulationResult();
                TimeSpan timeBeforeResetSum = TimeSpan.Zero;  // calculating average time before reset

                foreach (TimeSpan? segmentAttempt in segments[segment])
                {
                    TimeSpan? timeIfNotReset = null;
                    if (segmentAttempt != null)
                    {
                        timeIfNotReset = curTime + segmentAttempt;
                    }

                    if (segmentAttempt == null || timeIfNotReset >= newResetTime)
                    {
                        // it's a reset
                        TimeSpan timeBeforeReset = newResetTime - curTime + TimeSpan.FromSeconds(settings.TimeToReset);
                        timeBeforeResetSum += timeBeforeReset;
                    }
                    else
                    {
                        if (segment == segments.Length - 1)
                        {
                            // This was the last split. We would've reset if it didn't PB.
                            simulationResult.PbProbability += 1;
                        }
                        else
                        {
                            SimulationResult whatHappenedNext = GetSimulationResult(segment, timeIfNotReset.Value);
                            timeBeforeResetSum += whatHappenedNext.AverageResetTimeSave;
                            simulationResult.PbProbability += whatHappenedNext.PbProbability;
                        }
                    }
                }

                // we're calculating the average of all the possible cases - have to divide by attemptsCount.
                simulationResult.PbProbability /= attemptsCount;
                simulationResult.AverageResetTimeSave = Divide(timeBeforeResetSum, attemptsCount);

                SetSimulationResult(segment - 1, curTime, simulationResult);
            }
        }


        // Get list of attempts for each split, or null if not enough information
        private List<TimeSpan?>[] GetSegmentTimes()
        {
            // Create the lists of split times
            List<TimeSpan?>[] segments = new List<TimeSpan?>[state.Run.Count];
            for (int i = 0; i < state.Run.Count; i++)
            {
                segments[i] = new List<TimeSpan?>();
            }

            // Find the range of attempts to gather times from
            int lastAttempt = state.Run.AttemptHistory.Count;
            int firstAttempt = state.Run.GetMinSegmentHistoryIndex();

            // Gather split times
            for (int attempt = lastAttempt; attempt >= firstAttempt; attempt--)
            {
                int lastSegment = -1;

                // Get split times from a single attempt
                for (int segment = 0; segment < segments.Length; segment++)
                {
                    // gather AttemptCount attempts for each segment
                    if (segments[segment].Count >= settings.AttemptCount)
                    {
                        if (segment == segments.Length - 1)  // last split has the fewest attempts
                            break;
                        else
                            continue;
                    }

                    if (state.Run[segment].SegmentHistory == null || state.Run[segment].SegmentHistory.Count == 0)
                    {
                        // no attempts for a segment
                        return null;
                    }

                    if (state.Run[segment].SegmentHistory.ContainsKey(attempt) && state.Run[segment].SegmentHistory[attempt][state.CurrentTimingMethod] > TimeSpan.Zero)
                    {
                        TimeSpan? segmentTime = state.Run[segment].SegmentHistory[attempt][state.CurrentTimingMethod];
                        segmentTime = RoundToTenthOfSecond(segmentTime);
                        segments[segment].Add(segmentTime);
                        lastSegment = segment;
                    }
                }

                if (lastSegment < state.Run.Count - 1)
                {
                    // Run didn't finish, add "reset" for the last known split
                    segments[lastSegment + 1].Add(null);
                }
            }


            foreach (List<TimeSpan?> segmentAttempts in segments)
            {
                // Each attempt is a reset (null) - we can't calculate that
                if (!segmentAttempts.Any(attempt => attempt != null))
                    return null;
            }

            return segments;
        }

        private List<SimulationResult>[] simulationResultsCached;

        // If after a segment we have a time, what are our chances?
        private SimulationResult GetSimulationResult(int segment, TimeSpan currentTime)
        {
            return simulationResultsCached[segment + 1][GetSimulationResultsIndex(segment, currentTime)];
        }

        private void SetSimulationResult(int segment, TimeSpan currentTime, SimulationResult newResult)
        {
            simulationResultsCached[segment + 1][GetSimulationResultsIndex(segment, currentTime)] = newResult;
        }

        private int GetSimulationResultsIndex(int segment, TimeSpan currentTime)
        {
            TimeSpan minTime = minSplitTimes.ElementAtOrDefault(segment);
            return (int)(currentTime - minTime).TotalMilliseconds / 100;
        }

        private void CalculateMinMaxSplitTimes()
        {
            // "split" is the time from the start of the run
            // "segment" is the time for a selected segment only
            TimeSpan[] minSegmentTimes = new TimeSpan[segments.Length];
            TimeSpan[] maxSegmentTimes = new TimeSpan[segments.Length];
            for (int segment = 0; segment < segments.Length; segment++)
            {
                minSegmentTimes[segment] = segments[segment].Where(t => t != null).Min().Value;
                maxSegmentTimes[segment] = segments[segment].Where(t => t != null).Max().Value;
            }

            minSplitTimes = new TimeSpan[segments.Length];
            maxSplitTimes = new TimeSpan[segments.Length];

            for (int segment = 0; segment < segments.Length; segment++)
            {
                minSplitTimes[segment] = minSplitTimes.ElementAtOrDefault(segment - 1) + minSegmentTimes[segment];
                maxSplitTimes[segment] = maxSplitTimes.ElementAtOrDefault(segment - 1) + maxSegmentTimes[segment];
            }

            simulationResultsCached = new List<SimulationResult>[segments.Length + 1];
            for (int segment = -1; segment < segments.Length; segment++) {
                TimeSpan minTime = minSplitTimes.ElementAtOrDefault(segment);
                TimeSpan maxTime = maxSplitTimes.ElementAtOrDefault(segment);
                int listSize = (int)(maxTime - minTime).TotalMilliseconds / 100 + 1;
                simulationResultsCached[segment + 1] = new List<SimulationResult>(new SimulationResult[listSize]);
            }
        }

        private static TimeSpan? RoundToTenthOfSecond(TimeSpan? timeSpan)
        {
            if (timeSpan == null)
                return null;
            double milliseconds = timeSpan.Value.TotalMilliseconds;
            // using long to avoid double imprecision when storing decimals
            long millisecondRounded = ((long) Math.Round(milliseconds / 100d)) * 100;
            return TimeSpan.FromMilliseconds(millisecondRounded);
        }

        private static TimeSpan Multiply(TimeSpan timeSpan, double multiplier)
        {
            return TimeSpan.FromTicks((long)(timeSpan.Ticks * multiplier));
        }

        private static TimeSpan Divide(TimeSpan timeSpan, double divisor)
        {
            if (divisor == 0)
            {
                if (timeSpan > TimeSpan.Zero)
                    return infiniteTimeSpan;
                else  // 0 / 0
                    return TimeSpan.Zero;
            }
            else
            {
                return TimeSpan.FromTicks((long) (timeSpan.Ticks / divisor));
            }
        }

        public void OnCategoryChanged(string newCategory)
        {
            // if the category has changed, old reset times make no sense
            resetTimes = null;
            category = newCategory;
            CalculateResetTimes();
        }

        // Open up console for debugging

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
    }
}
