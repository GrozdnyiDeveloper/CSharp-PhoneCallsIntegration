using NLog;
using NLog.Config;
using NLog.Targets;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace crmPark.Integration.SpeechAnalysis
{
    class Program
    {
        private static readonly string CRM_BASE_URL = ConfigurationManager.AppSettings["CRM_BASE_URL"];
        private static readonly string CURRENT_TRANSCRIPTION_SOURCE = ConfigurationManager.AppSettings["CURRENT_TRANSCRIPTION_SOURCE"];
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static int _totalTranscriptionsCount = 0;
        private static int _totalRequestsCount = 0;
        private static int _sentRequestsCount = 0;
        private static int _failedSentRequestsCount = 0;
        private static int _successRequestsCount = 0;
        private static int _failedRequestsCount = 0;
        private static int _errorRequestsCount = 0;
        static async Task Main(string[] args)
        {
            {
                try
                {
                    _logger.Info("Запуск приложения интеграции с базой данных речевой аналитики");
                    var main = new MainLogic(_logger);

                    // Формирование строки подключения и подключение к PostgreSQL
                    var connString = main.GetPostgreConnectionString();
                    if (connString != null)
                    {
                        // Открываем подключение к БД интеграции
                        var db = new DBHelper(connString, _logger);
                        await db.OpenConnection();

                        // Получаем записи речевой аналитики 
                        var transcriptions = await db.GetTranscriptions(CURRENT_TRANSCRIPTION_SOURCE);
                        _totalTranscriptionsCount = transcriptions.Count;
                        _logger.Info($"Было получено {_totalTranscriptionsCount} записей речевой аналитики из БД интеграции");

                        if (transcriptions.Count > 0)
                        {
                            var resultErrors = new StringBuilder();
                            
                            // Разделяем список полученных записей речевой аналитики на порции по 100 записей
                            var transcriptionChunks = main.SplitByChunk(transcriptions, 100);
                            _totalRequestsCount = transcriptionChunks.Count;

                            // Для каждой полученной порции
                            foreach (var chunk in transcriptionChunks)
                            {
                                // Формируем запрос на отправку в CRM с данными в виде JSON строки
                                (var request, var jsonString) = main.GetFormatedJSON(chunk);

                                if (request != null && jsonString != null)
                                {
                                    // Отправляем запрос
                                    (var requestGuid, var code, var result) = main.SendRequest(request, jsonString);

                                    // Если результат обработки запроса положителен
                                    if (code != -1)
                                    {
                                        // Вызываем метод переключения флага по обработанным записям речевой аналитики
                                        await db.ProcessTranscriptions(chunk, CURRENT_TRANSCRIPTION_SOURCE);
                                        _sentRequestsCount++;
                                    }

                                    // Если возникли ошибки при отправке/обработке запроса
                                    if (code != 0)
                                    {
                                        // Если при отправке
                                        if (code == -1)
                                        {
                                            // То добавляем ошибку без указания конкретного запроса 
                                            resultErrors.AppendLine("При формировании запроса возникла следующие ошибки: ");
                                            _failedSentRequestsCount++;
                                        } 
                                        else
                                        {
                                            // Иначе указываем конкретный запрос
                                            var url = $"{CRM_BASE_URL}/main.aspx?pagetype=entityrecord&etn=crmpark_request&id={requestGuid}";
                                            // Если запрос частично успешный
                                            if (code == 1)
                                            {
                                                // То добавляем соотвтствующее сообщение
                                                resultErrors.Append($"При обработке запроса {url} не удалось сопоставить часть данных в CRM: \n");
                                                _failedRequestsCount++;
                                            } 
                                            else
                                            {
                                                // Иначе добавляем ошибку
                                                resultErrors.Append($"При обработке запроса {url} произошли следующие ошибки: \n");
                                                _errorRequestsCount++;
                                            }
                                            
                                        }
                                        resultErrors.AppendLine(result);
                                        resultErrors.Append("\n");
                                    }
                                    else
                                    {
                                        _successRequestsCount++;
                                    }
                                }
                            }

                            // Если возникли ошибки при работе программы
                            if (resultErrors.Length > 0)
                            {
                                main.SendErrorMessage(resultErrors.ToString());
                            }

                            _logger.Info("Успешное завершение работы приложения интеграции с базой данных речевой аналитики \n" +
                                $"Всего получено записей речевой аналитики: {_totalTranscriptionsCount}; \n" +
                                $"Всего сформированных запросов: {_totalRequestsCount}; \n" +
                                $"Успешно отправленных запросов: {_sentRequestsCount}, неуспешно: {_failedSentRequestsCount}; \n" +
                                $"Полностью успешно обработанных запросов: {_successRequestsCount}, частично успешных (часть звонков не удалось сопоставить в CRM): {_failedRequestsCount}, с ошибками: {_errorRequestsCount}; ");
                            return;
                        } 
                        else
                        {
                            _logger.Info("Завершение работы приложения в связи с отсутсвием записей для обработки");
                            return;
                        }
                    }
                    _logger.Warn("Преждевременное завершение работы приложения");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Возникла непредвиденная ошибка при работе приложения интеграции: {ex?.Message}\n{ex?.StackTrace}\n{ex?.InnerException}");
                }
            }
        }
    }
}
