using NLog;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace crmPark.Integration.External.Helpers
{
    public class MightyCallHelper
    {
        private HttpClient _httpClient;
        public static Logger _logger;
        public MightyCallHelper()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }
        /// <summary>
        /// Метод удаления заявки из очереди
        /// </summary>
        /// <param name="requestID">id заявки</param>
        /// <returns></returns>
        public async Task<Tuple<string, bool>> TerminateRequest(Guid requestID)
        {
            // Создание HTTP-клиента
            _httpClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });
            _httpClient.BaseAddress = new Uri(WebConfigHelper.MightyCallBaseUrl);

            try
            {
                // Создание контекста
                var requestContent = new StringContent(
                    $"{{ \"requestId\": \"{{{requestID}}}\" }}",
                    Encoding.UTF8,
                    "application/json");

                _logger.Info($"Попытка удалить заявку: {requestID}");

                // Выполнение POST-запроса на удаление заявки
                var response = await _httpClient.PostAsync(
                    "outbound.svc/json/request_terminate",
                    requestContent);

                // Читаем содержимое ответа (даже при ошибке)
                string responseContent = await response.Content.ReadAsStringAsync();
                string statusCode = response.StatusCode.ToString();

                if (response.IsSuccessStatusCode)
                {
                    _logger.Info($"Заявка {requestID} успешно удалена. Ответ: {responseContent}");
                    return new Tuple<string, bool>(
                        $"Заявка успешно удалена. Код: {statusCode}",
                        true);
                }
                else
                {
                    _logger.Warn($"Ошибка удаления заявки {requestID}. " +
                                $"Код: {statusCode}, Ответ: {responseContent}");

                    return new Tuple<string, bool>(
                        $"Ошибка: {statusCode}. Ответ сервера: {responseContent}",
                        false);
                }
            }
            catch (HttpRequestException httpEx)
            {
                string errorMessage = $"Ошибка сети при удалении заявки {requestID}: {httpEx.Message}";
                _logger.Error(httpEx, errorMessage);
                return new Tuple<string, bool>(errorMessage, false);
            }
            catch (TaskCanceledException timeoutEx)
            {
                string errorMessage = $"Таймаут при удалении заявки {requestID}";
                _logger.Error(timeoutEx, errorMessage);
                return new Tuple<string, bool>(errorMessage, false);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Неожиданная ошибка при удалении заявки {requestID}: {ex.Message}";
                _logger.Error(ex, errorMessage);
                return new Tuple<string, bool>(errorMessage, false);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}