using crmPark.Integration.SpeechAnalysis.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace crmPark.Integration.SpeechAnalysis.Helpers
{
    class CRMHelper
    {
        private CrmServiceClient _crmServiceClient;

        public CRMHelper(string connString)
        {
            this._crmServiceClient = new CrmServiceClient(connString);
        }
        /// <summary>
        /// Метод получения значения из записи настроек CRM
        /// </summary>
        /// <param name="name">Название записи настроек</param>
        /// <returns></returns>
        public string GetSetting(string name)
        {
            // Формируем запрос на получение записи Настроек по переданному имени
            var query = new QueryExpression("crmpark_setting");
            query.ColumnSet = new ColumnSet("crmpark_value");
            query.Criteria.AddCondition("crmpark_name", ConditionOperator.Equal, name);
            var result = _crmServiceClient.RetrieveMultiple(query);

            // Если получили результат
            if (result != null && result[0] != null && result[0]["crmpark_value"] != null)
                // То возвращаем его
                return (string)result[0]["crmpark_value"];
            else
                // Иначе возвращаем пустую строку
                return string.Empty;
        }
        /// <summary>
        /// Метод отправки запроса в CRM и возврата результата его работы
        /// </summary>
        /// <param name="request">Данные запроса</param>
        /// <param name="jsonString">Данные в формате JSON</param>
        /// <returns></returns>
        public (Guid, int, string) SendRequest(Request request, string jsonString)
        {
            // Формируем запрос на создание Запроса в CRM
            Entity createRequest = new Entity("crmpark_request");
            createRequest["crmpark_name"] = request.name;
            createRequest["crmpark_destination"] = request.destination;
            createRequest["crmpark_is_sync"] = request.isSync;
            createRequest["crmpark_in_body"] = jsonString;
            var newRequestGUID = _crmServiceClient.Create(createRequest);

            // Получаем и возвращаем результат обработки Запроcа из CRM
            var result = _crmServiceClient.Retrieve("crmpark_request", newRequestGUID, new ColumnSet("crmpark_code", "crmpark_out_body"));
            return (newRequestGUID, result.GetAttributeValue<int>("crmpark_code"), result.GetAttributeValue<string>("crmpark_out_body"));
        }
        /// <summary>
        /// Метод вызова действия в CRM по отправке сообщения об ошибке группе Администраторов
        /// </summary>
        /// <param name="result">Результат обработки запроса</param>
        /// <returns></returns>
        public OrganizationResponse SendErrorMessage(string result)
        {
            // Формируем запрос на вызов действия по отправке сообщения об ошибке группе Администраторов
            OrganizationRequest request = new OrganizationRequest("crmpark_SendNotificationSpeechAnalysis");
            request["ResultJson"] = result;
            OrganizationResponse response = _crmServiceClient.Execute(request); 

            // Возвращаем результат обработки запроса
            return response;
        }
    }
}
