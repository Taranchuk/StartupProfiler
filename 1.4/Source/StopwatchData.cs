using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;

namespace ModStartupImpactStats
{
    public class StopwatchData
    {
        public float totalTimeInSeconds;
        public float count;
        public MethodBase targetMethod;
        public string name;
        public Stopwatch stopwatch;
        public StopwatchData(MethodBase targetMethod)
        {
            this.targetMethod = targetMethod;
            this.stopwatch = new Stopwatch();
        }

        public StopwatchData(string name)
        {
            this.name = name;
            this.stopwatch = new Stopwatch();
        }
        public void Start()
        {
            stopwatch.Restart();
        }

        public void Stop()
        {
            stopwatch.Stop();
            var elapsed = (float)stopwatch.ElapsedTicks / Stopwatch.Frequency;
            count++;
            totalTimeInSeconds += elapsed;
        }

        public void LogTime()
        {
            //if (totalTime >= 0.01f)
            {
                if (targetMethod != null)
                {
                    Log.Message(targetMethod.FullMethodName() + " - " + string.Join(", ", targetMethod.GetParameters().Select(x => x.ParameterType)) + " took " + totalTimeInSeconds + ", run count: " + count);
                }
                else
                {
                    if (count > 1)
                    {
                        Log.Message(name + " took " + totalTimeInSeconds + ", run count: " + count + " - average time: " + totalTimeInSeconds / count);
                    }
                    else
                    {
                        Log.Message(name + " took " + totalTimeInSeconds);
                    }
                }
            }
        }
    }
}

