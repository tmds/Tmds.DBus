﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace XunitSkip.XunitExtensions
{
    public class SkippableTheoryTestCase : XunitTheoryTestCase
    {
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public SkippableTheoryTestCase() { }

        [Obsolete("Please call the constructor which takes TestMethodDisplayOptions")]
        public SkippableTheoryTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod)
            : base(diagnosticMessageSink, defaultMethodDisplay, TestMethodDisplayOptions.None, testMethod) { }

        public SkippableTheoryTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod) { }

        public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                        IMessageBus messageBus,
                                                        object[] constructorArguments,
                                                        ExceptionAggregator aggregator,
                                                        CancellationTokenSource cancellationTokenSource)
        {
            // Duplicated code from SkippableFactTestCase. I'm sure we could find a way to de-dup with some thought.
            var skipMessageBus = new SkippableFactMessageBus(messageBus);
            var result = await base.RunAsync(diagnosticMessageSink, skipMessageBus, constructorArguments, aggregator, cancellationTokenSource);
            if (skipMessageBus.DynamicallySkippedTestCount > 0)
            {
                result.Failed -= skipMessageBus.DynamicallySkippedTestCount;
                result.Skipped += skipMessageBus.DynamicallySkippedTestCount;
            }

            return result;
        }
    }
}
