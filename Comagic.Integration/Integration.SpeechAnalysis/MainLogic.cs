using crmPark.Integration.SpeechAnalysis.DBDataClasses;
using crmPark.Integration.SpeechAnalysis.Helpers;
using crmPark.Integration.SpeechAnalysis.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using NLog;

namespace crmPark.Integration.SpeechAnalysis
{
    class MainLogic
    {
        private static readonly string CRM_CONNECTION_STRING = ConfigurationManager.ConnectionStrings["CRM_CONNECTION_STRING"].ConnectionString;
        private static readonly string POSTGRE_CONNECTION_STRING = ConfigurationManager.ConnectionStrings["POSTGRE_CONNECTION_STRING"].ConnectionString;
        private static readonly CRMHelper crmHelper = new CRMHelper(CRM_CONNECTION_STRING);
        private static Logger Logger;
        public MainLogic(Logger logger)
        {
            Logger = logger;
        }
        /// <summary>
        /// Метод по получению данных из CRM и формирования строки подключения к PostgreSQL
        /// </summary>
        /// <returns></returns>
        public string GetPostgreConnectionString()
        {
            try
            {
                // Получаем логин и пароль для БД PostgreSQL из записей настроек CRM 
                var username = crmHelper.GetSetting("SPEECH_LOGIN");
                var password = crmHelper.GetSetting("SPEECH_PASSWORD");

                // Формируем и возвращаем строку подключения к БД PostgreSQL
                // на основе данных из файла конфигурации и полученных данных авторизации 
                var resultString = POSTGRE_CONNECTION_STRING.Replace("{username}", username).Replace("{password}", password);
                return resultString;
            }
            catch (Exception ex)
            {
                Logger.Error($"Возникла ошибка при формировании строки подключения к БД интеграции: {ex?.Message}\n{ex?.StackTrace}\n{ex?.InnerException}"); 
                return null;
            }
        }
        /// <summary>
        /// Метод разделения массива транскрипций на порции определённой длины
        /// </summary>
        /// <param name="allTranscriptions">Полный массив транскрипций</param>
        /// <param name="chunkSize">Размер каждой порции</param>
        /// <returns></returns>
        public List<List<Transcription>> SplitByChunk(List<Transcription> allTranscriptions, int chunkSize)
        {
            var result = new List<List<Transcription>>();

            // Берём из общего массива Транскрипций порции равные заданному размеру
            // или меньше, пока в массиве содержатся записи
            for (int i = 0; i < allTranscriptions.Count; i += chunkSize)
            {
                // Добавляем порции в итог
                result.Add(allTranscriptions.Skip(i).Take(chunkSize).ToList());
            }

            // Возвращаем поделённый на порции список Транскрипций
            return result;
        }
        /// <summary>
        /// Метод формирования запроса и преобразования данных в JSON строку
        /// </summary>
        /// <param name="transcriptions"></param>
        /// <returns></returns>
        public (Request, string) GetFormatedJSON(List<Transcription> transcriptions)
        {
            try 
            { 
                // Формируем запрос по шаблону
                var request = new Request()
                {
                    name = "ProcessSpeechAnalysis",
                    destination = "CRM",
                    isSync = true,
                    Data = new Data()
                    {
                        transcriptions = transcriptions
                    }
                };

                // Задаём параметры сериализации данных в JSON для корректной обработки кирилицы
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                };

                // Преобразуем данные в JSON и возвращаем
                var JSONResult = JsonSerializer.Serialize(request, options);
                return (request, JSONResult);
            }
            catch (Exception ex)
            {
                Logger.Error($"Возникла ошибка при преобразовании данных запроса в JSON: {ex?.Message}\n{ex?.StackTrace}\n{ex?.InnerException}"); 
                return (null, null);
            }
}
        /// <summary>
        /// Метод обработки создания запроса в CRM
        /// </summary>
        /// <param name="request">Данные запроса</param>
        /// <param name="jsonString">Данные в JSON формате</param>
        public (Guid, int, string) SendRequest(Request request, string jsonString)
        {
            try
            { 
                // Вызываем метод по отправке запроса в CRM
                (var requestGuid, var code, var result) = crmHelper.SendRequest(request, jsonString);

                // Если код результата не был равен 0
                if (code != 0)
                {
                    // Фиксируем в логах соответствующую ошибку
                    if (code == 1)
                    {
                        Logger.Warn($"При обработке запроса в CRM и поиска соответствующих звонков не все звонки были найдены: {result}");
                    }
                    else
                    {
                        Logger.Warn($"При обработке запроса в CRM и поиска соответствующих звонков произошла непредвиденная ошибка: {result}");
                    }
                }

                // При удачной отправке данных в CRM возвращаем положительный результат
                return (requestGuid, code, result);
            }
            catch (Exception ex)
            {
                var resultError = $"{ ex?.Message }\n{ ex?.StackTrace}\n{ ex?.InnerException}";
                Logger.Error($"Возникла ошибка при отправке запроса в CRM: {resultError}");
                return (Guid.Empty, -1, resultError);
            }
        }
        /// <summary>
        /// Метод обработки отправки сообщения об ошибке администраторам
        /// </summary>
        /// <param name="result">Общий результат запуска</param>
        public void SendErrorMessage(string result)
        {
            // Вызываем метод по отправке сообщения об ошибке администраторам
            crmHelper.SendErrorMessage(result);
        }
    }
}
