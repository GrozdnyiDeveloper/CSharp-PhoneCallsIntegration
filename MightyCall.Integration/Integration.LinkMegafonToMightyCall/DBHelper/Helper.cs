using crmPark.Integration.LinkMegafonToMightyCall.DbDataClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace crmPark.Integration.LinkMegafonToMightyCall.DBHelper
{
    public class Helper
    {
        /// <summary>
        /// Получение списка звонков для обновления
        /// </summary>
        /// <returns></returns>
        public List<Megafon_calls> GetMegafonCallsToUpdate()
        {

            using (var context = new MegaFonDatabaseEntities())
            {
                return context.Megafon_calls.Where(x => x.is_processed != true && (x.client != null)).ToList();
            }

        }
        /// <summary>
        /// Получение списка звонков из MightyCall по дате начала звонка. Отбираются только звонки клиентам (не переводы) 
        /// </summary>
        /// <param name="lastDate">Дата начала звонка</param>
        /// <returns></returns>
        public List<Tuple<IV_CallRecord, bool>> GetMightyCallCalls(DateTime lastDate)
        {
            using (var context = new INFRAVISOR())
            {

                var calls = context.IV_CallRecord.Where(x => x.StartTime >= lastDate).ToList();
                var callsMCE = calls.Where(x=> x.OrgNumber.Length >=10).Select(x => new Tuple<IV_CallRecord, bool>(x, false)).ToList();
                return callsMCE;
            }
        }
        /// <summary>
        /// Обновление звонков
        /// </summary>
        /// <param name="calls">Список звонков</param>
        public void UpdateCalls(List<Megafon_calls> calls)
        {
            using(var context = new MegaFonDatabaseEntities())
            {
                // Для каждого обработанного звонка
                foreach(var call in calls)
                {
                    // Получение звонок из БД по его ID
                    var callToUpdate = context.Megafon_calls.Where(x => x.call_id == call.call_id).FirstOrDefault();
                    // Если успешно получили данные звонка по ID
                    if (callToUpdate != null)
                    {
                        // Обновляем данные по результатам сопоставления
                        callToUpdate.call_external_id = call.call_external_id;
                        callToUpdate.is_processed = call.is_processed;
                    }
                }
                // Обновляем данные в БД
                context.SaveChanges();
            }
        }
    }
}