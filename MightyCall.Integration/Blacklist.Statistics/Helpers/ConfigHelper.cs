using System;
using System.Configuration;

namespace crmPark.Blacklist.Statistics.Helpers
{
    /// <summary>
    /// Вспомогательный класс для получения значения из конфигурационного файла
    /// </summary>
    public class ConfigHelper
    {

        /// <summary>
        /// Дата последнего обновления в CRM
        /// </summary>
        public string Date { get;}

        /// <summary>
        /// Путь к лог-файлу для обработки.
        /// </summary>
        public string LogPath { get; }

        /// <summary>
        /// Строка подключения к CRM
        /// </summary>
        public string ConnectionsString { get; }

        /// <summary>
        /// Конструктор вспомогательного класса для получения значений из конфигурационного файла
        /// </summary>
        public ConfigHelper()
        {
            Date = ConfigurationManager.AppSettings["LAST_DATE_UPDATE"] != null ? ConfigurationManager.AppSettings.Get("LAST_DATE_UPDATE") : "01.05.2025";
            LogPath = ConfigurationManager.AppSettings["LOG_PATH"] != null ? ConfigurationManager.AppSettings.Get("LOG_PATH") : "";
            if (ConfigurationManager.ConnectionStrings["CRM_KORTROS"] == null)
            {
                throw new Exception("Нет строки соединения в конфигурационном файле, параметр CRM_KORTROS");
            }
            ConnectionsString = ConfigurationManager.ConnectionStrings["CRM_KORTROS"].ConnectionString;
        }

        /// <summary>
        /// Метод для обновления параметров в конфигурационном файле
        /// </summary>
        /// <param name="key">Имя параметра</param>
        /// <param name="value">Значение параметра</param>
        /// <remarks>Метод обновляет только конфигурационные параметры (не строки подключения)</remarks>
        public void SetConfigValue(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            // Если в конфигурационном файле нет параметра с переданным именем, он будет создан, иначе — обновлён.
            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, value);
            }
            else
            {
                config.AppSettings.Settings[key].Value = value;
            }
            // Производим сохранение конфигурационного файла
            config.Save(ConfigurationSaveMode.Modified);
            // обновляем секцию appSettings
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
