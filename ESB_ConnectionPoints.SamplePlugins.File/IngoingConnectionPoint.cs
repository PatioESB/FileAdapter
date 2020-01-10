using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ESB_ConnectionPoints.PluginsInterfaces;

namespace ESB_ConnectionPoints.SamplePlugins.File
{
    /// <summary>
    /// Входящая точка подключения для передачи сообщений в шину.
    /// </summary>
    public sealed class IngoingConnectionPoint
        : IStandartIngoingConnectionPoint
    {
        /// <summary>
        /// Логгер.
        /// </summary>
        private readonly ILogger _logger;
        /// <summary>
        /// Фабрика сообщений.
        /// </summary>
        private readonly IMessageFactory _messageFactory;
        /// <summary>
        /// Банки
        /// </summary>
        private readonly string _agro;
        private readonly string _belarus;
        private readonly string _invest;
        private readonly string _llc;
        private readonly string _web;
        private readonly string _bps;
        private readonly string _mm;
        private readonly string _mt;
        private readonly string _fileNamePattern;
        /// <summary>
        /// Хеш суммы файлов
        /// </summary>
        private readonly HashSet<string> _processedFiles = new HashSet<string>();
        /// <summary>
        /// Интервал чтения из каталогов
        /// </summary>
        private TimeSpan _readInterval; 
        /// <summary>
        /// Массив директорий
        /// </summary>
        private string[] inputDirectory = new string[8];
        /// <summary>
        /// Массив времени модификации файлов.
        /// </summary>
        private DateTime[] arrLastWriteTime = new DateTime[8];
        /// <summary>
        /// Класс сообщения
        /// </summary>
        private string _classId;
        /// <summary>
        /// Нужно ли удаления файла
        /// </summary>
        private Boolean _needDelete;
        public IngoingConnectionPoint(string agro, string belarus, string invest, string web,
            string bps, string mm, string mt, string llc , IServiceLocator serviceLocator , string fileNamePattern , Double readInterval , Boolean needDelete)
        {
            _logger = serviceLocator.GetLogger(GetType());
            _messageFactory = serviceLocator.GetMessageFactory();
            _agro = agro;
            _belarus = belarus;
            _invest = invest;
            _web = web;
            _bps = bps;
            _mm = mm;
            _mt = mt;
            _llc = llc;
            _fileNamePattern = fileNamePattern;
            _readInterval = TimeSpan.FromSeconds(readInterval);
            _needDelete = needDelete;
        }

        public void Initialize()
        {
            if (!Directory.Exists(_agro))
            {
               _logger.Warning ("Не задана директория БелАгроБанка " + _agro);
            }
            if (!Directory.Exists(_belarus))
            {
                _logger.Warning ("Не задана директория БеларусьБанка " + _belarus);
            }
            if (!Directory.Exists(_invest))
            {
                _logger.Warning("Не задана директория БелИнвестБанка " + _invest);
            }
            if (!Directory.Exists(_web))
            {
                _logger.Warning("Не задана директория БелВЭББанка " + _web);
            }
            if (!Directory.Exists(_bps))
            {
                _logger.Warning("Не задана директория БПСБанка " + _bps);
            }
            if (!Directory.Exists(_mm))
            {
                _logger.Warning("Не задана директория ММБанка " + _mm);
            }
            if (!Directory.Exists(_mt))
            {
                _logger.Warning("Не задана директория МТБанка " + _mt);
            }
            if (!Directory.Exists(_llc))
            {
                _logger.Warning("Не задана директория ООО " + _llc);
            } 
            inputDirectory[0] = _agro;
            inputDirectory[1] = _belarus;
            inputDirectory[2] = _invest;
            inputDirectory[3] = _web;
            inputDirectory[4] = _bps;
            inputDirectory[5] = _mm;
            inputDirectory[6] = _mt;
            inputDirectory[7] = _llc;
        }

        public void Run(IMessageHandler messageHandler, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                foreach (string singleInput in inputDirectory)
            {
                    if (string.IsNullOrEmpty(singleInput))
                    {
                        continue;
                    }
                    _classId = getClassId(singleInput);
                    foreach (var filepath in GetFileList(singleInput))
                    {
                        var filename = Path.GetFileName(filepath);
                        //if (_processedFiles.Contains(filename))
                        //{
                        //    continue;
                        //}

                        var message = TryCreateMessageFromFile(filepath , _classId);
                        if (message == null)
                        {
                            // Не удалось прочитать файл и создать сообщение
                            _processedFiles.Add(filename);
                            continue;
                        }
                        try
                        {
                            if (messageHandler.HandleMessage(message))
                            {
                                if (!TryDeleteFile(filepath))
                                {
                                    _processedFiles.Add(filename);
                            }
                        }
                            else
                            {
                                // Обработчик сообщений занят, поэтому делаем паузу
                                ct.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(string.Format("Возникла ошибка при обработке файла {0}", filepath), ex);
                        }
                        if (DateTime.Now.Hour == 22 & filepath == inputDirectory[0] ^ filepath == inputDirectory[4])
                        {
                            _logger.Info("Начало удаления файлов из указанных папок " + filepath);
                            clearFolder(filepath);
                        }
                    }       
                }
                ct.WaitHandle.WaitOne(_readInterval);
            }
        }

        public void Cleanup()
        { 
        }

        public void Dispose()
        { 
        }
        /// <summary>
        /// Получения класса сообщения по директории
        /// </summary>
        /// <param name="singleInput"></param>
        /// <returns></returns>
        public string getClassId(string singleInput)
        {
            if (singleInput == inputDirectory[0])
            {
                _classId = "BelAgro";
            }
            if (singleInput == inputDirectory[1])
            {
                _classId = "Belarus";
            }
            if (singleInput == inputDirectory[2])
            {
                _classId = "BelInvest";
            }
            if (singleInput == inputDirectory[3])
            {
                _classId = "BelWEB";
            }
            if (singleInput == inputDirectory[4])
            {
                _classId = "BPS";
            }
            if (singleInput == inputDirectory[5])
            {
                _classId = "MMB";
            }
            if (singleInput == inputDirectory[6])
            {
                _classId = "MTBank";
            }
            if (singleInput == inputDirectory[7])
            {
                _classId = "OOO";
            }
            return _classId;
        }
        /// <summary>
        /// Получение спика файлов в директории.
        /// </summary>
        /// <returns>Список файлов.</returns>
        private string[] GetFileList(string singleInput)
        {
                if (string.IsNullOrEmpty(_fileNamePattern))
                {
                    return Directory.GetFiles(singleInput);
                }
                else
                {
                    return Directory.GetFiles(singleInput, _fileNamePattern);
                }
        }
        /// <summary>
        /// Создание сообщения из указанного файла.
        /// </summary>
        /// <param name="filepath">Путь к файлу.</param>
        /// <returns>Сообщение.</returns>
        private Message TryCreateMessageFromFile(string filepath , string classId)
        {
            try
            {
                if (System.IO.File.GetLastWriteTime(filepath) > getLastWriteTimeToMemorry(classId))
                {
                    var message = _messageFactory.CreateMessage("File");

                    message.AddPropertyWithValue("OriginalFileName", Path.GetFileName(filepath));
                    message.Body = System.IO.File.ReadAllBytes(filepath);
                    message.ClassId = classId;

                    addLastWriteTime(System.IO.File.GetLastWriteTime(filepath), classId);

                    return message;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("Не удалось создать сообщения из файла {0}", filepath), ex);
                return null;
            }
        }
        /// <summary>
        /// Попытка удаления файла.
        /// </summary>
        /// <param name="filepath">Путь к файлу.</param>
        /// <returns>true - если файл удален, иначе - false.</returns>
        private bool TryDeleteFile(string filepath)
        {
            try
            {
                if (_needDelete)
                {
                    System.IO.File.Delete(filepath);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Возникла ошибка при удалении файла " + filepath, ex);
                return false;
            }
        }
        /// <summary>
        /// Добавления времени модификации файла
        /// </summary>
        /// <param name="LastTimeDoc"></param>
        private void addLastWriteTime(DateTime LastTimeDoc , string classId)
        {
            if (classId == "BelAgro")
            {
                arrLastWriteTime[0] = LastTimeDoc;
            }
            if (classId == "Belarus")
            {
                arrLastWriteTime[1] = LastTimeDoc;
            }
            if (classId == "BelInvest")
            {
                arrLastWriteTime[2] = LastTimeDoc;
            }
            if (classId == "BelWEB")
            {
                arrLastWriteTime[3] = LastTimeDoc;
            }
            if (classId == "BPS")
            {
                arrLastWriteTime[4] = LastTimeDoc;
            }
            if (classId == "MMB")
            {
                arrLastWriteTime[5] = LastTimeDoc;
            }
            if (classId == "MTBank")
            {
                arrLastWriteTime[6] = LastTimeDoc;
            }
            if (classId == "OOO")
            {
                arrLastWriteTime[7] = LastTimeDoc;
            }
        }
        /// <summary>
        /// Получения времени модификации документов
        /// </summary>
        /// <param name="classId"></param>
        /// <returns></returns>
        private DateTime getLastWriteTimeToMemorry(string classId)
        {
            if (classId == "BelAgro")
            {
                return arrLastWriteTime[0];
            }
            if (classId == "Belarus")
            {
                return arrLastWriteTime[1];
            }
            if (classId == "BelInvest")
            {
                return arrLastWriteTime[2];
            }
            if (classId == "BelWEB")
            {
                return arrLastWriteTime[3];
            }
            if (classId == "BPS")
            {
                return arrLastWriteTime[4];
            }
            if (classId == "MMB")
            {
                return arrLastWriteTime[5];
            }
            if (classId == "MTBank")
            {
                return arrLastWriteTime[6];
            }
            if (classId == "OOO")
            {
                return arrLastWriteTime[7];
            }
            else
            {
                return DateTime.Now;
            }
        }
        /// <summary>
        /// Очистка папок.
        /// </summary>
        private void clearFolder(string filepath)
        {
            try
            {
                System.IO.File.Delete(filepath);
            } 
            catch(Exception ex)
            {
                _logger.Error("Произошла ошибка при удалении файла! " + ex.Message);
            }
        }
    }
}
