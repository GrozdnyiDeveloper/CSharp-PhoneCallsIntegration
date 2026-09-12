using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace crmPark.Integration.SpeechAnalysis.Helpers
{
    public static class DataReaderHelper
    {
        private static readonly Type DBNullType = typeof(DBNull);
        /// <summary>
        /// Метод получения  значения из поля записи БД с указанным типом
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetValue<T>(object value, T defaultValue = default)
        {
            if (value == null || value == DBNull.Value)
                return defaultValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }
        /// <summary>
        /// Метод получения  значения из поля записи БД с указанным типом (с поддержкой нулевых типов). 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T? GetNullableValue<T>(object value) where T : struct
        {
            if (value == null || value == DBNull.Value)
                return null;

            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
