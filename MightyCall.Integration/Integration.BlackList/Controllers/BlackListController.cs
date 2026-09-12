using crmPark.Integration.External.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace crmPark.Integration.External.Controllers
{
    public class BlackListController : ApiController
    {
        private readonly string _crmUrl = WebConfigHelper.CrmUrl;
        private readonly string _logPath = WebConfigHelper.LogPath;
        // POST BlackList
        [Route("api/blacklist")]
        [HttpPost]
        public IHttpActionResult Post()
        {
            try
            {
                // Формируем клиента, от которого будем посылать запросы
                var httpClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });

                // Формируем и шлём в CRM запрос на получение номеров черного списка
                var requestUrl = _crmUrl + "crmpark_phonenumbers?$select=crmpark_number&$filter=crmpark_isblocknumber eq true and statecode eq 0";
                var blacklist = new HashSet<string>();
                var errorText = string.Empty;

                while (!string.IsNullOrEmpty(requestUrl))
                {
                    //   var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                    // request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Получаем результат запроса
                    var response = httpClient.GetAsync(requestUrl).Result;
                    response.EnsureSuccessStatusCode();

                    var json = response.Content.ReadAsStringAsync().Result;
                    var result = JsonConvert.DeserializeObject<JObject>(json);

                    if (response.IsSuccessStatusCode)
                    {
                        // Получаем и формируем в HashSet
                        var items = result["value"];

                        foreach (var item in items)
                        {
                            blacklist.Add(item["crmpark_number"].ToString());
                        }
                    }
                    else
                    {
                        errorText = (string)result["error"];
                    }

                    requestUrl = String.Empty;
                    if (result["@odata.nextLink"] != null)
                    {
                        requestUrl = (string)result["@odata.nextLink"];
                    }
                }

                var currentTimeStamp = DateTime.Now.TimeOfDay.ToString().Replace(':', '.');

                // Если результат запроса удачный
                if (string.IsNullOrEmpty(errorText))
                {
                    // Создаём копию CSV файла для хранения черного списка и заполняем его
                    using (var streamWriter = new StreamWriter(_logPath + $"Blacklist_{currentTimeStamp}.csv", true))
                    {
                        foreach (var number in blacklist)
                        {
                            streamWriter.WriteLine(number);
                        }
                    }

                    if (WebConfigHelper.TerminateRequestsOnNewBLNumbers)
                    {
                        var oldBlacklist = new HashSet<string>();
                        using (var streamReader = new StreamReader(_logPath + "Blacklist.csv"))
                        {
                            string line;
                            while ((line = streamReader.ReadLine()) != null)
                            {
                                var number = line.Trim();
                                if (!string.IsNullOrEmpty(number))
                                    oldBlacklist.Add(number);
                            }
                        }
                        foreach(var number in blacklist)
                        {
                            if (!oldBlacklist.Contains(number))
                            {
                                terminateRequest(number);
                            }
                        }
                    }

                    // Удаляем текущий файл с черным списком и заменяем его новосозданным через переименование
                    System.IO.File.Delete(_logPath + "Blacklist.csv");
                    System.IO.File.Move(_logPath + $"Blacklist_{currentTimeStamp}.csv", _logPath + "Blacklist.csv");

                    // Возвращаем удачный результат
                    var result = new
                    {
                        Success = true,
                        Message = "Запрос успешно обработан"
                    };

                    return Json(result);
                }
                else
                {
                    // Возвращаем неудачный результат
                    // return BadRequest(JsonConvert.SerializeObject(errorText));
                    var result = new
                    {
                        Success = false,
                        Message = $"Запрос обработан с ошибкой: не удалось получить данные по номерам ЧС: {errorText}"
                    };

                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                // Возвращаем неудачный результат
                var result = new
                {
                    Success = false,
                    Message = $"Запрос обработан с ошибкой: {ex.Message} {ex.StackTrace}."
                };

                return Json(result);
            }
        }
        /// <summary>
        /// Метод удаления заявки из всех очередей
        /// </summary>
        /// <param name="phoneNumber">Номер телефона</param>
        private void terminateRequest(string phoneNumber)
        {
            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
            // Получение списка заявок
            List<Guid> SessionsId = DBHelper.GetSessionsId(digitsOnly.Length >= 10
                ? digitsOnly.Substring(digitsOnly.Length - 10) : digitsOnly, null);
            MightyCallHelper mightyCallHelper = new MightyCallHelper();
            // Вызываем метод API для удаления каждой заявки из списка
            foreach (Guid id in SessionsId) 
                mightyCallHelper.TerminateRequest(id);
        }
    }
}