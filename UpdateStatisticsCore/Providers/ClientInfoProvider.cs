using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateStatisticsCore.Models;
using UpdateStatisticsCore.Repositories;

namespace UpdateStatisticsCore.Providers
{
    public partial class ClientInfoProvider
    {
        /// <summary>
        /// Метод для получения попыток пополнения клиента
        /// </summary>
        /// <param name="distributiveId">Идентификатор дистрибутива</param>
        /// <returns></returns>
        public IEnumerable<Update> GetClientUpdates(int distributiveId)
        {
            DataTable dt = new DataTable();
            string query = @"SELECT * FROM GetClientUpdates(@DistrId)";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@DistrId", distributiveId);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);

                    cn.Close();
                }

                return dt.AsEnumerable().Select(r =>
                {
                    Update update = new Update
                    {
                        FileId = r["update_file_id"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["update_file_id"]),
                        StartDate = r["start_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["start_date"]),
                        EndDate = r["end_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["end_date"]),
                        ClientCode = r["client_returned_code"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["client_returned_code"]),
                        ServerCode = r["server_returned_code"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["server_returned_code"]),
                        IsUsrExists = r["usr_rar_file_name"] == DBNull.Value ? false : true,
                        ServerName = r["server_name"].ToString()
                    };

                    update.DownloadSpeed = FormatDownloadSpeed(r["download_speed"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["download_speed"]));
                    update.UpdateSize = FormatUpdateSize(r["update_size"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["update_size"]));
                    update.DownloadTime = FormatDownloadTime(r["download_time"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["download_time"]));
                    update.UpdateTime = FormatDownloadTime(r["update_time"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["update_time"]));

                    update.StatusColor = CalculateStatusColor(update);

                    return update;
                });
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить список попыток пополнения. DistrId: {0}. Текст ошибки: {1}", distributiveId, ex.Message);
                return null;
            }
        }
        /// <summary>
        /// Метод для вычисления цвета заголовка попытки пополнения
        /// </summary>
        /// <param name="update">Экземпляр попытки пополнения</param>
        /// <returns></returns>
        private string CalculateStatusColor(Update update)
        {
            if (!update.EndDate.HasValue)
                return "#BADEFC";

            string statusColor = "#f38888";
            if (!update.IsUsrExists)
                statusColor = "#f38888";
            switch (update.ClientCode)
            {
                case 70:
                    statusColor = "#a1f57e";
                    break;
                case 0:
                    statusColor = "#a1f57e";
                    break;
                default:
                    statusColor = "#fbfc96";
                    break;
            }

            return statusColor;
        }
        /// <summary>
        /// Метод для получения информации о клиенте на основании информации из попытки пополнения
        /// </summary>
        /// <param name="distributiveId">Идентификатор дистрибутива</param>
        /// <param name="serverName">Имя сервера</param>
        /// <param name="distributiveNumber">Номер дистрибутива</param>
        /// <returns></returns>
        public ClientInfo GetClientInfo(int distributiveId, string serverName, string distributiveNumber)
        {
            DataTable dt = new DataTable();
            string query = @"SELECT TOP 1 cl.Name, d.iu_client_comment
                            FROM Distributives d 
                            LEFT JOIN [TO] tos ON d.idTO = tos.id
                            LEFT JOIN Contracts ct ON tos.idContract = ct.id
                            LEFT JOIN Clients cl ON ct.idClient = cl.id
                            WHERE d.id = @DistrId";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@DistrId", distributiveId);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);

