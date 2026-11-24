using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Rinsen.Outback.Tests.Helpers
{
    /// <summary>
    /// Records every formatted log entry (message plus exception) for later assertion.
    /// </summary>
    public class CapturingLogger<T> : ILogger<T>
    {
        private readonly List<string> _entries = [];

        public IReadOnlyList<string> Entries => _entries;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var entry = formatter(state, exception);

            if (exception is not null)
            {
                entry = $"{entry} :: {exception}";
            }

            _entries.Add(entry);
        }

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
