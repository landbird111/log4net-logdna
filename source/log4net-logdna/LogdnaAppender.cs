using log4net.Appender;
using log4net.Core;
using System;

namespace log4net.logdna
{
    public class LogdnaAppender : AppenderSkeleton
    {
        private ILogdnaFormatter _formatter;
        private ILogdnaAsyncBuffer _buffer;
        private readonly Config _config;
        private ILogdnaClient _client;

        public LogdnaAppender()
        {
            _config = new Config();
        }

        internal LogdnaAppender(Config config, ILogdnaFormatter formatter, ILogdnaAsyncBuffer buffer)
            : this()
        {
            _config = config;
            _formatter = formatter;
            _buffer = buffer;
        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();
            _formatter = _formatter ?? new LogdnaFormatter(_config);
            _client = new LogdnaClient(_config);
            _buffer = _buffer ?? new LogdnaAsyncBuffer(_config, _client);
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            // We should always format event in the same thread as
            // many properties used in the event are associated with the current thread.

            // if user defined layout let it render the message based on layout, otherwise get message from event
            var renderedMessage = Layout != null
                ? RenderLoggingEvent(loggingEvent)
                : loggingEvent.RenderedMessage;

            var formattedLog = _formatter.ToJson(loggingEvent, renderedMessage);
            if (formattedLog != null)
            {
                _buffer.BufferForSend(formattedLog);
            }
        }

        public override bool Flush(int millisecondsTimeout)
        {
            return _buffer.Flush(TimeSpan.FromMilliseconds(millisecondsTimeout));
        }

        protected override void OnClose()
        {
            base.OnClose();
            _buffer.Flush(_config.FinalFlushWaitTime);
            _buffer.Dispose();
            _buffer = null;
        }

        #region Configuration properties

        public string RootUrl
        {
            get => _config.RootUrl;
            set => _config.RootUrl = value;
        }

        [Obsolete("This is old config key for customer token. It's now suggested to use more readable 'customerToken'")]
        public string InputKey
        {
            get => CustomerToken;
            set => CustomerToken = value;
        }

        public string CustomerToken
        {
            get => _config.CustomerToken;
            set => _config.CustomerToken = value;
        }

        public string UserAgent
        {
            get => _config.UserAgent;
            set => _config.UserAgent = value;
        }

        public int TimeoutInSeconds
        {
            get => _config.TimeoutInSeconds;
            set => _config.TimeoutInSeconds = value;
        }

        public string Tag
        {
            get => _config.Tag;
            set => _config.Tag = value;
        }

        public int BufferSize
        {
            get => _config.BufferSize;
            set => _config.BufferSize = value;
        }

        public TimeSpan SendInterval
        {
            get => _config.SendInterval;
            set => _config.SendInterval = value;
        }

        public int NumberOfInnerExceptions
        {
            get => _config.NumberOfInnerExceptions;
            set => _config.NumberOfInnerExceptions = value;
        }

        public string App
        {
            get => _config.App;
            set => _config.App = value;
        }

        public string Env
        {
            get => _config.Env;
            set => _config.Env = value;
        }

        #endregion Configuration properties
    }
}