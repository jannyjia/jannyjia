using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaApi.Controllers
{
    public class SqlliteStructure{
        /// <summary>
        /// column
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 中文名称
        /// </summary>
        public string desc { get; set; }

        /// <summary>
        /// varchar, string, int, 
        /// </summary>
        public string colType { get; set; }
    }
    [Route("schemaapi/[controller]/[action]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        /// <summary>
        /// Excel返回数据
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<string> GetExcelJsonAsync(IFormFile files)
        {
            var filePath = @"D:\UploadingFiles\" + files.FileName;

            if (files.Length > 0)
            {
                using (var stream = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    files.CopyTo(stream);
                    stream.Flush();
                }

                FileInfo file = new FileInfo(filePath);
                using (ExcelPackage package = new ExcelPackage(file))
                {
                    StringBuilder sb = new StringBuilder();
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                    int rowCount = worksheet.Dimension.Rows;
                    int ColCount = worksheet.Dimension.Columns;
                    bool bHeaderRow = true;
                    for (int row = 1; row <= rowCount; row++)
                    {
                        for (int col = 1; col <= ColCount; col++)
                        {
                            var value = worksheet.Cells[row, col].Value;
                            if (value == null)
                                value = string.Empty;
                            if (bHeaderRow)
                            {
                                sb.Append(value + "\t");
                            }
                            else
                            {
                                sb.Append(value + "\t");
                            }
                        }
                        sb.Append(Environment.NewLine);
                    }
                    return Content(sb.ToString());
                }
            }

            return "test";
        }

        /// <summary>
        /// sqllite返回表结构
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<string> GetSqlliteJson(IFormFile files)
        {
            JObject result = new JObject();

            var filePath = Directory.GetCurrentDirectory() + "/UploadingFiles/" + files.FileName;
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "/UploadingFiles"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/UploadingFiles");
            }
            
            if (files.Length > 0)
            {
                if (!System.IO.File.Exists(filePath))
                {
                    System.IO.File.Create(filePath).Close();//创建该文件
                }
                using (var stream = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    files.CopyTo(stream);
                    stream.Flush();
                }

                SqliteConnection cnn = new SqliteConnection();
                cnn.ConnectionString = "Data Source=" + filePath;
                cnn.Open();

                string sql = "select * from sqlite_master where type=\"table\"";
                SqliteCommand cmd = cnn.CreateCommand();
                cmd.CommandText = sql;

                SqliteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var tempStr = reader.GetString(4);
                    string[] rows = tempStr.Split(Environment.NewLine);
                    string tableName = reader.GetString(1);
                    JArray infoList = new JArray();
                    foreach (var row in rows)
                    {
                        string sql2 = "PRAGMA table_info('" + tableName + "')";
                        SqliteCommand cmd2 = cnn.CreateCommand();
                        cmd2.CommandText = sql2;
                        SqliteDataReader reader2 = cmd2.ExecuteReader();
                        while (reader2.Read())
                        {
                            JObject info = new JObject();
                            info["name"] = reader2.GetString(1);
                            info["colType"] = reader2.GetString(2);
                            info["desc"] = "";
                            infoList.Add(info);
                        }
                        //if (row.Contains("CREATE TABLE"))
                        //    tableName = Util.TextGainCenter_firstRight("\"", "\"", row);
                        //if (row.StartsWith("\"")) //说明是列
                        //{
                        //    string[] fields = row.Split(" ");
                        //    if (fields.Length < 3)
                        //        continue;
                        //    JObject info = new JObject();
                        //    info["name"] = Util.TextGainCenter_firstRight("\"", "\"", fields[0]);
                        //    info["colType"] = fields[1];
                        //    info["desc"] = fields[2].Trim();
                        //    infoList.Add(info);
                        //}
                    }
                    if (!string.IsNullOrWhiteSpace(tableName))
                        result[tableName] = infoList;
                }
                cnn.Close();
            }

            return "{\"result\":" + result.ToString()+"}";
        }
    }
}
