using MongoDB.Bson;
using MongoDB.Driver;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateStatisticsCore.Models;
using UpdateStatisticsCore.Repositories;

namespace UpdateStatisticsCore.Providers
{
    public class ClientUpdatesProvider
    {
        /// <summary>
        /// Метод для получения списка последних попыток пополнения всех клиентов, находящихся на интернет пополнении
        /// </summary>
        /// <returns></returns>
        public IQueryable<ClientUpdate> GetClientUpdates()
        {
            List<ClientUpdate> clientStatistics = GetClientStatistics();
            List<ClientUpdate> unfinishedClientUpdates = GetUnfinishedClientUpdates();

            foreach (ClientUpdate unfinishedUpdate in unfinishedClientUpdates)
            {
                ClientUpdate clientStatistic = clientStatistics.Where(cls => cls.DistributiveId == unfinishedUpdate.DistributiveId).FirstOrDefault();
                if (clientStatistic != null)
                {
                    if (unfinishedUpdate.StartDate > clientStatistic.StartDate)
                    {
                        clientStatistic.StartDate = unfinishedUpdate.StartDate;
                        clientStatistic.EndDate = null;
                        clientStatistic.LastSuccessUpdateDate = null;
                        clientStatistic.Status = "purple";
                    }
                }
                else
                    clientStatistics.Add(unfinishedUpdate);
            }

            return clientStatistics.AsQueryable();
        }

