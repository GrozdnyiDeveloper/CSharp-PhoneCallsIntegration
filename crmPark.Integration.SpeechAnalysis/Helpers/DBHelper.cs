using crmPark.Integration.SpeechAnalysis.DBDataClasses;
using crmPark.Integration.SpeechAnalysis.Helpers;
using NLog;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace crmPark.Integration.SpeechAnalysis
{
    class DBHelper
    {
        private NpgsqlConnection npgsqlConnection;
        private static Logger Logger;
        public DBHelper(string connString, Logger logger)
        {
            this.npgsqlConnection = new NpgsqlConnection(connString);
            Logger = logger;
        }
        /// <summary>
        /// Метод открытия подключения к БД интеграции
        /// </summary>
        /// <returns></returns>
        public async Task OpenConnection()
        {
            try
            {
                // Открываем подключение к БД интеграции
                await npgsqlConnection.OpenAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Возникла ошибка при открытии подключения к БД интеграции: {ex?.Message}\n{ex?.StackTrace}\n{ex?.InnerException}");
            }
        }
        /// <summary>
        /// Метод получения записей речевой аналитики из БД интеграции
        /// </summary>
        /// <returns></returns>
        public async Task<List<Transcription>> GetTranscriptions(string CURRENT_TRANSCRIPTION_SOURCE)
        {
            List<Transcription> result = new List<Transcription>();

            try
            {
                // Получаем текст запроса из файла transcriptions.sql на получение записей речевой аналитики
                string sqlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "transcriptions.sql");
                string sqlQuery = File.ReadAllText(sqlFilePath).Replace("{transctiption_source}", CURRENT_TRANSCRIPTION_SOURCE);

                // Отправляем команду на получение речевой аналитики в БД
                using (var cmd = new NpgsqlCommand(sqlQuery, npgsqlConnection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        // Для каждой полученной записи
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                // Записываем данные в соответствующий объект и добавляем в результат
                                var transcript = new Transcription()
                                {
                                    task_id = DataReaderHelper.GetValue<int>(reader.GetValue(0)),
                                    call_id = DataReaderHelper.GetNullableValue<long>(reader.GetValue(1)),
                                    transcription_raw = DataReaderHelper.GetValue<string>(reader.GetValue(2)),
                                    transcription_enhanced = SanitizeForCrm(DataReaderHelper.GetValue<string>(reader.GetValue(3))),
                                    duration = DataReaderHelper.GetNullableValue<decimal>(reader.GetValue(4)),
                                    language = DataReaderHelper.GetValue<string>(reader.GetValue(5)),
                                    config = DataReaderHelper.GetValue<string>(reader.GetValue(6)),
                                    created_at = DataReaderHelper.GetNullableValue<DateTime>(reader.GetValue(7)),
                                    summary = SanitizeForCrm(DataReaderHelper.GetValue<string>(reader.GetValue(8))),
                                    is_processed = DataReaderHelper.GetNullableValue<bool>(reader.GetValue(9)),
                                };
                                result.Add(transcript);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"Возникла ошибка при получении данных речевой аналитики из БД интеграции: {ex?.Message}\n{ex?.StackTrace}\n{ex?.InnerException}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Возникла ошибка при отправке запроса в БД интеграции на получение данных речевой аналитики: {ex?.Message}\n{ex?.StackTrace}\n{ex?.InnerException}");
            }

            // Возвращаем полученный список записей речевой аналитики
            return result;
        }
        /// <summary>
        /// Внутренний метод для очистки строки от не воспринимаемых в CRM управляющих символов
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static string SanitizeForCrm(string input)
        {
            // Если строка пустая, то просто возвращаем её обратно
            if (string.IsNullOrEmpty(input))
                return input;

            // Проходим по всем символам полученной строки
            var sanitized = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                // Если символ является управляющим символом и не разрешён (не табуляция или перенос строки)
                if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
                {
                    // Пропускаем его
                    continue;
                }

                // Добавляем символ в итоговую строку
                sanitized.Append(c);
            }

            // Возвращаем итоговую строку
            return sanitized.ToString();
        }
        /// <summary>
        /// Метод переключения флага по всем обработанным записям речевой аналитики в БД интеграции
        /// </summary>
        /// <param name="transcriptions">Записи речевой аналитики</param>
        /// <returns></returns>
        public async Task ProcessTranscriptions(List<Transcription> transcriptions, string CURRENT_TRANSCRIPTION_SOURCE)
        {
            List<int> taskIds = new List<int>();

            try
            {
                // Сохраняем ID всех полученных записей речевой аналитики
                foreach (var transcription in transcriptions)
                {
                    taskIds.Add(transcription.task_id);
                }

                var taskIdsString = string.Join(", ", taskIds);

                // Добавляем все ID записей в запрос
                string sqlQuery = $"UPDATE {CURRENT_TRANSCRIPTION_SOURCE}.transcriptions SET is_processed = true WHERE task_id IN ({taskIdsString})";

                // Отправляем запрос в БД интеграции
                using (var cmd = new NpgsqlCommand(sqlQuery, npgsqlConnection))
                {
                    await cmd.ExecuteNonQueryAsync();
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Возникла ошибка при отправке запроса в БД интеграции на переключение флагов обработки: {ex?.Message}\n{ex?.StackTrace}\n{ex?.InnerException}");
            }
        }
    }
}
