using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateStatisticsCore.Models;

namespace UpdateStatisticsCore.Repositories
{
    public static class DBRepository
    {
        /// <summary>
        /// Метод для получения экземпляра лог файла попытки пополнения
        /// </summary>
        /// <param name="distributiveId">Идентификатор дистрибутива</param>
        /// <param name="serverName">Имя сервера</param>
        /// <param name="startDate">Дата начала попытки пополнения</param>
        /// <returns>Экземпляр лог файла попытки пополнения</returns>
        public static UpdateLog GetUpdateLog(int distributiveId, string serverName, DateTime? startDate)
        {
            UpdateLog updateLog = new UpdateLog();

            string fileName = null;
            DataTable dt = new DataTable();
            string query = @"SELECT uafm.*, uaf.FileName FROM updateserver.UpdateAttemptFilesMessages uafm
                            LEFT JOIN updateserver.UpdateAttemptFiles uaf ON uafm.UpdateFileId = uaf.id
                            WHERE uaf.id = 
	                            (SELECT TOP 1 UpdateFileId FROM updateserver.UpdateAttemptFilesMessages uafm
	                            LEFT JOIN updateserver.UpdateAttemptFiles uaf ON uafm.UpdateFileId = uaf.id
	                            LEFT JOIN updateserver.Servers s ON uaf.ServerId = s.id
	                            WHERE s.Name = @ServerName AND DATEADD(ms, -DATEPART(ms, Date), Date) = @StartDate AND uaf.DistrId = @DistrId
                            ) ";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@ServerName", serverName);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@DistrId", distributiveId);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);

                    cn.Close();
                }

                if (dt.Rows.Count == 0)
                    return null;

                fileName = dt.AsEnumerable().Select(r => r["FileName"].ToString()).FirstOrDefault();
              
                var messageListArray = dt.AsEnumerable().Select(r => new
                {
                    Date = r["Date"].ToString(),
                    Message = r["Message"].ToString()
                }).ToArray();

                foreach (var item in messageListArray)
                {
                    updateLog.MessageList += string.Format(@"<b>{0}</b> <em>{1}</em></br>", item.Date, item.Message);
                }

