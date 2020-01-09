using System;
using System.Collections.Generic;
using ESB_ConnectionPoints.PluginsInterfaces;

namespace ESB_ConnectionPoints.SamplePlugins.File
{
    /// <summary>
    /// Фабрика для создания входящей точки подключения.
    /// </summary>
    public sealed class IngoingConnectionPointFactory
        : IIngoingConnectionPointFactory
    {
        public const string AGROPROM = @"БелАгроПром банк";
        public const string BELARUS = @"Беларусь банк";
        public const string INVEST = @"Белинвест банк";
        public const string Web = @"БелВэб банк";
        public const string BPS = @"БПС банк";
        public const string MM = @"Москва - Минск банк";
        public const string MT = @"МТбанк";
        public const string FILE_NAME_PATTERN_PARAMETER = @"Расширение файлов";
        public const string READ_INTERVAL = @"Интервал чтения";
        public const string NEED_DELETE = @"Удалить файлы после обработки";
        public const string ALL_OOO = @"Выписки ООО";

        public IIngoingConnectionPoint Create(Dictionary<string, string> parameters,
            IServiceLocator serviceLocator)
        {
            string fileNamePattern = "*.*";
            if (parameters.ContainsKey(FILE_NAME_PATTERN_PARAMETER))
            {
                fileNamePattern = parameters[FILE_NAME_PATTERN_PARAMETER];
            }

            string agro = parameters[AGROPROM];
            string belarus = parameters[BELARUS];
            string invest = parameters[INVEST];
            string web = parameters[Web];
            string bps = parameters[BPS];
            string mm = parameters[MM];
            string mt = parameters[MT];
            string llc = parameters[ALL_OOO];
            Double readInterval = Double.Parse(parameters[READ_INTERVAL]);
            Boolean needDelete = false;

            if (parameters[NEED_DELETE] == @"Да")
            {
                needDelete = true;
            }

            return new IngoingConnectionPoint(agro , belarus , invest
                , web , bps , mm , mt , llc , serviceLocator , fileNamePattern , readInterval , needDelete);
    }
    }
}