                    cn.Close();
                }

                return dt.AsEnumerable().Select(r => new ClientInfo
                {
                    DistributiveId = distributiveId,
                    ServerName = serverName,
                    DistributiveNumber = distributiveNumber,
                    ClientName = r["Name"].ToString(),
                    ClientComment = r["iu_client_comment"] == DBNull.Value ? null : r["iu_client_comment"].ToString()
                }).FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить информацию о клиенте. DistrId: {0}. Текст ошибки: {1}", distributiveId, ex.Message);
                return null;
            }
        }
        /// <summary>
        /// Метод для получения экземпляра попытки пополнения
        /// </summary>
        /// <param name="distributiveId">Идентификатор дистрибутива</param>
        /// <param name="serverName">Имя сервера</param>
        /// <param name="startDate">Дата начала пополнения</param>
        /// <returns></returns>
        public UpdateLog GetUpdateLog(int distributiveId, string serverName, DateTime? startDate)
        {
            UpdateLog updateLog = new UpdateLog();

            updateLog = DBRepository.GetUpdateLog(distributiveId, serverName, startDate);

            if (updateLog == null)
                updateLog = FSRepository.GetUpdateLog(distributiveId, serverName, startDate);

            return updateLog;
        }
        /// <summary>
        /// Метод для получения списка клиентов из кэша (для возможности перехода от одного клиента к другому на странице информации о попытке пополнения)
        /// </summary>
        /// <param name="engineerName">Имя СИО, по которому отсортирован список</param>
        /// <param name="ipAddress">IP адрес клиента, отправившего запрос</param>
        /// <returns></returns>
        public IEnumerable<ClientInfo> GetClientsListFromCache(string engineerName, string ipAddress)
        {
            DataTable dt = new DataTable();
            string query = @"SELECT clcd.distributive_id, clcd.server_name, clcd.distributive_number, clcd.current_client 
                            FROM updateserver.ClientsListCacheDistributives clcd
                            LEFT JOIN updateserver.ClientsListCache clc ON clcd.cache_id = clc.id
                            WHERE clc.engineer_name = @EngineerName AND clc.ip_address = @IpAddress
                            ORDER BY clcd.id";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cmd.Parameters.AddWithValue("@EngineerName", engineerName);
                    cmd.Parameters.AddWithValue("@IpAddress", ipAddress);
                    cn.Open();

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);

                    cn.Close();

                    return dt.AsEnumerable().Select(r => new ClientInfo
                    {
                        DistributiveId = Convert.ToInt32(r["distributive_id"]),
                        DistributiveNumber = r["distributive_number"].ToString(),
                        ServerName = r["server_name"].ToString(),
                        Current = r["current_client"] == DBNull.Value ? false : Convert.ToBoolean(r["current_client"])
                    });
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить список клиентов по имени инженера. EngineerName: {0}. Текст ошибки: {1}", engineerName, ex.Message);
                return null;
            }
        }
        /// <summary>
        /// Метод для установки текущего клиента в списке закэшированных клиентов
        /// </summary>
        /// <param name="engineerName">Имя СИО, по которому отсортирован список</param>
        /// <param name="ipAddress">IP адрес клиента, отправившего запрос</param>
        /// <param name="distributiveId">Идентификатор дистрибутива текущего клиента</param>
        /// <param name="serverName">Имя сервера</param>
        public void SetCurrentClient(string engineerName, string ipAddress, int distributiveId, string serverName)
        {
            if (string.IsNullOrEmpty(engineerName))
                return;

            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(@"UPDATE updateserver.ClientsListCacheDistributives SET current_client = 0 
                                                        WHERE cache_id = (SELECT TOP 1 id FROM updateserver.ClientsListCache WHERE ip_address = @IpAddress AND engineer_name = @EngineerName)", cn))
                {
                    cmd.Parameters.AddWithValue("@IpAddress", ipAddress);
                    cmd.Parameters.AddWithValue("@EngineerName", engineerName);
                    cn.Open();

                    cmd.ExecuteNonQuery();

                    cn.Close();
                }

                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(@"UPDATE updateserver.ClientsListCacheDistributives SET current_client = 1 
                                                        WHERE distributive_id = @DistributiveId AND server_name = @ServerName 
                                                        AND cache_id = (SELECT TOP 1 id FROM updateserver.ClientsListCache WHERE ip_address = @IpAddress AND engineer_name = @EngineerName)", cn))
                {
                    cmd.Parameters.AddWithValue("@DistributiveId", distributiveId);
                    cmd.Parameters.AddWithValue("@ServerName", serverName);
                    cmd.Parameters.AddWithValue("@IpAddress", ipAddress);
                    cmd.Parameters.AddWithValue("@EngineerName", engineerName);
                    cn.Open();

                    cmd.ExecuteNonQuery();

                    cn.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке установить текущего клиента в кэше. IpAddress: {0}. EngineerName: {1}. DistributiveId:{2}. Текст ошибки: {3}", ipAddress, engineerName,
                    distributiveId, ex.Message);
            }
        }
        public string GetClientCodeDescription(string code)
        {
            string description = "Не удалось определить";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("SELECT Description FROM updateserver.ClientCodeHandbook WHERE Code = @Code", cn))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    cn.Open();

                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                        description = reader.GetString(0);

                    cn.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить расшифровку кода возврата клиента. Код: {0}. Текст ошибки: {1}", code, ex.Message);
            }
            return description;
        }
        #region Converters
        private string FormatDownloadTime(long? downloadTime)
        {
            if (!downloadTime.HasValue)
                return "Пополнение не окончено";
            decimal converted = 0;

            converted = Convert.ToDecimal(downloadTime) / 60;
            if (converted < 1)
                return "меньше минуты";
            else
                return converted.ToString("0.00 м");
        }

        private string FormatUpdateSize(long? updateSize)
        {
            if (!updateSize.HasValue)
                return "Пополнение не окончено";
            decimal converted = 0;

            converted = Convert.ToDecimal(updateSize) / (1024 * 1024);
            return converted.ToString("0.00 мб");
        }

        private string FormatDownloadSpeed(long? downloadSpeed)
        {
            if (!downloadSpeed.HasValue)
                return "Пополнение не окончено";
            decimal converted = 0;

            converted = Convert.ToDecimal(downloadSpeed) / 1024;
            return converted.ToString("0.00 мб/с");
        }
        #endregion
    }
}
