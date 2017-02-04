using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateStatisticsCore.Models;
using System.Configuration;

namespace UpdateStatisticsCore
{
    public sealed class UpdateStatisticsConfig
    {
        private static UpdateStatisticsConfig _instance;

        public UpdateStatisticsConfig() { }

        public static UpdateStatisticsConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UpdateStatisticsConfig();
                    _instance.FillProperties();
                }
                return _instance;
            }
        }

        private void FillProperties()
        {
            _instance.ConnectionString = LogManager.Configuration.Variables["ConnectionString"].Text;
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM updateserver.UpdateMessagesRules", cn))
                {
                    cn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);

                    cn.Close();
                }

                _instance.UpdateMessagesRules = dt.AsEnumerable().Select(r => new UpdateMessageRule
                {
                    IPSMessage = r["IPSMessage"].ToString(),
                    Message = r["Message"] == DBNull.Value ? null : r["Message"].ToString()
                }).ToList();

                using (SqlConnection cn = new SqlConnection(UpdateStatisticsConfig.Instance.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM updateserver.Environments WHERE CommonValue IS NOT NULL", cn))
                {
                    cn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);

                    cn.Close();
                }

                var settings = dt.AsEnumerable().Select(r => new
                {
                    Name = r["Name"].ToString(),
                    CommonValue = r["CommonValue"].ToString()
                });

                foreach (var setting in settings)
                {
                    switch(setting.Name)
                    {
                        case "tempDir":
                            _instance.TempDirectory = setting.CommonValue;
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке получить список клиентов, обновляющихся в данный момент. Текст ошибки: {0}", ex.Message);
            }
        }

        private string _connectionString = @"data source=EPSILON\SQLEXPRESS;initial catalog=consbase;Password=f1r0e0k8by;User ID=IIS Apps";
        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        private List<UpdateMessageRule> _updateMessagesRules;
        public List<UpdateMessageRule> UpdateMessagesRules
        {
            get { return _updateMessagesRules; }
            set { _updateMessagesRules = value; }
        }

        private string _tempDirectory;
        public string TempDirectory
        {
            get { return _tempDirectory; }
            set { _tempDirectory = value; }
        }
    }
}
