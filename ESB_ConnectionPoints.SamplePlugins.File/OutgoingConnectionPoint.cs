using System;
using System.IO;
using ESB_ConnectionPoints.PluginsInterfaces;

namespace ESB_ConnectionPoints.SamplePlugins.File
{
    /// <summary>
    /// Исходящая точка подключения для обработки сообщений из шины данных.
    /// </summary>
    public sealed class OutgoingConnectionPoint
        : ISimpleOutgoingConnectionPoint
    {
        /// <summary>
        /// Логгер.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Директория для записи файлов.
        /// </summary>
        private readonly string _outputDirectory;

        public OutgoingConnectionPoint(string outputDirectory, IServiceLocator serviceLocator)
        {
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new ArgumentException("Не задан параметр <outputDirectory>");
            }

            _logger = serviceLocator.GetLogger(GetType());
            _outputDirectory = outputDirectory;
        }

        public void Initialize()
        {
            if (!Directory.Exists(_outputDirectory))
            {
                _logger.Debug("Создание директории " + _outputDirectory);
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        public bool HandleMessage(Message message, IMessageReplyHandler replyHandler)
        {
            var data = message.Body;
            if (data == null)
            {
                data = new byte[0];
            }

            try
            {
                string filePath = Path.Combine(_outputDirectory,
                    string.Format("{0}.message", message.Id));
                System.IO.File.WriteAllBytes(filePath, data);
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("Возникла ошибка при записи в файл данных сообщения {0}", message));
                throw new MessageHandlingException(ex.Message, ex);
            }
            return true;
        }

        public bool IsReady()
        {
            return true;
        }

        public void Cleanup()
        { 
        }

        public void Dispose()
        { 
        }
    }
}
