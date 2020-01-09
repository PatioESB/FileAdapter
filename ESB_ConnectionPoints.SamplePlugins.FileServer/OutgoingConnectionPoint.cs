using System;
using System.IO;
using System.Text;
using System.Threading;
using ESB_ConnectionPoints.PluginsInterfaces;

namespace ESB_ConnectionPoints.SamplePlugins.FileServer
{
    /// <summary>
    /// Исходящая точка подключения для обработки сообщений из шины данных.
    /// </summary>
    public sealed class OutgoingConnectionPoint
        : IStandartOutgoingConnectionPoint
    {
        private const string PUT_FILE_COMMAND = "PutFile";
        private const string GET_FILE_COMMAND = "GetFile";
        private const string LIST_FILES_COMMAND = "ListFiles";
        
        private const string FILE_NAME_PROPERTY = "FileName";

        /// <summary>
        /// Логгер.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Фабрика сообщений.
        /// </summary>
        private readonly IMessageFactory _messageFactory;

        /// <summary>
        /// Директория для хранения сообщений.
        /// </summary>
        private readonly string _directory;

        public OutgoingConnectionPoint(string directory, IServiceLocator serviceLocator)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("Не задан параметр <directory>");
            }

            _logger = serviceLocator.GetLogger(GetType());
            _messageFactory = serviceLocator.GetMessageFactory();
            _directory = directory;
        }

        public void Initialize()
        {
            if (!Directory.Exists(_directory))
            {
                _logger.Debug("Создание директории " + _directory);
                Directory.CreateDirectory(_directory);
            }
        }

        public void Run(IMessageSource messageSource, IMessageReplyHandler replyHandler,
            CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var message = messageSource.PeekLockMessage(ct, 10000);
                if (message == null)
                {
                    continue;
                }

                Message reply = null;
                try
                {
                    reply = ExecuteRequest(message);
                }
                catch (Exception ex)
                {
                    _logger.Error(string.Format("Возникла ошибка при обработке сообщения {0}", message), ex);
                    
                    var errorCode = MessageHandlingError.UnknowError;
                    if (ex is MessageHandlingException)
                    {
                        errorCode = (ex as MessageHandlingException).GetErrorCode();
                    }
                    // Удаляем сообщение с кодом ошибки
                    messageSource.CompletePeekLock(message.Id, errorCode, ex.Message);
                    continue;
                }

                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        if (replyHandler.HandleReplyMessage(reply))
                        {
                            break;
                        }
                        ct.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    }
                }
                catch (MessageHandlingException ex)
                {
                    _logger.Error(string.Format("Возникла ошибка при обработке ответного сообщения {0}", reply), ex);
                }
                messageSource.CompletePeekLock(message.Id);
            }
        }

        private Message ExecuteRequest(Message request)
        {
            if (request.Type == GET_FILE_COMMAND)
            {
                if (!request.HasProperty(FILE_NAME_PROPERTY))
                {
                    throw new InvalidMessageFormatException(string.Format(
                        "Сообщение не содержит свойство <{0}>", FILE_NAME_PROPERTY));
                }
                var filename = request.Properties[FILE_NAME_PROPERTY].ToString();
                var filepath = Path.Combine(_directory, filename);
                var data = System.IO.File.ReadAllBytes(filepath);

                var reply = _messageFactory.CreateReplyMessage(request, "File");
                reply.Body = data;
                return reply;
            }
            if (request.Type == PUT_FILE_COMMAND)
            {
                if (!request.HasProperty(FILE_NAME_PROPERTY))
                {
                    throw new InvalidMessageFormatException(string.Format(
                        "Сообщение не содержит свойство <{0}>", FILE_NAME_PROPERTY));
                }
                var filename = request.Properties[FILE_NAME_PROPERTY].ToString();
                var filepath = Path.Combine(_directory, filename);
                var data = request.Body;
                if (data == null)
                {
                    data = new byte[0];
                }
                System.IO.File.WriteAllBytes(filepath, data);

                return _messageFactory.CreateReplyMessage(request, "Success");
            }
            if (request.Type == LIST_FILES_COMMAND)
            {
                var files = Directory.GetFiles(_directory);
                var data = string.Join("\n", files);

                var reply = _messageFactory.CreateReplyMessage(request, "FileList");
                reply.Body = Encoding.UTF8.GetBytes(data);
                return reply;
            }
            throw new InvalidMessageTypeException(request.Type);
        }

        public void Cleanup()
        {
        }

        public void Dispose()
        {
        }
    }
}
