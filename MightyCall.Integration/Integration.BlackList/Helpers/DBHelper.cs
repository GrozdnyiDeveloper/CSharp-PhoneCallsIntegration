using crmPark.Integration.External.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace crmPark.Integration.External.Helpers
{
    public static class DBHelper
    {
        /// <summary>
        /// Получение списка идентификаторов заявок
        /// </summary>
        /// <param name="phoneNumber">Номер телефона</param>
        /// <param name="queueID">Необязательный параметр "идентификатор очереди"</param>
        /// <returns></returns>
        public static List<Guid> GetSessionsId(string phoneNumber, Guid? queueID = null)
        {
            if (phoneNumber == null) return new List<Guid>();
            using (var context = new INFRAACDEntities())
            {
                return (queueID == null ?
                    context.ACD_CustomerSession.Where(x => x.Properties.Contains(phoneNumber) && x.SessionEnd == null)
                     : context.ACD_CustomerSession.Where(x => x.Properties.Contains(phoneNumber) && x.QueueID == queueID && x.SessionEnd == null))
                     .Select(x => x.SessionID).ToList();
            }
        }
        /// <summary>
        /// Метод проверки наличия номера телефона в очереди на перезвон
        /// </summary>
        /// <param name="phoneNumber">Номер телефона</param>
        /// <param name="guidQueue">Guid очереди, если Guid равен null используется Guid очереди на перезвон</param>
        /// <returns></returns> 
        public static bool IsPhoneNumberInQueue(string phoneNumber, Guid? guidQueue)
        {
            if (phoneNumber == null) return false;
            Guid? queueID = guidQueue == null ? new Guid(WebConfigHelper.Campaingid) : guidQueue;
            using (var context = new INFRAACDEntities())
            {
                return
                    context.ACD_CustomerSession.Where(x => x.QueueID == queueID
                    && x.SessionEnd == null && x.Properties.Contains(phoneNumber)).Count() > 0 ? true : false;
            }
        }
    }
}