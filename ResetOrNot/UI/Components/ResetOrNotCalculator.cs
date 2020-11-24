using LiveSplit.Model;
using LiveSplit.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ResetOrNot.UI.Components
{
    class ResetOrNotCalculator
    {
        private List<TimeSpan>[] splits;
        private TimeSpan[] resetTimes;  // worst acceptable time (from the beginning of the run) at the end of each split
        private TimeSpan PB;
        private const int maxSimulationIterations = 10_000_000;
        private static readonly TimeSpan infiniteTimeSpan = TimeSpan.FromDays(1000);

        private LiveSplitState state;
        private ResetOrNotSettings settings;
        private Random rand;

        public ResetOrNotCalculator(LiveSplitState state, ResetOrNotSettings settings)
        {
            this.state = state;
            this.settings = settings;
            this.rand = new Random();
            AllocConsole();
        }

        public enum ResetAction
        {
            RESET,
            CONTINUE_RUN,
            NOT_APPLICABLE
        }

        public async Task<ResetAction> ShouldReset()
        {
            // for now doing it like this
            if (resetTimes == null)  // fixme!! reset times may be null if something goes wrong!
                await CalculateResetTimes();
            if (resetTimes == null)
                return ResetAction.NOT_APPLICABLE;
            if (state.CurrentSplitIndex == -1)
                return ResetAction.NOT_APPLICABLE;

            TimeSpan currentTime = (TimeSpan)state.CurrentSplit.SplitTime[state.CurrentTimingMethod];
            if (currentTime < resetTimes[state.CurrentSplitIndex])
                return ResetAction.CONTINUE_RUN;
            else
                return ResetAction.RESET;
        }

        public async Task CalculateResetTimes()
        {
            splits = GetSplitTimes();
            if (splits == null) {  // Data for some of the splits is missing
                resetTimes = null;
                return;
            }

            // Get the current Personal Best, if it exists
            PB = (TimeSpan)state.Run.Last().PersonalBestSplitTime[state.CurrentTimingMethod];

            if (PB == TimeSpan.Zero)
            {
                // No personal best, so any run will PB. Don't reset!
                resetTimes = Enumerable.Repeat(infiniteTimeSpan, splits.Length).ToArray();
                return;
            }

            resetTimes = new TimeSpan[splits.Length];

            TimeSpan targetPBTime = TimeSpan.FromHours(1000);
            CalculateResetTimes(targetPBTime);
            (TimeSpan averageResetTime, double pbProbability) = RunSimulation(-1, TimeSpan.Zero);
            TimeSpan actualPBTime = Divide(averageResetTime, pbProbability);
            Console.WriteLine("PB probability: " + pbProbability);
            Console.WriteLine("average reset time: " + averageResetTime);
            Console.WriteLine("actualPbTime: " + actualPBTime);
            foreach (var time in resetTimes)
            {
                Console.WriteLine("Reset time: " + time);
            }
        }

        // Calculate reset times, if we assume it's possible to achieve a PB in targetPBTime (on average)
        private void CalculateResetTimes(TimeSpan targetPBTime)
        {
            resetTimes[splits.Length - 1] = PB;  // we want to PB at the end of last split
            for (int segment = splits.Length - 2; segment >= 0; segment--)
            {
                TimeSpan minimumResetTime = TimeSpan.Zero;
                TimeSpan maximumResetTime = PB;
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

            int iteration;
            for (iteration = 0; iteration < maxSimulationIterations; iteration++)
            {
                TimeSpan resultTime = currentTime;
                for (int segment = startSegment + 1; segment < splits.Length; segment++)
                {
                    TimeSpan splitTime = splits[segment][rand.Next(splits[segment].Count)];
                    TimeSpan timeIfNotReset = resultTime + splitTime;
                    if (timeIfNotReset >= resetTimes[segment])
                    {
                        // this is a reset
                        amountOfResets++;
                        TimeSpan timeBeforeReset = resetTimes[segment] - currentTime;
                        timeBeforeReset += TimeSpan.FromSeconds(settings.TimeToReset);
                        timeBeforeResetSum += timeBeforeReset;
                        break;
                    }
                    else
                    {
                        resultTime = timeIfNotReset;
                        if (segment == splits.Length - 1)
                        {
                            // this was the last split
                            amountOfPBs++;
                            if (amountOfPBs >= 50)
                                break;
                        }
                    }
                }
            }

            TimeSpan averageTimeBeforeReset = Divide(timeBeforeResetSum, amountOfResets);
            double pbProbability = (double)amountOfPBs / iteration;
            return (averageTimeBeforeReset, pbProbability);
        }


        // List of attempts for each split, or null if not enough information
        private List<TimeSpan>[] GetSplitTimes()
        {
            // Create the lists of split times
            List<TimeSpan>[] splits = new List<TimeSpan>[state.Run.Count];
            for (int i = 0; i < state.Run.Count; i++)
            {
                splits[i] = new List<TimeSpan>();
            }

            // Find the range of attempts to gather times from
            int lastAttempt = state.Run.AttemptHistory.Count;
            int runCount = state.Run.AttemptHistory.Count;
            if (!settings.IgnoreRunCount)
            {
                runCount = Math.Min(state.Run.AttemptCount, state.Run.AttemptHistory.Count);
            }

            int firstAttempt = lastAttempt / 2;  // Using 50% of the attempts by default
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

            for (int segment = 0; segment < state.Run.Count; segment++)
            {
                if (state.Run[segment].SegmentHistory == null || state.Run[segment].SegmentHistory.Count == 0)
                {
                    // No history for a segment
                    return null;
                }

                for (int attempt = firstAttempt; attempt < lastAttempt; attempt++)
                {
                    if (state.Run[segment].SegmentHistory.ContainsKey(attempt))
                    {
                        TimeSpan attemptTime = state.Run[segment].SegmentHistory[attempt][state.CurrentTimingMethod] ?? TimeSpan.Zero;
                        if (attemptTime > TimeSpan.Zero)
                            splits[segment].Add(attemptTime);
                    }
                }
            }


            foreach (var segmentAttempts in splits)
            {
                if (segmentAttempts.Count == 0)
                {
                    // No successful attempts for a segment
                    return null;
                }
            }

            return splits;
        }

        private static TimeSpan Divide(TimeSpan timeSpan, double divisor)
        {
            if (divisor == 0)
            {
                return infiniteTimeSpan;
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

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeConsole();
    }
}
