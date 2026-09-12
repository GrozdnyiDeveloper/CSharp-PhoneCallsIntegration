using crmPark.Blacklist.Statistics.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using NLog;
using System;
using System.Collections.Generic;

namespace crmPark.Blacklist.Statistics.Helpers
{

    /// <summary>
    /// Класс для работы с CRM
    /// </summary>
    public class CRMHelper
    {

        /// <summary>
        /// Логгер
        /// </summary>
        private static Logger _logger;

        /// <summary>
        /// Строка подключения
        /// </summary>
        private string _connString { get; set; }

        /// <summary>
        /// Конструктор класса для работы с CRM
        /// </summary>
        /// <param name="logger">Логгер</param>
        /// <param name="connString">Строка подключения</param>
        public CRMHelper(Logger logger, string connString)
        {
            _logger = logger;
            _connString = connString;
        }

        /// <summary>
        /// Подключение к CRM
        /// </summary>
        private CrmServiceClient ConnectCRM()
        {
            if (_connString == "")
            {
                _logger.Error("Пустая строка подключения в конфигурационном файле, параметр CRM_KORTROS");
                throw new Exception("Пустая строка подключения в конфигурационном файле, параметр CRM_KORTROS");
            }

            CrmServiceClient crmServiceClient = new CrmServiceClient(_connString);
            crmServiceClient.OrganizationServiceProxy.Timeout = new TimeSpan(0, 10, 0);

            return crmServiceClient;
        }

        /// <summary>
        /// Метод для отправки статистики в CRM.
        /// </summary>
        /// <param name="CallStats">Словарь, содержащий заблокированные номера телефонов и статистику по ним.</param>
        public void UpdateAllPhoneNumber(Dictionary<string, StatsData> CallStats)
        {
            _logger.Info("Начат процесс передачи статистики в CRM");
            // Инициализация переменной для хранения количества успешных отправок данных в CRM
            int correctUpdate = 0;
            foreach (var call in CallStats)
            {
                try
                {
                    // Открываем соединение с CRM
                    using (var service = ConnectCRM())
                    {
                        // Получение активной записи сущности "Номер телефона"
                        var phoneNumber = GetEntityPhoneNumber(call.Key, service);
                        // Проверка, что полученная запись содержит данные
                        if (phoneNumber != null)
                        {
                            // Если полученная запись содержит поле crmpark_callattempts, то производим увеличение счётчика попыток дозвона
                            int count = phoneNumber.Contains("crmpark_callattempts") ? phoneNumber.GetAttributeValue<int>("crmpark_callattempts") : 0;
                            phoneNumber["crmpark_callattempts"] = count + call.Value.CountCalls;

                            phoneNumber["crmpark_lastcalldate"] = call.Value.LastCallDate;
                            // Обновляем запись сущности "Номер телефона"
                            service.Update(phoneNumber);
                            _logger.Info($"Номер телефона {call.Key} успешно обновлён в CRM ({phoneNumber.Id})");
                            correctUpdate++;
                        }
                        else
                        {
                            _logger.Info($"Не удалось найти номер {call.Key}");
                        }
                    }
                }
                catch(Exception ex)
                {
                    _logger.Error($"При попытке обновления номера телефона ({call.Key}) произошла ошибка: {ex.Message}");
                }
            }
            _logger.Info($"Окончание передачи статистики. В процессе работы приложением были успешно обновлены {correctUpdate}/{CallStats.Count} номера телефонов");
        }
        /// <summary>
        /// Метод для получения активной записи сущности "Номер телефона" по переданному номеру.
        /// </summary>
        /// <param name="phoneNumber">Номер телефона</param>
        /// <param name="CrmServiceClient">Сервис для выполнения операций в CRM</param>
        /// <returns>Активная запись сущности, если она найдена; в противном случае — null</returns>
        private Entity GetEntityPhoneNumber(string phoneNumber, CrmServiceClient CrmServiceClient)
        {
            QueryExpression query = new QueryExpression("crmpark_phonenumber");
            query.ColumnSet = new ColumnSet("crmpark_number", "crmpark_callattempts", "crmpark_lastcalldate", "crmpark_phonenumberid");
            query.Criteria.AddCondition("crmpark_isblocknumber", ConditionOperator.Equal, true);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            query.Criteria.AddCondition("crmpark_number", ConditionOperator.Like, $"%{phoneNumber}%");
            query.AddOrder("createdon", OrderType.Descending);
            var result = CrmServiceClient.RetrieveMultiple(query);
            if(result != null && result?.Entities != null && result.Entities.Count > 0)
            {
                return result.Entities[0];
            }
            else
            {
                return null;
            }
        }
    }
}
