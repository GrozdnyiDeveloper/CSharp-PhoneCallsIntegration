using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using crmPark.Integration.External.DataClass;
using crmPark.Integration.External.Helpers;
using NLog;

namespace crmPark.Integration.External.Controllers
{
    public class PhoneController : ApiController
    {
        private readonly Logger logger;
        /// <summary>
        /// Контроллер для обработки взаимодействия с MightyCall
        /// </summary>
        public PhoneController()
        {
            logger = LogManager.GetCurrentClassLogger();
        }
        /// <summary>
        /// Проверяет, находится ли телефон в очереди на перезвон
        /// </summary>
        /// <param name="phoneNumber">Номер телефона</param>
        /// <param name="guidQueue">GUID очереди</param>
        /// <returns>true, если телефон найден в очереди на перезвон; иначе - false</returns>
        [Route("api/isqueue")]
        [HttpGet]
        public IHttpActionResult IsPhoneNumberInQueue(string phoneNumber, Guid? guidQueue = null) 
        {
            logger.Info($"Вызов метода IsPhoneNumberInQueue с параметром {phoneNumber}");
            // Проверка, что полученный параметр не пустой
            if (string.IsNullOrEmpty(phoneNumber))
            {
                logger.Info("Метод IsPhoneNumberInQueue был вызван с пустым номером телефона");
                return Json(new { result = false });
            }
            bool isInQueue = false;
            try
            {
                string lastTenDigit = GetLastTenDigit(phoneNumber);
                if(lastTenDigit != "")
                {
                    isInQueue = DBHelper.IsPhoneNumberInQueue(lastTenDigit, guidQueue);
                    logger.Info($"Вызванный номер {phoneNumber} | Находится в очереди на перезвон? {isInQueue}");
                }
                else
                {
                    logger.Info($"Переданн не корректный номер: {phoneNumber}");
                }
            }
            catch (Exception ex)
            {
                logger.Info($"Произошла ошибка при выполнении метода IsPhoneNumberInQueue: {ex.Message}");
            }
            return Json(new { result = isInQueue }); 
        }

        /// <summary>
        /// Метод удаления заявки по номеру телефона
        /// </summary>
        /// <param name="request">тело запроса</param>
        /// <returns>Возвращает true, если заявка была успешно удалена, иначе возвращает false</returns>
        [Route("api/terminaterequestbyphone")]
        [HttpPost]
        public async Task<IHttpActionResult> TerminateRequestByPhone([FromBody] TerminateRequestModel request)
        {
            logger.Info($"Вызов метода TerminateRequestByPhone с параметром {request.PhoneNumber}");
            var account = System.Security.Principal.WindowsIdentity.GetCurrent();
            logger.Debug($"Вызов метода от лица пользователя: {account.Name}");
            ResponseJson result = new ResponseJson();
            // Проверка, что полученный параметр не пустой
            if (string.IsNullOrEmpty(request.PhoneNumber))
            {
                logger.Info("Метод TerminateRequestByPhone был вызван с пустым номером телефона");
                return Json(new ResponseJson(false, "Метод TerminateRequestByPhone был вызван с пустым номером телефона"));
            }
            try{
                // Оставляем только цифры
                var lastTenDigit = GetLastTenDigit(request.PhoneNumber);
                if(lastTenDigit != "")
                {
                    List<Guid> SessionsID = DBHelper.GetSessionsId(lastTenDigit, request.Campaingid);
                    var res = new Tuple<string, bool>($"Попытка удалить заявку", true);
                    logger.Info($"Получено {SessionsID.Count} активных записей для  номер телефона {request.PhoneNumber}");
                    MightyCallHelper mightyCall = new MightyCallHelper();
                    result = new ResponseJson(true, "");

                    foreach (var sessionID in SessionsID)
                    {
                        
                        res = await mightyCall.TerminateRequest(sessionID);
                        
                        if (res.Item2)
                        {
                            logger.Info($"Заявка {sessionID} была успешно удалена");
                            result.Message += $"Заявка {sessionID} была успешно удалена\n";
                        }
                        else
                        {
                            logger.Error($"Не удалось удалить заявку {sessionID}");
                            result.Message += $"Не удалось удалить заявку {sessionID}\n{res.Item1}";
                        }
                    }
                }
                else
                {
                    logger.Info($"Переданн не корректный номер: {request.PhoneNumber}");
                    result = new ResponseJson(false, $"Переданн не корректный номер: {request.PhoneNumber}");
                }
            }
            catch(Exception ex)
            {
                logger.Info($"Произошла ошибка при выполнении метода TerminateRequestByPhone: {ex.Message}");
                result.Result = false;
                result.Message = $"Произошла ошибка при выполнении метода TerminateRequestByPhone: {ex.Message}";
            }
            return Json(new { result });
        }
        /// <summary>
        /// Получение последних 10 цифр номера телефона
        /// </summary>
        /// <param name="phoneNumber">Номер телефона</param>
        /// <returns>Последние 10 цифр номера телефона</returns>
        private string GetLastTenDigit(string phoneNumber)
        {
            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
            return digitsOnly.Length >= 10
                    ? digitsOnly.Substring(digitsOnly.Length - 10) : digitsOnly;
        }
    }
}