using System;

namespace crmPark.Blacklist.Statistics.Models
{

    /// <summary>
    /// Модель данных для сбора статистики
    /// </summary>
    public class StatsData
    {

        /// <summary>
        /// Количество дозвонов
        /// </summary>
        public int CountCalls { get; set; }

        /// <summary>
        /// Дата последней попытки дозвона
        /// </summary>
        public DateTime LastCallDate { get; set; }

        /// <summary>
        /// Конструктор модели данных для сбора статистики.
        /// </summary>
        /// <param name="countCalls">Количество дозвонов.</param>
        /// <param name="lastCallDate">Дата последней попытки дозвона.</param>
        public StatsData(int countCalls, DateTime lastCallDate)
        {
            CountCalls = countCalls;
            LastCallDate = lastCallDate;
        }
    }
}
