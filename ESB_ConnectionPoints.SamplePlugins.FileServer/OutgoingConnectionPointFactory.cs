using System;
using System.Collections.Generic;
using ESB_ConnectionPoints.PluginsInterfaces;

namespace ESB_ConnectionPoints.SamplePlugins.FileServer
{
    /// <summary>
    /// Фабрика для создания исходящей точки подключения.
    /// </summary>
    public sealed class OutgoingConnectionPointFactory
        : IOutgoingConnectionPointFactory
    {
        public const string DIRECTORY_PARAMETER = "Directory";

        public IOutgoingConnectionPoint Create(Dictionary<string, string> parameters,
            IServiceLocator serviceLocator)
        {
            if (!parameters.ContainsKey(DIRECTORY_PARAMETER))
            {
                throw new ArgumentException(string.Format("Не задан параметр <{0}>",
                    DIRECTORY_PARAMETER));
            }
            string directory = parameters[DIRECTORY_PARAMETER];

            return new OutgoingConnectionPoint(directory, serviceLocator);
        }
    }
}
