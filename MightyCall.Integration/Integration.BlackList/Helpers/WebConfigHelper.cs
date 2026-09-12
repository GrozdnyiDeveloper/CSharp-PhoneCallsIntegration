using System.Web.Configuration;

namespace crmPark.Integration.External.Helpers
{
    /// <summary>
    /// Класс, инкапсулирующий получение параметров из WebConfig
    /// </summary>
    public static class WebConfigHelper
    {
        #region Получение данных из WebConfig
        private readonly static string mightyCallBaseUrl = WebConfigurationManager.AppSettings["MCBaseUrl"];
        private readonly static string campaingId = WebConfigurationManager.AppSettings["Campaingid"];
        private readonly static string logPath = WebConfigurationManager.AppSettings["CSVPath"];
        private readonly static string crmUrl = WebConfigurationManager.ConnectionStrings["CrmUrl"].ConnectionString;
        private readonly static bool terminateRequestsOnNewBLNumbers = bool.Parse(WebConfigurationManager.AppSettings["TerminateRequestsOnNewBLNumbers"] ?? "false");
        #endregion

        /// <summary>
        /// Значение базового URL MightyCall
        /// </summary>
        public static string MightyCallBaseUrl
        {
            get => mightyCallBaseUrl;
        }
        /// <summary>
        /// GUID очереди (компании) для автоперезвона
        /// </summary>
        public static string Campaingid
        {
            get => campaingId;
        }
        /// <summary>
        /// Путь к CSV-файлу
        /// </summary>
        public static string LogPath
        {
            get => logPath;
        }
        /// <summary>
        /// Url адрес CRM
        /// </summary>
        public static string CrmUrl
        {
            get => crmUrl;
        }
        /// <summary>
        /// Удалять заявки при попадании номера в ЧС.
        /// true - удалять, false - не удалять.
        /// </summary>
        public static bool TerminateRequestsOnNewBLNumbers
        {
            get => terminateRequestsOnNewBLNumbers;
        }
    }
}