namespace AIAssistantEndpoint.Configuration
{
    using System;
    using System.IO;
    using System.Xml.Serialization;
    using AIAssistantEndpoint.Settings;

    public interface IConfigurationManager
    {
        void SaveSettings(ServerConnectionSettings settings);
        ServerConnectionSettings LoadSettings();
        bool SettingsExist();
        void DeleteSettings();
    }
}
