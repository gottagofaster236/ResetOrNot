using LiveSplit.Model;
using LiveSplit.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ResetOrNot.UI.Components
{
    class ResetOrNotCalculator
    {
        private List<TimeSpan?>[] segments;
        private TimeSpan[] resetTimes;  // worst acceptable time (from the beginning of the run) at the end of each split
        private TimeSpan PB;
        private const int simulationIterations = 200_000;
        private static readonly TimeSpan infiniteTimeSpan = TimeSpan.FromDays(1000);
        public bool HasDoneCalculatingBefore { get; set; } = false;
        private bool isRecalculating = false;
        private string category = null;

        private LiveSplitState state;
        private ResetOrNotSettings settings;
        private Random rand;
        

        public ResetOrNotCalculator(LiveSplitState state, ResetOrNotSettings settings)
        {
            this.state = state;
            this.settings = settings;
            this.rand = new Random();
            #if DEBUG
            AllocConsole();
            #endif
        }

        public enum ResetAction
        {
            CALCULATING,
            START_THE_RUN,
            CONTINUE_RUN,
            RESET,
            NOT_APPLICABLE
        }

        public ResetAction ShouldReset()
        {
            if (resetTimes == null)
            {
                if (isRecalculating)
                    return ResetAction.CALCULATING;
                else
                    return ResetAction.NOT_APPLICABLE;
            }
            if (state.CurrentSplitIndex == -1)
                return ResetAction.START_THE_RUN;

            TimeSpan? currentTime = state.CurrentTime[state.CurrentTimingMethod];
            if (currentTime < resetTimes[state.CurrentSplitIndex])
                return ResetAction.CONTINUE_RUN;
            else
                return ResetAction.RESET;
        }

        public void CalculateResetTimes()
        {
            Task.Run(CalculateResetTimesSync);
        }

        private void CalculateResetTimesSync()
        {
            lock (this)
            {
                isRecalculating = true;
                HasDoneCalculatingBefore = true;

                if (!settings.SettingsLoaded)  // sometimes we have to wait before the settings get loaded
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

                // Get the current Personal Best, if it exists
                PB = state.Run.Last().PersonalBestSplitTime[state.CurrentTimingMethod].Value;

                if (PB == TimeSpan.Zero)
                {
                    // No personal best, so any run will PB. Don't reset!
                    resetTimes = Enumerable.Repeat(infiniteTimeSpan, segments.Length).ToArray();
                    isRecalculating = false;
                    return;
                }

                string categoryWhenCalculationStarted = category;

                // finding the reset times configuration with best avg PB time
                TimeSpan resetTimesPBTime = infiniteTimeSpan + infiniteTimeSpan;

                TimeSpan targetPBTime = infiniteTimeSpan;
                // targetPBTime is estimated time needed to PB (sum of unsuccessful runs + the successful one)
                for (int iteration = 0; iteration < 10; iteration++)
                {
                    TimeSpan[] resultResetTimes = CalculateResetTimes(targetPBTime); 
                    
                    (TimeSpan averageResetTime, double pbProbability) = RunSimulation(-1, TimeSpan.Zero, resultResetTimes);
                    targetPBTime = Divide(averageResetTime, pbProbability) + PB;
                    // We assume that PB happens in (1 / pbProbability) attempts. Thus, the avg time to PB is the above formula.

                    if (categoryWhenCalculationStarted != category)  // category was changed - have to redo everything
                        return;

                    if (targetPBTime < resetTimesPBTime)
                    {
                        resetTimesPBTime = targetPBTime;
                        resetTimes = resultResetTimes;
                    }

                    // Debug prints
                    foreach (var time in resultResetTimes)
                    {
                        Console.WriteLine("Reset time: " + time);
                    }
                    Console.WriteLine("PB probability: " + pbProbability);
                    Console.WriteLine("average reset time: " + averageResetTime);
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
            for (int segment = segments.Length - 2; segment >= 0; segment--)
            {
                // do a binary search to find the reset time for this split
                TimeSpan minimumResetTime = TimeSpan.Zero;
                TimeSpan maximumResetTime = resetTimes[segment + 1];
                
                for (int iteration = 0; iteration < 20; iteration++)
                {
                    TimeSpan medium = Divide(minimumResetTime + maximumResetTime, 2);
                    if (ShouldReset(segment, medium, targetPBTime, resetTimes))
                        maximumResetTime = medium;
                    else
                        minimumResetTime = medium;
                }

                resetTimes[segment] = minimumResetTime;
            }
            return resetTimes;
        }

        // Answers whether you should reset, assuming that reset times for the next segments were calculated already
        private bool ShouldReset(int segment, TimeSpan currentTime, TimeSpan targetPBTime, TimeSpan[] resetTimes)
        {
            (TimeSpan averageTimeBeforeReset, double pbProbability) = RunSimulation(segment, currentTime, resetTimes);
            // If we play a no-reset run, then with a probability of (1 - pbProbability) it would reset.
            // If we reset now, we will save (on average) averageTimeBeforeReset.
            // But we can lose the time to get a PB (if we reset a run that would otherwise PB).
            TimeSpan resetTimeSave = Multiply(averageTimeBeforeReset, 1 - pbProbability);
            TimeSpan pbTimeLoss = Multiply(targetPBTime, pbProbability);
            bool shouldReset = (resetTimeSave > pbTimeLoss);
            return shouldReset;
        }

        private (TimeSpan averageTimeBeforeReset, double pbProbability) RunSimulation
            (int startSegment, TimeSpan currentTime, TimeSpan[] resetTimes)
        {
            // calculating average time before reset
            TimeSpan timeBeforeResetSum = TimeSpan.Zero;
            int amountOfResets = 0;

            int amountOfPBs = 0;
            
            for (int iteration = 0; iteration < simulationIterations; iteration++)
            {
                TimeSpan resultTime = currentTime;
                for (int segment = startSegment + 1; segment < segments.Length; segment++)
                {
                    TimeSpan? splitTime = segments[segment][rand.Next(segments[segment].Count)];
                    TimeSpan? timeIfNotReset = null;
                    if (splitTime != null)
                    {
                        timeIfNotReset = resultTime + splitTime;
                    }

                    if (splitTime == null || timeIfNotReset >= resetTimes[segment])
                    {
                        // This is a reset
                        amountOfResets++;
                        TimeSpan timeBeforeReset = resetTimes[segment] - currentTime + TimeSpan.FromSeconds(settings.TimeToReset);
                        timeBeforeResetSum += timeBeforeReset;
                        break;
                    }
                    else
                    {
                        resultTime = timeIfNotReset.Value;
                        if (segment == segments.Length - 1)
                        {
                            // This was the last split. We would've reset if it didn't PB.
                            amountOfPBs++;
                        }
                    }
                }
            }

            TimeSpan averageTimeBeforeReset = Divide(timeBeforeResetSum, amountOfResets);
            double pbProbability = (double)amountOfPBs / simulationIterations;
            return (averageTimeBeforeReset, pbProbability);
        }


        // List of attempts for each split, or null if not enough information
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
            int runCount = state.Run.AttemptHistory.Count;
            if (!settings.IgnoreRunCount)
            {
                runCount = Math.Min(state.Run.AttemptCount, state.Run.AttemptHistory.Count);
            }

            int firstAttempt;
            if (settings.UseFixedAttempts)
            {
                // Fixed number of attempts
                firstAttempt = lastAttempt - settings.AttemptCount;

                if (firstAttempt < state.Run.GetMinSegmentHistoryIndex())
                {
                    firstAttempt = state.Run.GetMinSegmentHistoryIndex();
                }
            }
            else
            {
                // Percentage of attempts
                firstAttempt = lastAttempt - runCount * settings.AttemptCount / 100;
                if (firstAttempt < state.Run.GetMinSegmentHistoryIndex())
                {
                    firstAttempt = state.Run.GetMinSegmentHistoryIndex();
                }
            }

            // Gather split times
            for (int attempt = firstAttempt; attempt < lastAttempt; attempt++)
            {
                int lastSegment = -1;

                // Get split times from a single attempt
                for (int segment = 0; segment < state.Run.Count; segment++)
                {
                    if (state.Run[segment].SegmentHistory == null || state.Run[segment].SegmentHistory.Count == 0)
                    {
                        // no attempts for a segment
                        return null;
                    }

                    if (state.Run[segment].SegmentHistory.ContainsKey(attempt) && state.Run[segment].SegmentHistory[attempt][state.CurrentTimingMethod] > TimeSpan.Zero)
                    {
                        segments[segment].Add(state.Run[segment].SegmentHistory[attempt][state.CurrentTimingMethod]);
                        lastSegment = segment;
                    }
                }

                if (lastSegment < state.Run.Count - 1)
                {
                    // Run didn't finish, add "reset" for the last known split
                    segments[lastSegment + 1].Add(null);
                }
            }


            foreach (var segmentAttempts in segments)
            {
                // Each attempt is a reset (null) - we can't calculate that
                if (!segmentAttempts.Any(attempt => attempt != null))
                    return null;
            }

            return segments;
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

        // Debug stuff

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
    }
}