        public IQueryable<ClientUpdate> GetClientUpdatesMongo()
        {
            MongoClient mongoClient = new MongoClient();
            var mongoDb = mongoClient.GetDatabase("UpdateServer");

            var collection = mongoDb.GetCollection<ClientUpdate>("AllClients");

            return collection.Find(new BsonDocument()).ToList().AsQueryable();
        }
        /// <summary>
        /// Метод для получения списка незавершенных попыток пополнения
        /// </summary>
        /// <returns></returns>
        private List<ClientUpdate> GetUnfinishedClientUpdates()
        {
            List<ClientUpdate> lastUpdates = new List<ClientUpdate>();
            string query = @"SELECT * FROM GetNowUpdatingFunction()";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cn.Open();

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    lastUpdates = dt.AsEnumerable().Select(r => new ClientUpdate
                    {
                        DistributiveId = Convert.ToInt32(r["id"]),
                        DistributiveNumber = r["distr_number"].ToString(),
                        SystemCode = Convert.ToInt32(r["system_code"]),
                        StartDate = r["start_date"] == DBNull.Value ? (double?)null : Convert.ToDateTime(r["start_date"]).ToJson(),
                        SttReceivedDate = r["send_stt_date"] == DBNull.Value ? (double?)null : Convert.ToDateTime(r["send_stt_date"]).ToJson(),
                        ClientName = r["client_name"] == DBNull.Value ? null : r["client_name"].ToString(),
                        IsCanceled = Convert.ToBoolean(r["is_canceled"]),
                        EngineerName = r["engineer_name"] == DBNull.Value ? null : r["engineer_name"].ToString(),
                        GroupChiefName = r["group_chief_name"] == DBNull.Value ? null : r["group_chief_name"].ToString(),
                        DistributiveComment = r["distributive_comment"] == DBNull.Value ? null : r["distributive_comment"].ToString(),
                        EngineerDistributiveComment = r["iu_client_comment"] == DBNull.Value ? null : r["iu_client_comment"].ToString(),
                        ResVersion = r["res_version"] == DBNull.Value ? null : r["res_version"].ToString(),
                        SendStt = r["send_stt"] == DBNull.Value ? -1 : Convert.ToInt32(r["send_stt"]),
                        ClientReturnedCode = r["client_returned_code"] == DBNull.Value ? -1 : Convert.ToInt32(r["client_returned_code"]),
                        ServerReturnedCode = r["server_returned_code"] == DBNull.Value ? -1 : Convert.ToInt32(r["server_returned_code"]),
                        UsrRarFileName = r["usr_rar_file_name"] == DBNull.Value ? null : r["usr_rar_file_name"].ToString(),
                        ServerName = r["server_name"] == DBNull.Value ? null : r["server_name"].ToString(),
                        Status = "purple"
                    }).ToList();

                    cn.Close();
                    return lastUpdates;
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить список последних попыток пополнения. Текст ошибки: {0}", ex.Message);
                return null;
            }
        }
        /// <summary>
        /// Метод для получения последних попыток пополнения
        /// </summary>
        /// <returns></returns>
        private List<ClientUpdate> GetClientStatistics()
        {
            List<ClientUpdate> updates = new List<ClientUpdate>();
            string query = @"SELECT cls.client_name, cls.is_canceled, cls.distr_number, cls.engineer_name, cls.group_chief_name, cls.id,
                             cls.distributive_comment, cls.iu_client_comment, cls.system_code,
                             cls.start_date, cls.end_date, cls.send_stt_date, cls.res_version, cls.client_returned_code, cls.server_returned_code,
                             cls.usr_rar_file_name, cls.send_stt, lsu.end_date AS lsu_date,
                             lsu.client_returned_code AS lsu_client_returned_code, lsu.server_returned_code AS lsu_server_returned_code,
                             lsu.usr_rar_file_name AS lsu_usr_rar_file_name, CASE WHEN lsu.server_name IS NULL THEN cls.server_name ELSE lsu.server_name END AS server_name 
                             FROM GetClientStatisticsFunctionDev() cls
                             LEFT JOIN GetLastSuccessUpdatesFunctionDev() lsu ON cls.id = lsu.iu_client_distr_id";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cn.Open();

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    updates = dt.AsEnumerable().Select(r => new ClientUpdate
                    {
                        DistributiveId = Convert.ToInt32(r["id"]),
                        DistributiveNumber = r["distr_number"].ToString(),
                        SystemCode = Convert.ToInt32(r["system_code"]),
                        StartDate = r["start_date"] == DBNull.Value ? (double?)null : Convert.ToDateTime(r["start_date"]).ToJson(),
                        LastSuccessUpdateDate = r["lsu_date"] == DBNull.Value ? (double?)null : Convert.ToDateTime(r["lsu_date"]).ToJson(),
                        EndDate = r["end_date"] == DBNull.Value ? (double?)null : Convert.ToDateTime(r["end_date"]).ToJson(),
                        SttReceivedDate = r["send_stt_date"] == DBNull.Value ? (double?)null : Convert.ToDateTime(r["send_stt_date"]).ToJson(),
                        ClientName = r["client_name"] == DBNull.Value ? null : r["client_name"].ToString(),
                        IsCanceled = Convert.ToBoolean(r["is_canceled"]),
                        EngineerName = r["engineer_name"] == DBNull.Value ? null : r["engineer_name"].ToString(),
                        GroupChiefName = r["group_chief_name"] == DBNull.Value ? null : r["group_chief_name"].ToString(),
                        DistributiveComment = r["distributive_comment"] == DBNull.Value ? null : r["distributive_comment"].ToString(),
                        EngineerDistributiveComment = r["iu_client_comment"] == DBNull.Value ? null : r["iu_client_comment"].ToString(),
                        ResVersion = r["res_version"] == DBNull.Value ? null : r["res_version"].ToString(),
                        SendStt = r["send_stt"] == DBNull.Value ? -1 : Convert.ToInt32(r["send_stt"]),
                        ClientReturnedCode = r["client_returned_code"] == DBNull.Value ? -1 : Convert.ToInt32(r["client_returned_code"]),
                        LastSuccessUpdateClientReturnedCode = r["lsu_client_returned_code"] == DBNull.Value ? -1 : Convert.ToInt32(r["lsu_client_returned_code"]),
                        ServerReturnedCode = r["server_returned_code"] == DBNull.Value ? -1 : Convert.ToInt32(r["server_returned_code"]),
                        LastSuccessUpdateServerReturnedCode = r["lsu_server_returned_code"] == DBNull.Value ? -1 : Convert.ToInt32(r["lsu_server_returned_code"]),
                        UsrRarFileName = r["usr_rar_file_name"] == DBNull.Value ? null : r["usr_rar_file_name"].ToString(),
                        LastSuccessUpdateUsrRarFileName = r["lsu_usr_rar_file_name"] == DBNull.Value ? null : r["lsu_usr_rar_file_name"].ToString(),
                        ServerName = r["server_name"] == DBNull.Value ? null : r["server_name"].ToString()
                    }).Select(cu =>
                    {
                        if (!cu.EndDate.HasValue)
                            cu.Status = "purple";
                        else
                        {
                            TimeSpan? startTime, lsuTime = null;
                            DateTime? startDate = null, lsuDate = null;

                            if (cu.StartDate.HasValue && cu.StartDate != 0)
                            {
                                startTime = TimeSpan.FromMilliseconds(cu.StartDate.Value);
                                startDate = new DateTime(1970, 1, 1) + startTime;
                            }
                            if (cu.LastSuccessUpdateDate.HasValue && cu.LastSuccessUpdateDate != 0)
                            {
                                lsuTime = TimeSpan.FromMilliseconds(cu.LastSuccessUpdateDate.Value);
                                lsuDate = new DateTime(1970, 1, 1) + lsuTime;
                            }

                            if (lsuDate == null)
                                cu.Status = "yellow-red";
                            else
                                cu.Status = CalculateStatus(cu.ClientReturnedCode, cu.LastSuccessUpdateClientReturnedCode, cu.UsrRarFileName,
                                    cu.LastSuccessUpdateUsrRarFileName, startDate, lsuDate);
                        }

                        string[] resWords = cu.ResVersion.Split('.');
                        string resTemp = resWords[0];
                        for (int i = 1; i < resWords.Count() - 1; i++)
                        {
                            resTemp += "." + resWords[i];
                        }

                        cu.ResVersion = resTemp;

                        return cu;
                    }).ToList();

                    cn.Close();
                    return updates;
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить список последних попыток пополнения. Текст ошибки: {0}", ex.Message);
                return null;
            }
        }

        public List<ClientUpdateShort> GetNowUpdatingMongo()
        {
            MongoClient mongoClient = new MongoClient();
            var mongoDb = mongoClient.GetDatabase("UpdateServer");

            var collection = mongoDb.GetCollection<ClientUpdateShort>("NowUpdatingClients");

            return collection.Find(new BsonDocument()).ToList();
        }

        /// <summary>
        /// Метод для получения клиентов, осуществляющих в данный момент попытку пополнения
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ClientUpdateShort> GetNowUpdating()
        {
            DataTable dt = new DataTable();
            string query = @"SELECT * FROM GetNowUpdating()
                            ORDER BY LastMessageDate desc";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cn.Open();

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);

                    cn.Close();
                }

                return dt.AsEnumerable().Select(r =>
                {
                    if (r["LastMessageDate"] == DBNull.Value)
                        return null;

                    ClientUpdateShort cluShort = new ClientUpdateShort();
                    cluShort.DistributiveNumber = r["DistrNumber"].ToString();
                    cluShort.FileName = r["FileName"].ToString();
                    string distrComment = r["DistrComment"] == DBNull.Value ? null : string.Format(" ({0})", r["DistrComment"].ToString());
                    cluShort.ClientName = r["ClientName"].ToString() + distrComment;

                    if (string.IsNullOrEmpty(cluShort.DistributiveNumber))
                    {
                        string distrNumber = cluShort.FileName.Split('#')[0].Split('_')[1];
                        string compNumber = "";

                        if (cluShort.FileName.Split('#').Length > 2)
                            compNumber = "." + cluShort.FileName.Split('#')[0].Split('_')[2];

                        while (distrNumber.StartsWith("0"))
                            distrNumber = distrNumber.Remove(0, 1);
                        while (compNumber.StartsWith("0"))
                            compNumber = compNumber.Remove(0, 1);

                        cluShort.DistributiveNumber = distrNumber + compNumber;
                        cluShort.ClientName = "Не удалось определить";
                    }

                    DateTime lastMessageDate = Convert.ToDateTime(r["LastMessageDate"]);

                    if ((DateTime.Now - lastMessageDate).TotalHours > 6)
                        cluShort.Status = "red";
                    else if ((DateTime.Now - lastMessageDate).TotalHours > 3 && (DateTime.Now - lastMessageDate).Hours < 6)
                        cluShort.Status = "yellow";

                    cluShort.FormattedDate = string.Format("{0},{1},{2},{3},{4},{5}",
                        lastMessageDate.Year, lastMessageDate.Month, lastMessageDate.Day, lastMessageDate.Hour, lastMessageDate.Minute, lastMessageDate.Second);
                    cluShort.Message = FormatIPSMessage(r["LastMessage"].ToString());

                    return cluShort;
                });
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить список клиентов, обновляющихся в данный момент. Текст ошибки: {0}", ex.Message);
                return null;
            }
        }
        /// <summary>
        /// Метод для форматирования сообщения из лог файла СИП
        /// </summary>
        /// <param name="lastMessage"></param>
        /// <returns></returns>
        private string FormatIPSMessage(string lastMessage)
        {
            UpdateMessageRule rule = UpdateStatisticsConfig.Instance.UpdateMessagesRules.Where(usc => lastMessage.StartsWith(usc.IPSMessage)).FirstOrDefault();

            if (rule != null)
                return rule.Message;

            return lastMessage;
        }
        /// <summary>
        /// Метод для вычисления статуса попытки пополнения
        /// </summary>
        /// <param name="clientReturnedCode">Код возврата клиента последней попытки пополнения</param>
        /// <param name="lastSuccessUpdateClientReturnedCode">Код возврата клиента последней удачной попытки пополнения</param>
        /// <param name="usrRarFileName">Имя архива с USR файлом последней попытки пополнения</param>
        /// <param name="lastSuccessUpdateUsrRarFileName">Имя архива с USR файлом последней удачной попытки пополнения</param>
        /// <param name="startDate">Дата начала последней попытки пополнения</param>
        /// <param name="lastSuccessUpdateDate">Дата окончания последней удачной попытки пополнения</param>
        /// <returns></returns>
        private string CalculateStatus(int clientReturnedCode, int lastSuccessUpdateClientReturnedCode, string usrRarFileName,
            string lastSuccessUpdateUsrRarFileName, DateTime? startDate, DateTime? lastSuccessUpdateDate)
        {
            string status = null;

            switch (clientReturnedCode)
            {
                case 0:
                    status = "default";
                    break;
                case 70:
                    status = "green";
                    break;
                default:
                    status = "yellow";
                    break;
            }

            if ((string.IsNullOrEmpty(usrRarFileName) || usrRarFileName == "-") && clientReturnedCode != 70)
                status = "yellow";

            if (DateTime.Now - startDate > TimeSpan.FromDays(7) || lastSuccessUpdateDate == null)
                status = "red";
            if (status == "yellow")
            {
                if (DateTime.Now - lastSuccessUpdateDate > TimeSpan.FromDays(7))
                    status = "yellow-red";
                else if (DateTime.Now - lastSuccessUpdateDate < TimeSpan.FromDays(7) && lastSuccessUpdateClientReturnedCode != 70)
                    status = "yellow";
                else
                    status = "green";
            }

            return status;
        }
        /// <summary>
        /// Метод для получения последнего сообщения попытки пополнения
        /// </summary>
        /// <param name="distributiveId">Идентификатор дистрибутива</param>
        /// <returns></returns>
        public LastMessage GetDistributiveUpdateLastMessage(int distributiveId)
        {
            LastMessage lastMessage = new LastMessage();
            string query = @"SELECT TOP 1 uafm.Message, uafm.Date FROM updateserver.UpdateAttemptFilesMessages uafm
                            LEFT JOIN updateserver.UpdateAttemptFiles uaf ON uafm.UpdateFileId = uaf.id
                            WHERE uaf.DistrId = @DistrId
                            ORDER BY uafm.Date desc";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cn.Open();

                    cmd.Parameters.Add("@DistrId", SqlDbType.Int).Value = distributiveId;

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        lastMessage.Message = FormatIPSMessage(reader[0].ToString());
                        lastMessage.Moment = Convert.ToDateTime(reader[1]);
                    }

                    cn.Close();
                }
                return lastMessage;
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить последнее сообщение попытки пополнения. ID Дистрибутива: {0} Текст ошибки: {1}", distributiveId, ex.Message);
                return null;
            }
        }
        /// <summary>
        /// Метод для обновления пользовательского комментария к дистрибутиву 
        /// </summary>
        /// <param name="distributiveId">Идентификатор дистрибутива</param>
        /// <param name="comment">Текст комментария</param>
        /// <returns></returns>
        public bool UpdateDistributiveComment(int distributiveId, string comment)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(@"UPDATE Distributives SET iu_client_comment = @Comment WHERE id = @DistrId", cn))
                {
                    cmd.Parameters.AddWithValue("@Comment", string.IsNullOrEmpty(comment) ? (object)DBNull.Value : comment);
                    cmd.Parameters.AddWithValue("@DistrId", distributiveId);

                    cn.Open();

                    cmd.ExecuteNonQuery();

                    cn.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке обновить комментарий клиента. DistrId: {0}. Текст ошибки: {1}", distributiveId, ex.Message);
                return false;
            }

            return true;
        }
        /// <summary>
        /// Метод для обновления списка кэшированных клиентов
        /// </summary>
        /// <param name="clients">Список клиентов</param>
        /// <param name="engineerName">Имя СИО, по которому отсортирован список</param>
        /// <param name="ipAddress">IP адрес клиента, отправившего запрос</param>
        /// <returns></returns>
        public int UpdateClientsListByEngineerName(List<ClientInfo> clients, string engineerName, string ipAddress)
        {
            int cacheId = GetClientListCacheId(engineerName, ipAddress);

            string query = @"INSERT INTO updateserver.ClientsListCacheDistributives(distributive_number, distributive_id, server_name, cache_id)
                             VALUES(@distributive_number, @distributive_id, @server_name, @cache_id)";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, cn))
                {                  
                    cn.Open();
                    cmd.Parameters.AddWithValue("@cache_id", cacheId);
                    bool first = true;
                    foreach (ClientInfo client in clients)
                    {
                        if (!DBRepository.CheckRecordExisting("updateserver.ClientsListCacheDistributives", new Dictionary<string, object>
                            {
                                { "distributive_number", client.DistributiveNumber },
                                { "distributive_id", client.DistributiveId },
                                { "server_name", client.ServerName},
                                { "cache_id", cacheId }
                            }))
                        {
                            if (first)
                            {
                                cmd.Parameters.AddWithValue("@distributive_number", client.DistributiveNumber);
                                cmd.Parameters.AddWithValue("@distributive_id", client.DistributiveId);
                                cmd.Parameters.AddWithValue("@server_name", client.ServerName);
                                first = false;
                            }
                            else
                            {
                                cmd.Parameters["@distributive_number"].Value = client.DistributiveNumber;
                                cmd.Parameters["@distributive_id"].Value = client.DistributiveId;
                                cmd.Parameters["@server_name"].Value = client.ServerName;
                            }
                            cmd.ExecuteNonQuery();
                        }
                    }                   
                    cn.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить список клиентов по имени инженера. EngineerName: {0}. Текст ошибки: {1}", engineerName, ex.Message);
            }

            return cacheId;
        }
        /// <summary>
        /// Метод для получения идентификатора закэшированного списка дистрибутивов
        /// </summary>
        /// <param name="engineerName">Имя СИО, по которому отсортирован список</param>
        /// <param name="ipAddress">IP адрес клиента, отправившего запрос</param>
        /// <returns></returns>
        private int GetClientListCacheId(string engineerName, string ipAddress)
        {
            int cacheId = -1;
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(@"SELECT TOP 1 id FROM updateserver.ClientsListCache WHERE engineer_name = @EngineerName AND ip_address = @IpAddress", cn))
                {
                    cmd.Parameters.AddWithValue("@EngineerName", engineerName);
                    cmd.Parameters.AddWithValue("@IpAddress", ipAddress);
                    cn.Open();

                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                        cacheId = reader.GetInt32(0);

                    cn.Close();
                }

                if (cacheId == -1)
                {
                    using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                    using (SqlCommand cmd = new SqlCommand(@"INSERT INTO updateserver.ClientsListCache (engineer_name, ip_address) VALUES (@EngineerName, @IpAddress);
                                                             SELECT SCOPE_IDENTITY();", cn))
                    {
                        cmd.Parameters.AddWithValue("@EngineerName", engineerName);
                        cmd.Parameters.AddWithValue("@IpAddress", ipAddress);
                        cn.Open();

                        cacheId = Convert.ToInt32(cmd.ExecuteScalar().ToString());

                        cn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить список клиентов по имени инженера. EngineerName: {0}. Текст ошибки: {1}", engineerName, ex.Message);
            }

            return cacheId;
        }
        /// <summary>
        /// Метод для получения названия систему по ее коду
        /// </summary>
        /// <param name="systemCode"></param>
        /// <returns></returns>
        public string GetSystemCodeDescription(string systemCode)
        {
            string systemName = "Не найдено";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(@"SELECT ss.Name FROM SoftSystems ss
                                                         LEFT JOIN updateserver.SystemCodeHandbook sch ON ss.id = sch.idSystem
                                                         WHERE sch.Code = @SystemCode", cn))
                {
                    cmd.Parameters.AddWithValue("@SystemCode", systemCode);
                    cn.Open();

                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                        systemName = reader.GetString(0);

                    cn.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить название системы по ее коду. SystemCode: {0}. Текст ошибки: {1}", systemCode, ex.Message);
            }

            return systemName;
        }
    }
}
