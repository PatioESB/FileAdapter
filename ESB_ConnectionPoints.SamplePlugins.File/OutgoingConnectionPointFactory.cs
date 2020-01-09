using System;
using System.Collections.Generic;
using ESB_ConnectionPoints.PluginsInterfaces;

namespace ESB_ConnectionPoints.SamplePlugins.File
{
    /// <summary>
    /// Фабрика для создания исходящей точки подключения.
    /// </summary>
    public sealed class OutgoingConnectionPointFactory
        : IOutgoingConnectionPointFactory
    {
        public const string OUTPUT_DIRECTORY_PARAMETER = "OutputDirectory";

        public IOutgoingConnectionPoint Create(Dictionary<string, string> parameters,
            IServiceLocator serviceLocator)
        {
            if (!parameters.ContainsKey(OUTPUT_DIRECTORY_PARAMETER))
            {
                throw new ArgumentException(string.Format("Не задан параметр <{0}>",
                    OUTPUT_DIRECTORY_PARAMETER));
            }
            string outputDirectory = parameters[OUTPUT_DIRECTORY_PARAMETER];

            return new OutgoingConnectionPoint(outputDirectory, serviceLocator);
        }
    }
}