                updateLog.QstList = GetQstFiles(fileName);
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить информацию о пополнении. DistributiveId: {0}. ServerName: {1}. Текст ошибки: {2}", distributiveId, serverName, ex.Message);
                return null;
            }

            return updateLog;
        }
        /// <summary>
        /// Метод для получения списка обработанных QST файлов
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        private static List<QST> GetQstFiles(string fileName)
        {
            string resultFileName = string.Format(@"{0}_result.log", fileName.Split('.')[0]);
            List<QST> qst = new List<QST>();
            DataTable dt = new DataTable();
            string query = @"SELECT uaqf.QstFileName, qh.Description, qh.Color FROM updateserver.UpdateAttemptQstFiles uaqf
                            LEFT JOIN updateserver.UpdateAttemptFiles uaf ON uaqf.UpdateFileId = uaf.id
                            LEFT JOIN updateserver.QstHandbook qh ON qh.Code = uaqf.StatusCode
                            WHERE uaf.FileName = @FileName";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@FileName", resultFileName);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);

                    cn.Close();
                }

                qst = dt.AsEnumerable().Select(r => new QST
                {
                    QstFileName = r["QstFileName"].ToString(),
                    QstStatusDescription = r["Description"] == DBNull.Value ? null : r["Description"].ToString(),
                    QstStatusColor = r["Color"] == DBNull.Value ? null : r["Color"].ToString()
                }).ToList();

            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить информацию о полученных QST файлах. FileName: {0}. Текст ошибки: {1}", fileName, ex.Message);
            }

            return qst;
        }

        #region Helpers
        /// <summary>
        /// Метод для получения номера дистрибутива по его идентификатору
        /// </summary>
        /// <param name="distributiveId">Идентификатор дистрибутива</param>
        /// <returns></returns>
        public static string GetDistrNumberById(int distributiveId)
        {
            string distrNumber = null;
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(@"SELECT Number FROM Distributives WHERE id = @DistrId", cn))
                {
                    cmd.Parameters.AddWithValue("@DistrId", distributiveId);

                    cn.Open();

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                        while (reader.Read())
                            distrNumber = reader.GetString(0);

                    cn.Close();
                }
            }
            catch(Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(@"Ошибка при попытке получить номер дистрибутива по его Id. DistrId: {0}. Текст ошибки: {1}", distributiveId, ex.Message);
            }

            return distrNumber;
        }
        /// <summary>
        /// Метод для получения названия рабочей директории сервера по его имени
        /// </summary>
        /// <param name="serverName"></param>
        /// <returns></returns>
        public static string GetServerDirectory(string serverName)
        {
            string directoryName = null;

            string query = @"SELECT TOP 1 ebs.Path FROM updateserver.EnvironmentsByServers ebs
                            LEFT JOIN updateserver.Servers s ON ebs.ServerId = s.id
                            LEFT JOIN updateserver.Environments e ON ebs.EnvironmentId = e.Id
                            WHERE s.Name = @ServerName AND e.Name = 'USCommonLogFolder'";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, cn))
                    {
                        cmd.Parameters.AddWithValue("@ServerName", serverName);

                        cn.Open();

                        SqlDataReader reader = cmd.ExecuteReader();

                        if (reader.HasRows)
                            while (reader.Read())
                                directoryName = reader.GetString(0);
                        cn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить рабочую директорию сервера. ServerName: {0}. Текст ошибки: {1}", serverName, ex.Message);
            }

            return directoryName;
        }

        /// <summary>
        /// Метод для получения идентификатора Точки Обслуживания по идентификатору дистрибутива
        /// </summary>
        /// <param name="distributiveId">Идентификатор дистрибутива</param>
        /// <returns></returns>
        internal static int? GetIdToByDistributiveId(int distributiveId)
        {
            int? idTO = null;
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT idTO FROM Distributives WHERE id = @DistrId", cn))
                    {
                        cmd.Parameters.AddWithValue("@DistrId", distributiveId);

                        cn.Open();

                        SqlDataReader reader = cmd.ExecuteReader();

                        if (reader.HasRows)
                            while (reader.Read())
                                idTO = reader.GetInt32(0);
                        cn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить идентификатор ТО из БД. DistrId: {0}. Текст ошибки: {1}", distributiveId, ex.Message);
            }

            return idTO;
        }

        /// <summary>
        /// Метод для получения имени архива с USR файлом по идентификатору дистрибутива
        /// </summary>
        /// <param name="distributiveId">Мдентификатор дистрибутива</param>
        /// <returns></returns>
        public static string GetUsrRarFileNameFromDB(int distributiveId)
        {
            string fileName = null;

            string query = @"SELECT TOP 1 usr_rar_file_name FROM (SELECT start_date, usr_rar_file_name, iu_client_distr_id AS DistrId FROM updateserver.ClientStatistic
                            UNION SELECT (SELECT MIN(Date) FROM updateserver.UpdateAttemptFilesMessages WHERE UpdateFileId = uaf.Id),
                            UsrRarFileName, DistrId FROM updateserver.UpdateAttemptFiles uaf
                            ) as t
                            WHERE usr_rar_file_name <> '-' AND usr_rar_file_name IS NOT NULL AND DistrId = @DistrId
                            ORDER BY t.start_date desc";
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, cn))
                    {
                        cmd.Parameters.AddWithValue("@DistrId", distributiveId);

                        cn.Open();

                        SqlDataReader reader = cmd.ExecuteReader();

                        if (reader.HasRows)
                            while (reader.Read())
                                fileName = reader.GetString(0);
                        cn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить имя USR файла из БД. DistrId: {0}. Текст ошибки: {1}", distributiveId, ex.Message);
            }

            return fileName;
        }
        /// <summary>
        /// Метод для получения экземпляра дистрибутива по его номеру
        /// </summary>
        /// <param name="distrNumber">Номер дистрибутива</param>
        /// <returns></returns>
        public static DBDistributive GetRightDistributive(string distrNumber, int? mainDistributiveIdTo)
        {
            DBDistributive distr = new DBDistributive();

            string query = @"SELECT d.Id, d.SoprType, sdt.Name AS DistrType, CASE WHEN PodklDate IS NULL THEN InstDate ELSE PodklDate END AS PodklDate 
                              FROM Distributives d
                              LEFT JOIN SoftDistrType sdt ON d.idDistrType = sdt.id
                              WHERE Number = @DistrNumber" + (mainDistributiveIdTo.HasValue? " AND idTO = @MainDistrIdTO":"");

            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    cmd.Parameters.Add("@DistrNumber", SqlDbType.VarChar, Int32.MaxValue).Value = distrNumber;
                    if(mainDistributiveIdTo.HasValue)
                        cmd.Parameters.Add("@MainDistrIdTO", SqlDbType.Int).Value = mainDistributiveIdTo.Value;

                    cn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    var distributives = dt.AsEnumerable().Select(r => new
                    {
                        Id = (int)r["Id"],
                        SoprType = r["SoprType"].ToString(),
                        DistrType = r["DistrType"] == DBNull.Value ? null : r["DistrType"].ToString(),
                        PodklDate = Convert.ToDateTime(r["PodklDate"])
                    }).ToList();

                    distr = distributives.Where(d => d.SoprType == "+").Select(d => new DBDistributive
                    {
                        Id = d.Id,
                        DistrType = d.DistrType,
                        SoprType = d.SoprType
                    }).FirstOrDefault();

                    if (distr == null)
                        distr = distributives.OrderByDescending(d => d.PodklDate).Select(d => new DBDistributive
                        {
                            Id = d.Id,
                            DistrType = d.DistrType,
                            SoprType = d.SoprType
                        }).FirstOrDefault();

                    cn.Close();
                    da.Dispose();
                }

                if (distr == null)
                {
                    query = @"SELECT Id FROM distributive_storage.distributive WHERE number = @DistrNumber";

                    using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                    using (SqlCommand cmd = new SqlCommand(query, cn))
                    {
                        cmd.Parameters.Add("@DistrNumber", SqlDbType.VarChar, Int32.MaxValue).Value = distrNumber;
                        cn.Open();

                        SqlDataReader reader = cmd.ExecuteReader();

                        if (reader.HasRows)
                            while (reader.Read())
                                distr = new DBDistributive { Id = reader.GetInt32(0), DistrType = "ОДД" };

                        cn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить дистрибутив из БД. DistrNumber: {0}. Текст ошибки: {1}", distrNumber, ex.Message);
            }
            return distr;
        }
        /// <summary>
        /// Метод для проверки существования записи в БД
        /// </summary>
        /// <param name="tableName">Имя таблицы в БД</param>
        /// <param name="parameters">Параметры запроса</param>
        /// <returns></returns>
        public static bool CheckRecordExisting(string tableName, Dictionary<string, object> parameters)
        {
            bool recordExist = false;

            string query = string.Format(@"SELECT id FROM {0} WHERE ",
                tableName);
            string whereClause = string.Empty;

            foreach (KeyValuePair<string, object> parameter in parameters)
                whereClause += string.IsNullOrEmpty(whereClause) ? string.Format(@"{0} = @{0}", parameter.Key) : string.Format(@" AND {0} = @{0}", parameter.Key);

            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query + whereClause, cn))
                {
                    foreach (KeyValuePair<string, object> parameter in parameters)
                    {
                        SqlParameter param = new SqlParameter(parameter.Key, parameter.Value);
                        cmd.Parameters.Add(param);
                    }

                    cn.Open();

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                        recordExist = true;

                    cn.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке проверить существование записи о пополнении. TableName: {0}. Текст ошибки: {1}", tableName, ex.Message);
            }

            return recordExist;
        }
        #endregion
    }
}
