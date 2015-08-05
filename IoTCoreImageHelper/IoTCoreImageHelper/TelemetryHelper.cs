// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Microsoft.Diagnostics.Tracing;
using System.Security.Cryptography;

namespace IoTCoreImageHelper
{
    class TelemetryHelper
    {
        // Trait that declares the provider's membership in the Microsoft Telemetry provider group
        private static string[] traits = { "ETW_GROUP", "{4f50731a-89cf-4782-b3e0-dce8c90476ba}" };

        // Setting the event category to Critical so events won't be sampled out
        private const EventKeywords MicrosoftTelemetry = (EventKeywords)0x0000800000000000;  //0x0200000000000;
        private static readonly string EventProvider = "Microsoft.Windows.IoT.CoreImageHelper";
        public static EventSource eventLogger = new EventSource(EventProvider, EventSourceSettings.EtwSelfDescribingEventFormat, traits);

        public static EventSourceOptions DebugVerboseOption = new EventSourceOptions() { Level = EventLevel.Verbose };
        public static EventSourceOptions DebugErrorOption = new EventSourceOptions() { Level = EventLevel.Error };
        public static EventSourceOptions TelemetryInfoOption = new EventSourceOptions() { Keywords = MicrosoftTelemetry };
        public static EventSourceOptions TelemetryErrorOption = new EventSourceOptions() { Level = EventLevel.Error, Keywords = MicrosoftTelemetry };
        public static EventSourceOptions TelemetryStartOption = new EventSourceOptions() { Opcode = EventOpcode.Start, Keywords = MicrosoftTelemetry };
        public static EventSourceOptions TelemetryStopOption = new EventSourceOptions() { Opcode = EventOpcode.Stop, Keywords = MicrosoftTelemetry };

        // For app Launch and Exit events, we rely on built in events from Threshold
        // KernelProcess.ProcessStarted
        // KernelProcess.ProcessStopped

        public static readonly string SessionEvent = "SessionEvent";
        public static readonly string FlashEvent = "FlashEvent";
        public static readonly string FlashErrorEvent = "FlashErrorEvent";

        public static string getMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                }
            }
        }
    }
}
