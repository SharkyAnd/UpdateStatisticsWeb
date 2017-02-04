using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UpdateStatisticsCore.Models;
using UpdateStatisticsCore.Repositories;

namespace UpdateStatisticsCore.Providers
{
    public partial class ClientInfoProvider
    {
        public enum UsrFileGetMode
        {
            Open,
            Download
        }
        /// <summary>
        /// Метод для получения расшифрованного USR файла клиента
        /// </summary>
        /// <param name="distributiveId">Идентификатор дистрибутива</param>
        /// <param name="distributiveNumber">Номер дистрибутива</param>
        /// <param name="serverName">Имя сервера</param>
        /// <returns></returns>
        public UsrFile GetUsrFileDecryption(int distributiveId, string distributiveNumber, string serverName)
        {
            UsrFile usrFileInstance = new UsrFile();

            FileInfo usrFile = GetUsrFile(distributiveId, distributiveNumber, UsrFileGetMode.Open, serverName);

            if (usrFile == null)
                return null;

            usrFileInstance.UsrFileDate = GetUsrFileLastWriteTime(distributiveId, serverName);
            usrFileInstance.UsrSystems = GetUsrFileSystemsFromXML(usrFile.FullName, distributiveId);
            usrFileInstance.InUsrFileDate = GetInUsrFileDate(usrFile.FullName);

            return usrFileInstance;
        }
        /// <summary>
        /// Метод для получения даты последней записи в USR файл
        /// </summary>
        /// <param name="distributiveId">Идентификатор дистрибутива</param>
        /// <param name="serverName">Имя сервера</param>
        /// <returns></returns>
        private DateTime? GetUsrFileLastWriteTime(int distributiveId, string serverName)
        {
            string rarFileName = DBRepository.GetUsrRarFileNameFromDB(distributiveId);
            string serverDirectory = new DirectoryInfo(DBRepository.GetServerDirectory(serverName)).FullName;

            return new FileInfo(string.Format(@"{0}\Reports\{1}", serverDirectory, rarFileName)).LastWriteTime;
        }
        /// <summary>
        /// Метод для получения экземпляра USR файла
        /// </summary>
        /// <param name="distributiveId">Идентификатор дистрибутива</param>
        /// <param name="distributiveNumber">Номер дистрибутива</param>
        /// <param name="mode">Режим получения файла</param>
        /// <param name="serverName">Имя сервера</param>
        /// <returns></returns>
        public FileInfo GetUsrFile(int distributiveId, string distributiveNumber, UsrFileGetMode mode, string serverName)
        {
            string rarFileName = DBRepository.GetUsrRarFileNameFromDB(distributiveId);
            if (string.IsNullOrEmpty(rarFileName))
                return null;

            string serverDirectory = DBRepository.GetServerDirectory(serverName);
            if (string.IsNullOrEmpty(serverDirectory))
                return null;

            serverDirectory = new DirectoryInfo(DBRepository.GetServerDirectory(serverName)).FullName;

            FileInfo usrFile = null;
            DirectoryInfo tempWorkDir = null;
            try
            {
                tempWorkDir = Directory.CreateDirectory(string.Format(@"{0}\temp_{1}", UpdateStatisticsConfig.Instance.TempDirectory, distributiveId));              
            }
            catch(Exception ex)
            {
                /*using (StreamWriter sw = new StreamWriter($@"{UpdateStatisticsConfig.Instance.TempDirectory}\log.txt", true))
                    sw.WriteLine(ex.Message);*/
            }
            string tempWorkDirPath = tempWorkDir.FullName;
            CleanUp(tempWorkDirPath);
            try
            {
                File.Copy(string.Format(@"{0}\Reports\{1}", serverDirectory, rarFileName), string.Format(@"{0}\{1}", tempWorkDirPath, rarFileName));

                File.Copy(string.Format(@"{0}\7za.exe", serverDirectory), string.Format(@"{0}\7za.exe", tempWorkDirPath));
                Process unrar = new Process();
                unrar.StartInfo.FileName = string.Format(@"{0}\7za.exe", tempWorkDirPath);
                unrar.StartInfo.CreateNoWindow = true;
                unrar.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                unrar.StartInfo.WorkingDirectory = tempWorkDirPath;
                unrar.StartInfo.Arguments = string.Format(@"e {0}", rarFileName);
                unrar.Start();
                unrar.WaitForExit();
                File.Delete(string.Format(@"{0}\{1}", tempWorkDirPath, rarFileName));
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке разархивировать архив с USR файлом. DistrId: {0}. ServerName: {1}. Текст ошибки: {2}", distributiveId, serverName, ex.Message);
            }

            try
            {
                File.Copy(string.Format(@"{0}\uinf4000.exe", serverDirectory), string.Format(@"{0}\uinf4000.exe", tempWorkDirPath));
                Process uinf = new Process();
                uinf.StartInfo.FileName = string.Format(@"{0}\uinf4000.exe", tempWorkDirPath);
                uinf.StartInfo.WorkingDirectory = tempWorkDirPath;
                uinf.StartInfo.CreateNoWindow = true;
                uinf.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                uinf.Start();
                uinf.WaitForExit();
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке расшифровать USR файл. DistrId: {0}. ServerName: {1}. Текст ошибки: {2}", distributiveId, serverName, ex.Message);
            }

            string fileExtension = null;
            switch (mode)
            {
                case UsrFileGetMode.Open:
                    fileExtension = ".XML";
                    break;
                case UsrFileGetMode.Download:
                    fileExtension = ".USR";
                    break;
                default:
                    break;
            }

            usrFile = tempWorkDir
                .GetFiles()
                .Where(f => f.Extension.ToUpper() == fileExtension && f.Name.Split('#')[1].Contains(distributiveNumber))
                .FirstOrDefault();

            return usrFile;
        }
        /// <summary>
        /// Метод для получения списка систем из USR файла
        /// </summary>
        /// <param name="filePath">Полный путь до файла</param>
        /// <returns></returns>
        private List<UsrSystems> GetUsrFileSystemsFromXML(string filePath, int distributiveId)
        {
            List<UsrSystems> usrSystems = new List<UsrSystems>();
            try
            {
                XDocument doc = XDocument.Load(filePath);
                foreach (XElement el in doc.Root.Elements())
                {
                    if (el.Name == "ib")
                    {
                        foreach (XElement ib in el.Elements())
                        {
                            foreach (XElement update in ib.Elements())
                            {
                                XElement u1 = update.Element("u1");
                                XElement u2 = update.Element("u2");
                                XElement u3 = update.Element("u3");
                                XElement u4 = update.Element("u4");

                                usrSystems.Add(new UsrSystems
                                {
                                    SystemName = ib.Attribute("name").Value + "(" + ib.Attribute("directory").Value + ")",
                                    DistributiveNumber = ib.Attribute("nDistr").Value,
                                    Computer = ib.Attribute("nComp").Value == "1" ? null : ib.Attribute("nComp").Value,
                                    SystemDirectory = ib.Attribute("directory").Value,
                                    UpdateDateWithDocs1 = u1.Attribute("sysdate").Value + " " + u1.Attribute("time").Value.Replace('.', ':') + ":00" + "\\" + u1.Attribute("docs").Value,
                                    UpdateDateWithDocs2 = u2.Attribute("sysdate").Value + " " + u2.Attribute("time").Value.Replace('.', ':') + ":00" + "\\" + u2.Attribute("docs").Value,
                                    UpdateDateWithDocs3 = u3.Attribute("sysdate").Value + " " + u3.Attribute("time").Value.Replace('.', ':') + ":00" + "\\" + u3.Attribute("docs").Value,
                                    UpdateDateWithDocs4 = u4.Attribute("sysdate").Value + " " + u4.Attribute("time").Value.Replace('.', ':') + ":00" + "\\" + u4.Attribute("docs").Value,
                                    UpdateKind = u1.Attribute("kind").Value
                                });

                            }
                        }
                    }
                }
                FilterUSRSystems(usrSystems, distributiveId);
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке расшифровать USR файл. Путь к файлу: {0}. Текст ошибки: {1}", filePath, ex.Message);
                return null;
            }

            return usrSystems;
        }
        /// <summary>
        /// Метод для установки окраса строк в таблице систем USR файла
        /// </summary>
        /// <param name="UsrSystems"></param>
        private void FilterUSRSystems(List<UsrSystems> UsrSystems, int distributiveId)
        {
            string distrNumber = null;

            foreach (var system in UsrSystems)
            {
                if (!string.IsNullOrEmpty(system.Computer))
                    distrNumber = system.DistributiveNumber + "." + system.Computer;
                else
                    distrNumber = system.DistributiveNumber;
                int? mainDistributiveidTO = DBRepository.GetIdToByDistributiveId(distributiveId);
                if (system.DistributiveNumber == "1")
                    system.DistrStatus = "#FFFFFF";

                DBDistributive distr = DBRepository.GetRightDistributive(distrNumber, mainDistributiveidTO);
                try
                {
                    DateTime? date;
                    distrNumber = system.DistributiveNumber;
                    string datePart = system.UpdateDateWithDocs1.Split('\\')[0];
                    date = datePart == "0 00:00:00" ? (DateTime?)null : DateTime.Parse(datePart);

                    TimeSpan result = DateTime.Now - date.Value;
                    if (system.UpdateKind == "R")
                        system.UpdateStatus = "#95A5A6";
                    else
                    {
                        if (system.DistributiveNumber == "1")
                            system.UpdateStatus = "#FFFFFF";
                        else
                        {
                            if (DateTime.Now.Date == date.Value.Date)
                                system.UpdateStatus = "#a1f57e";
                            else if (result > TimeSpan.FromDays(7))
                                system.UpdateStatus = "#f38888";
                        }
                    }

                    if (distr != null)
                    {
                        if (system.DistributiveNumber == "1")
                            system.DistrStatus = "#FFFFFF";
                        else
                        {
                            if (distr.DistrType == "ОДД")
                                system.DistrStatus = "#54b1f5";
                            else
                            {
                                if (distr.SoprType == "+")
                                    system.DistrStatus = "#a1f57e";
                                else
                                    system.DistrStatus = "#fbfc96";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().Error("Ошибка при попытке определить статус систем из USR фала. Номер дистрибутива: {0}. Текст ошибки: {1}", system.DistributiveNumber, ex.Message);
                }
            }
        }
        /// <summary>
        /// Метод для получения даты формирования USR файла, записанной в нем
        /// </summary>
        /// <param name="filePath">Полный путь к файлу</param>
        /// <returns></returns>
        private DateTime? GetInUsrFileDate(string filePath)
        {
            DateTime? date = null;
            try
            {
                XDocument doc = XDocument.Load(filePath);
                foreach (XElement el in doc.Root.Elements())
                {
                    bool outerBreak = false;
                    if (el.Name == "files")
                    {
                        foreach (XElement filesEl in el.Elements())
                        {
                            if (filesEl.Name == "USR_FILE")
                            {
                                date = DateTime.ParseExact(filesEl.Attribute("date").Value + " " + filesEl.Attribute("time").Value, "dd.MM.yyyy HH.mm.ss", CultureInfo.InvariantCulture);
                                outerBreak = true;
                                break;
                            }
                        }
                        if (outerBreak)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить дату из USR файла. Путь к файлу: {0}. Текст ошибки: {1}", filePath, ex.Message);
            }
            return date;
        }
        /// <summary>
        /// Метод для получения экземпляра архива с USR файлом
        /// </summary>
        /// <param name="distributiveId">Идентификатор дистрибутива</param>
        /// <param name="serverName">Имя сервера</param>
        /// <returns></returns>
        public FileInfo GetRarFile(int distributiveId, string serverName)
        {
            string rarFileName = DBRepository.GetUsrRarFileNameFromDB(distributiveId);
            if (string.IsNullOrEmpty(rarFileName))
                return null;

            string serverDirectory = DBRepository.GetServerDirectory(serverName);
            if (string.IsNullOrEmpty(serverDirectory))
                return null;

            serverDirectory = new DirectoryInfo(DBRepository.GetServerDirectory(serverName)).FullName;

            try
            {
                return new FileInfo(string.Format(@"{0}\Reports\{1}", serverDirectory, rarFileName));
            }
            catch(Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить архив с USR файлом. DistributiveId: {0}. ServerName: {1}. Текст ошибки: {2}", distributiveId, serverName, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Чистка директории
        /// </summary>
        /// <param name="dirName">Путь до директории</param>
        private void CleanUp(string dirName)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(dirName);
                if (dir.Exists)
                {
                    foreach (FileInfo file in dir.GetFiles())
                    {
                        file.Delete();
                    }
                }
            }
            catch(Exception ex)
            {
                using (StreamWriter sw = new StreamWriter(@"D:\Temp\log.txt", true))
                    sw.WriteLine(ex.Message);
            }
        }
    }
}
