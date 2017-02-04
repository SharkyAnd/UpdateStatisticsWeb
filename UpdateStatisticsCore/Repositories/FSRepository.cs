using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateStatisticsCore.Models;

namespace UpdateStatisticsCore.Repositories
{
    /// <summary>
    /// File System Repositroy
    /// </summary>
    public static class FSRepository
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

            string folderName = startDate.Value.ToString("yyyy_MM_dd");
            string timePartFileName = startDate.Value.ToString("HH_mm_ss");
            string workFolderPath = new DirectoryInfo(DBRepository.GetServerDirectory(serverName)).FullName;
            string distrNumber = DBRepository.GetDistrNumberById(distributiveId);

            try
            {
                FileInfo logFile = new DirectoryInfo(string.Format(@"{0}\Logs\{1}", workFolderPath, folderName)).GetFiles()
                    .Where(f => f.Name.Contains(string.Format(@"{0}#{1}", distrNumber, timePartFileName)) && f.Extension == ".log" && !f.Name.Contains("result")).FirstOrDefault();

                if (logFile == null)
                    return null;

                string[] unformatLines = File.ReadAllLines(logFile.FullName, Encoding.Default);

                foreach (var item in unformatLines)
                {
                    string[] words = item.Split(' ');
                    string formatMessage = "<b>" + words[1] + "</b><em>";
                    for (int i = 7; i < words.Count(); i++)
                        formatMessage += " " + words[i];
                    formatMessage += "</em></br>";
                    updateLog.MessageList += formatMessage;
                }
                updateLog.QstList = GetQstFiles(logFile);
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
        private static List<QST> GetQstFiles(FileInfo file)
        {
            List<QST> qst = new List<QST>();

            string resultFileName = string.Format(@"{0}_result.log", file.Name.Split('.')[0]);
            string directoryPath = file.Directory.FullName;

            string resultFilePath = string.Format(@"{0}\{1}", directoryPath, resultFileName);

            if (!File.Exists(resultFilePath))
                return null;

            string[] unformatLines = File.ReadAllLines(resultFilePath, Encoding.Default);
            for (int i = 2; i < unformatLines.Count(); i++)
            {
                var item = unformatLines[i];
                string[] words = item.Split(';');
                QST currentQst = new QST();
                currentQst.QstFileName = words[1];
                int qstCode = Convert.ToInt32(words[0]);

                GetQstDecryption(qstCode, currentQst);

                qst.Add(currentQst);
            }

            return qst;
        }
        /// <summary>
        /// Метод для получения расшифровки кода обработки QST файла
        /// </summary>
        /// <param name="qstCode">Код</param>
        /// <param name="currentQst">Экземпляр QST файла</param>
        private static void GetQstDecryption(int qstCode, QST currentQst)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(@"SELECT Description, Color FROM updateserver.QstHandbook WHERE Code = @Code", cn))
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@Code", qstCode);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while(reader.Read())
                    {
                        currentQst.QstStatusDescription = reader.GetString(0);
                        currentQst.QstStatusColor = reader.GetString(1);
                    }

                    cn.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить расшифровку QST файла. QstCode: {0}. Текст ошибки: {1}", qstCode, ex.Message);
            }
        }
    }
}
