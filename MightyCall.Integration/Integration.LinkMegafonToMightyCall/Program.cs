using NLog;
using System;
using System.Linq;
using System.Collections.Generic;
using crmPark.Integration.LinkMegafonToMightyCall.DBHelper;
using crmPark.Integration.LinkMegafonToMightyCall.DbDataClasses;

namespace crmPark.Integration.LinkMegafonToMightyCall
{
    class Program
    {
        public static Logger _logger = LogManager.GetCurrentClassLogger();
        public static uint linkedCallsCount = 0;
        static void Main(string[] args)
        {
            StartProcessing();
        }

        static void StartProcessing()
        {
            _logger.Info("");
            _logger.Info($"Начинаем обработку записей");
            try
            {
                var helper = new Helper();

                // Получаем звонки Мегафона
                var calls = helper.GetMegafonCallsToUpdate();
                _logger.Info($"Получено звонков Мегафон для проверки: {calls.Count}");
                Console.WriteLine($"Получено звонков Мегафон для проверки: {calls.Count}");

                // Если не были получены звонки Мегафона
                if (calls.Count == 0)
                {
                    // То отмечаем это в логах соответствующим сообщением и завершам работу консоли
                    _logger.Info($"Завершение работы, так как не было найдено звонков Мегафон");
                    Console.WriteLine($"Завершение работы, так как не было найдено звонков Мегафон");
                    return;
                }

                // Сортируем по дате начала звонка и получаем самый новый звонок
                var lastCallDate = calls.OrderBy(x => x.start).FirstOrDefault();

                // Получаем звонки MightyCall
                var callsMCE = helper.GetMightyCallCalls(DateTime.Parse(lastCallDate.start));
                _logger.Info($"Получено звонков MightyCall для проверки: {callsMCE.Count}");
                Console.WriteLine($"Получено звонков MightyCall для проверки: {callsMCE.Count}");

                // Если не были получены звонки MightyCall
                if (callsMCE.Count == 0)
                {
                    // То отмечаем это в логах соответствующим сообщением и завершам работу консоли
                    _logger.Info($"Завершение работы, так как не было найдено звонков MightyCall");
                    Console.WriteLine($"Завершение работы, так как не было найдено звонков MightyCall");
                    return;
                }

                // Вызываем метод обработки звонков
                HandlExternalCalls(calls, callsMCE);
                _logger.Info($"Обработка записей завершена. Сопоставлено номеров: {linkedCallsCount}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке записи. Подробности: {ex.Message + Environment.NewLine + ex.InnerException}");
            }
        }
        private static void HandlExternalCalls(List<Megafon_calls> calls,
            List<Tuple<IV_CallRecord, bool>> callsMCE)
        {
            int i = 0;
            
            foreach (var call in calls.OrderByDescending(x => x.start))
            {
                try
                {
                    // Получаем все подходящие по времени звонки MightyCall
                    var formatedMCECalls = callsMCE.OrderBy(x => x.Item1.StartTime).Where(x => (x.Item1.StartTime >= DateTime.Parse(call.start) && x.Item1.StartTime <= DateTime.Parse(call.start).AddMinutes(2) && x.Item2 == false));

                    // Ищем подходящий по client или diversion звонок
                    var phoneFormated = GetLastTenDigits(call.client);
                    var correspondingCall = formatedMCECalls.FirstOrDefault(x => GetLastTenDigits(x.Item1.OrgNumber) == GetLastTenDigits(call.client)
                    || GetLastTenDigits(x.Item1.OrgNumber) == GetLastTenDigits(call.diversion));

                    // Если нашли подходящий звонок
                    if (correspondingCall != null)
                    {
                        // Указываем ID внешнего звонка
                        call.call_external_id = correspondingCall.Item1.SessionID;

                        // Убираем из выборки все звонки находящиеся в той же сессии что и сопоставленный звонок
                        var callsMCEToUpdate = callsMCE.Where(x => x.Item1.SessionID == correspondingCall.Item1.SessionID).ToList();
                        foreach (var callToUpdate in callsMCEToUpdate)
                        {
                            var index = callsMCE.FindIndex(x => x.Item1.pkID == callToUpdate.Item1.pkID);
                            callsMCE[index] = new Tuple<IV_CallRecord, bool>(correspondingCall.Item1, true);
                        }

                        linkedCallsCount++;
                    }

                    // Переключаем флаг обработки
                    call.is_processed = true;
                    i++;

                    // Отмечаем в логгере каждую тысячу обработанных звонков
                    if (i > 1000 && i % 1000 == 0)
                    {
                        _logger.Info($"Обработано звонков: {i}");

                    }
                }
                catch (Exception ex)
                {
                    _logger.Info($"Ошибка при обработке звонков: {ex.Message + Environment.NewLine + ex.InnerException}");
                }
            }
            try
            {
                var saveHelper = new Helper();
                saveHelper.UpdateCalls(calls);
            }
            catch (Exception ex)
            {
                _logger.Info($"Ошибка при сохранении информации в БД: {ex.Message + Environment.NewLine + ex.InnerException}");
            }
        }
        private static string GetLastTenDigits(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return string.Empty;

            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            return digitsOnly.Length >= 10 ? digitsOnly.Substring(digitsOnly.Length - 10) : digitsOnly;
        }
    }
}
