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

        private LiveSplitState state;
        private ResetOrNotSettings settings;
        private Random rand;
        private bool isRecalculating = false;

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
            RESET,
            CONTINUE_RUN,
            CALCULATING,  // "Please wait..."
            NOT_APPLICABLE
        }

        public ResetAction ShouldReset()
        {
            if (isRecalculating)
                return ResetAction.CALCULATING;
            if (resetTimes == null)
                return ResetAction.NOT_APPLICABLE;
            if (state.CurrentSplitIndex == -1)
                return ResetAction.NOT_APPLICABLE;

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
                if (isRecalculating)
                    return;
                isRecalculating = true;
            }

            if (!settings.SettingsLoaded)
            {
                isRecalculating = false;
                return;
            }

            segments = GetSegmentTimes();
            if (segments == null) {  // Data for some of the splits is missing
                resetTimes = null;
                isRecalculating = false;
                return;
            }

            // Get the current Personal Best, if it exists
            PB = (TimeSpan)state.Run.Last().PersonalBestSplitTime[state.CurrentTimingMethod];

            if (PB == TimeSpan.Zero)
            {
                // No personal best, so any run will PB. Don't reset!
                resetTimes = Enumerable.Repeat(infiniteTimeSpan, segments.Length).ToArray();
                isRecalculating = false;
                return;
            }

            resetTimes = new TimeSpan[segments.Length];

            TimeSpan targetPBTime = infiniteTimeSpan;
            // targetPBTime is estimated time needed to PB (sum of runs)
            for (int iteration = 0; iteration < 5; iteration++)
            {
                CalculateResetTimes(targetPBTime);
                (TimeSpan averageResetTime, double pbProbability) = RunSimulation(-1, TimeSpan.Zero);
                targetPBTime = Divide(averageResetTime, pbProbability) + PB;

                foreach (var time in resetTimes)
                {
                    Console.WriteLine("Reset time: " + time);
                }
                Console.WriteLine("PB probability: " + pbProbability);
                Console.WriteLine("average reset time: " + averageResetTime);
                Console.WriteLine("targetPBTime: " + targetPBTime);
            }
            isRecalculating = false;
        }

        // Calculate reset times, if we assume it's possible to achieve a PB in targetPBTime (on average)
        private void CalculateResetTimes(TimeSpan targetPBTime)
        {
            resetTimes[segments.Length - 1] = PB;  // we want to PB at the end of last split
            for (int segment = segments.Length - 2; segment >= 0; segment--)
            {
                TimeSpan minimumResetTime = TimeSpan.Zero;
                TimeSpan maximumResetTime = resetTimes[segment + 1];
                // do a binary search to find the reset time for this split
                for (int iteration = 0; iteration < 20; iteration++)
                {
                    TimeSpan medium = Divide(minimumResetTime + maximumResetTime, 2);
                    if (ShouldReset(segment, medium, targetPBTime))
                        maximumResetTime = medium;
                    else
                        minimumResetTime = medium;
                }

                resetTimes[segment] = minimumResetTime;
            }
        }

        // assuming that the reset time for next segments were counted already
        private bool ShouldReset(int segment, TimeSpan currentTime, TimeSpan targetPBTime)
        {
            (TimeSpan averageTimeBeforeReset, double pbProbability) = RunSimulation(segment, currentTime);
            TimeSpan resetTimeSave = averageTimeBeforeReset;
            TimeSpan pbTimeLoss = Divide(targetPBTime, 1 / pbProbability);  // should've implemented multiply
            bool shouldReset = (resetTimeSave > pbTimeLoss);
            return shouldReset;
        }

        private (TimeSpan averageTimeBeforeReset, double pbProbability) RunSimulation
            (int startSegment, TimeSpan currentTime)
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
                        resultTime = (TimeSpan)timeIfNotReset;
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

        // Debug stuff

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
    }
}
