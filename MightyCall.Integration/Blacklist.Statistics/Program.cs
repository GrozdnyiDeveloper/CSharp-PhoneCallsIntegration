using crmPark.Blacklist.Statistics.Helpers;
using crmPark.Blacklist.Statistics.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace crmPark.Blacklist.Statistics
{
    /// <summary>
    /// Основной класс программы
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Инициализация экземпляра логгера
        /// </summary>
        public static Logger _logger = LogManager.GetCurrentClassLogger();
        public static void Main(string[] args)
        {
            // Инициализация вспомогательного объекта для получения конфигурации
            var configHelper = new ConfigHelper();
            // Инициализация вспомогательного объекта для работы с CRM
            var crmHelper = new CRMHelper(_logger, configHelper.ConnectionsString);
            // Инициализация словаря для временного хранеия статистики по звонкам
            Dictionary<string, StatsData> CallStats = new Dictionary<string, StatsData>();
            _logger.Info($"Начало работы приложения. Время: {DateTime.Now}");
            try
            {
                // Производим конвертацию даты из строки в тип DateTime
                DateTime startDate = StringToDate(configHelper.Date);
                // Производим чтение и парсинг лог-файла
                CallStats = ProcessReadLog(configHelper.LogPath, startDate);
                _logger.Info($"Лог-файл обработан, получено: {CallStats.Count} номеров");
                // Начинаем обновление сущностей "Номер телефона"
                crmHelper.UpdateAllPhoneNumber(CallStats);
                // Обновляем дату последней отправки статистики по звонкам в CRM
                configHelper.SetConfigValue("LAST_DATE_UPDATE", DateTime.Now.ToString("dd.MM.yyyy"));
            }
            catch(Exception ex)
            {
                StringBuilder errorMessage = new StringBuilder($"Ошибка при выполнении метода Main. Сведения об ошибке: {ex.Message}, Трейс: {ex.StackTrace}");
                Exception inner = ex.InnerException;
                int level = 1;

                while (inner != null)
                {
                    errorMessage.AppendLine();
                    errorMessage.Append($"Уровень {level} — сообщение внутренней ошибки: {inner.Message}, StackTrace: {inner.StackTrace}");
                    inner = inner.InnerException;
                    level++;
                    //больше 10 уровней не затрагиваем
                    if (level == 10)
                    {
                        inner = null;
                    }
                }
                _logger.Error(errorMessage.ToString());
            }
            
            _logger.Info($"Завершение работы программы");
        }

        /// <summary>
        /// Метод для чтения и парсинга лог-файла
        /// </summary>
        /// <param name="pathLog">Путь к лог-файлу</param>
        /// <param name="startDate">Дата последней отправки данных в CRM</param>
        /// <returns>Словарь, где ключом является номер телефона, а значением — структура с количеством попыток дозвона 
        /// и датой последней такой попытки</returns>
        public static Dictionary<string, StatsData> ProcessReadLog(string pathLog, DateTime startDate)
        {
            // Создаем пустой словарь
            Dictionary<string, StatsData> CallStats = new Dictionary<string, StatsData>();
            // Проверяем существование файла перед попыткой чтения
            if (!File.Exists(pathLog))
            {
                _logger.Info($"Файл лога не найден: {pathLog}");
                return CallStats;
            }
            // Создаем строковый массив для хранения строк лога
            string[] allLines;
            try
            {
                allLines = File.ReadLines(pathLog).ToArray();
            }
            catch (Exception ex)
            {
                _logger.Error($"Произошла ошибка при чтении файла: {ex.Message}");
                return CallStats;
            }
            // Создаем регулярное выражение с шаблоном
            var regex = new Regex(
                @"^Номер найден в черном списке\s*–\s*(.+)\s*–\s*(\d{1,2}\.\d{1,2}\.\d{4})\s+(\d{1,2}:\d{1,2}:\d{1,2})",
                RegexOptions.Compiled | RegexOptions.CultureInvariant
                );

            foreach (var item in allLines)
            {

                try
                {
                    string line = item;
                    Match match = regex.Match(line);
                    // Если регулярное выражение успешно применено, производим запись в словарь
                    if (match.Success)
                    {
                        // Получаем номер телефона
                        string number = GetLastTenDigits(match.Groups[1].Value);
                        // Производим конвертацию даты из строки в тип DateTime
                        DateTime dateTime = StringToDate($"{match.Groups[2].Value} {match.Groups[3].Value}");
                        // Если дата последнего обновления в CRM меньше, чем дата, полученная из строки лога, то добавляем запись в словарь
                        if (startDate < dateTime)
                        {
                            // Пытаемся получить значение по ключу.
                            // Если значение существует — сравниваем дату, иначе — добавляем новое.
                            if (CallStats.TryGetValue(number, out StatsData existingData))
                            {
                                // Увеличиваем счётчик попыток дозвона номеров
                                existingData.CountCalls++;
                                // Сравниваем даты и при необходимости обновляем на более новую
                                if (existingData.LastCallDate < dateTime)
                                {
                                    existingData.LastCallDate = dateTime;
                                }
                            }
                            else
                            {
                                CallStats.Add(number, new StatsData(1, dateTime));
                            }
                        }
                    }

                }
                catch(Exception ex)
                {
                    _logger.Error($"Произошла ошибка при попытке парсинга лог-файла в строке: \n{item}\nОписание ошибки: {ex.Message}");
                }
            }
            return CallStats;
        }
        
        /// <summary>
        /// Метод для конвертации строкового представления даты в тип DateTime
        /// </summary>
        /// <param name="dateString">Строковое представление даты</param>
        /// <returns>Объект DateTime, соответствующий переданной строке</returns>
        private static DateTime StringToDate(string dateString)
        {
            // Массив возможных форматов
            string[] formats = {
                "dd.MM.yyyy H:mm:ss",
                "d.MM.yyyy H:mm:ss",
                "dd.M.yyyy H:mm:ss",
                "d.M.yyyy H:mm:ss",
                "dd.MM.yyyy HH:mm:ss",
                "d.MM.yyyy HH:mm:ss",
                "dd.M.yyyy HH:mm:ss",
                "d.M.yyyy HH:mm:ss",
                "dd.M.yyyy"
            };

            return DateTime.ParseExact(
                dateString,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None
            );
        }

        /// <summary>
        /// Метод возвращает последние 10 цифр номера телефона.
        /// </summary>
        /// <param name="phoneNumber">Номер телефона.</param>
        /// <returns>
        /// Последние 10 цифр номера телефона. 
        /// Если переданный номер короче 10 цифр, возвращается он полностью.
        /// </returns>
        private static string GetLastTenDigits(string phoneNumber)
        {
            // Если переданный номер не содержит данных, возвращаем пустую строку
            if (string.IsNullOrEmpty(phoneNumber))
                return string.Empty;
            // Оставляем в строке только цифровые символы
            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
            // Если длина строки номера превышает 10 символов, возвращаем подстроку из последних 10 символов
            // В противном случае возвращаем исходную строку
            return digitsOnly.Length >= 10 ? digitsOnly.Substring(digitsOnly.Length - 10) : digitsOnly;
        }
    }
}
