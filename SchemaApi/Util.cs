using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Timers;
using System.IO;
using System.Collections;
using System.Drawing;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Xml;

namespace SchemaApi
{
    /// <summary>
    /// 单例工具类
    /// </summary>
    class Util
    {
        /// <summary>
        /// 换行符
        /// </summary>
        private static string NewLineChar = "\r\n";

        private static Util utilInstance;

        /// <summary>
        /// 存储所有tag标签对应的布局方式
        /// </summary>
        public Dictionary<string, string> LayoutTagsDic = new Dictionary<string, string>();

        /// <summary>
        /// 存储常用的tag标签
        /// </summary>
        public Dictionary<string, string> UsualTagsDic = new Dictionary<string, string>();

        public static Util GetInstance()
        {
            if (utilInstance == null)
                utilInstance = new Util();
            return utilInstance;
        }

        public Util()
        {
            //初始化LayoutTagsDic
            LayoutTagsDic["div"] = "占满布局";
            LayoutTagsDic["el-tabs"] = "占满布局";
            LayoutTagsDic["el-tab-pane"] = "占满布局";
            LayoutTagsDic["el-container"] = "容器布局";
            LayoutTagsDic["el-aside"] = "容器布局";
            LayoutTagsDic["el-main"] = "容器布局"; //以后不会用到此标签，因为用el-container代替了
            LayoutTagsDic["el-header"] = "容器布局";
            LayoutTagsDic["el-footer"] = "容器布局";
            LayoutTagsDic["el-row"] = "栅格布局";
            LayoutTagsDic["el-col"] = "栅格布局";
            LayoutTagsDic["CetDialog"] = "弹窗页面";
            LayoutTagsDic["CetForm"] = "表单布局";
            LayoutTagsDic["el-form-item"] = "表单布局";

            //初始化UsualTagsDic
            UsualTagsDic["div-label"] = "静态文本";
        }

        /// <summary>
        /// 从路径中获取文件名
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public string GetFileName(string filePath)
        {
            int index = filePath.LastIndexOf("\\");
            return filePath.Substring(index + 1, filePath.Length - index - 1);
        }

        /// <summary>
        /// 导出数据
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="TextAll"></param>
        /// <param name="isExportAfterTransFile">是否导出翻译后文本</param>
        /// <returns></returns>
        public bool ExportToTxt(string filePath, string TextAll, Encoding encodeType)
        {
            StreamWriter sw;
            try
            {
                if (!File.Exists(filePath))
                {
                    //如果不存在该路径则先创建该路径
                    int index = filePath.LastIndexOf("\\");
                    string path = filePath.Substring(0, index);
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }
                if (File.Exists(filePath) && File.GetAttributes(filePath).ToString().Contains("ReadOnly")) //去掉文件的只读属性
                    File.SetAttributes(filePath, FileAttributes.Normal);

                sw = new StreamWriter(filePath, false, encodeType); //直接覆盖
                sw.WriteLine(TextAll);
                sw.Close();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否是不带 BOM 的 UTF8 格式
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1; //计算当前正分析的字符应还有的字节数
            byte curByte; //当前分析的字节.
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前
                        while (((curByte <<= 1) & 0x80) != 0)
                            charByteCounter++;

                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X 
                        if (charByteCounter == 1 || charByteCounter > 6)
                            return false;
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1
                    if ((curByte & 0xC0) != 0x80)
                        return false;

                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("非预期的byte格式");
            }
            return true;
        }

        public Encoding GetEncodeType(string filePath)
        {
            return Encoding.UTF8; //固定UTF8编码方式

            byte[] fileContents = File.ReadAllBytes(filePath);
            //读取不同类型的编码的preamble，utf8是239，187，191；ASCII没有preamble；
            //BigEndianUnicode的是254，255；unicode little endian的是255，254；UTF32 little endian的是255，254，0，0；
            //UTF32 big endian 的是0，0，254，255；其他的没有
            if (IsUTF8Bytes(fileContents) || (fileContents[0] == 239 && fileContents[1] == 187 && fileContents[2] == 191))
                return Encoding.UTF8;
            else if (fileContents[0] == 255 && fileContents[1] == 254)
                return Encoding.Unicode;
            else if (fileContents[0] == 254 && fileContents[1] == 255)
                return Encoding.BigEndianUnicode;
            return Encoding.Default;
        }

        /// <summary>
        /// 对查询关键字进行处理，特别是[]%等特殊字符前面要加上/进行处理
        /// </summary>
        /// <param name="keyWord"></param>
        /// <returns></returns>
        public string FormatKeyword(string keyWord)
        {
            string resultStr = string.Empty;
            resultStr = keyWord.Trim();
            resultStr = resultStr.Replace("[", "[[]");//此句一定要在最前
            resultStr = resultStr.Replace("%", "[%]");
            resultStr = resultStr.Replace("_", "[_]");
            resultStr = resultStr.Replace("^", "/^");
            resultStr = resultStr.Replace(@"\", @"/\");
            resultStr = resultStr.Replace("'", "''");
            resultStr = resultStr.Replace("/", "//");
            return resultStr;
        }

        /// <summary>
        /// 判断是否为中文汉字
        /// </summary>
        /// <param name="targetChar"></param>
        /// <returns></returns>
        public bool IsChineseChar(char targetChar)
        {
            return 0x4e00 <= targetChar && targetChar <= 0x9fa5;
        }

        /// <summary>
        /// 判断字符串中是否含有中文汉字或中文字符
        /// </summary>
        /// <param name="targetStr"></param>
        /// <returns></returns>
        public bool ContainChineseChar(string targetStr)
        {
            char[] ch = targetStr.ToCharArray();
            for (int i = 0; i < ch.Length; i++)
            {
                if (IsChineseChar(ch[i]))
                    return true;

                byte[] bArry = System.Text.Encoding.Unicode.GetBytes(ch[i].ToString());
                if (bArry.Length == 2 && bArry[1] != 0)
                    return true; //有两个字节的就为中文字符，已用，。；？！：‘“”’【】（）测试通过。                
            }
            return false;
        }

        public JObject ParseJObjectFromStr(string jsonStr)
        {
            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(jsonStr);
                return jo;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public T ParseObjectFromStr<T>(string jsonStr)
        {
            T result = default(T);
            try
            {
                result = JsonConvert.DeserializeObject<T>(jsonStr);
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public JArray ParseJArrayFromStr(string jsonStr)
        {
            try
            {
                JArray jo = (JArray)JsonConvert.DeserializeObject(jsonStr);
                return jo;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message + NewLineChar + ex.StackTrace, ex.Message);
                return null;
            }
        }

        public string ParseObjectToStr(object jObject, bool removeQuote = true)
        {
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.Formatting = Newtonsoft.Json.Formatting.Indented;
                settings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;//指定如何处理循环引用，None--不序列化，Error-抛出异常，Serialize--仍要序列化
                string result = JsonConvert.SerializeObject(jObject, settings);
                if (removeQuote)
                    result = JsonRegex(result);
                return result;
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 修改value后，判断value是json对象还是数组，进行处理
        /// </summary>
        /// <param name="jo"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public bool EndEditingValue(JObject jo, string key, string value)
        {
            var oldValue = jo[key];
            try
            {
                if (oldValue != null && oldValue.Type == JTokenType.Object) //说明是json字符串，需要先反序列化成对象
                {
                    jo[key] = (JObject)JsonConvert.DeserializeObject(value);
                }
                else if (oldValue != null && oldValue.Type == JTokenType.Array) //说明是数组字符串，需要先反序列化成数组
                {
                    jo[key] = (JArray)JsonConvert.DeserializeObject(value);
                }
                else if (oldValue != null && oldValue.Type == JTokenType.Boolean)
                    jo[key] = Convert.ToBoolean(value);
                else if (oldValue != null && oldValue.Type == JTokenType.Integer)
                    jo[key] = Convert.ToInt32(value);
                else
                    jo[key] = value;
            }
            catch (Exception ex)
            {
                jo[key] = oldValue;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 去除json key双引号
        /// </summary>
        /// <param name="jsonInput">json</param>
        /// <returns>去除key引号</returns>
        public string JsonRegex(string jsonInput)
        {
            string result = string.Empty;
            try
            {
                string pattern = "\"(\\w+)\"(\\s*:\\s*)";
                string replacement = "$1$2";
                System.Text.RegularExpressions.Regex rgx = new System.Text.RegularExpressions.Regex(pattern);
                result = rgx.Replace(jsonInput, replacement);
            }
            catch (Exception ex)
            {
                result = jsonInput;
            }
            return result;
        }
        
        /// <summary>
        /// 首字母小写
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FirstCharToLower(string input)
        {
            if (String.IsNullOrEmpty(input))
                return input;
            string str = input.First().ToString().ToLower() + input.Substring(1);
            return str;
        }

        /// <summary>
        /// 首字母大写
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                return input;
            string str = input.First().ToString().ToUpper() + input.Substring(1);
            return str;
        }

        ///<summary>
        ///取出文本中间内容，其中获取right字符串的时候是从left字符串右边最后1个匹配的
        ///<summary>
        ///<param name="left">左边文本</param>
        ///<param name="right">右边文本</param>
        ///<param name="text">全文本</param>
        ///<return>完事返回成功文本|没有找到返回no</return>
        public static string TextGainCenter_lastRight(string left, string right, string text)
        {
            //判断是否为null或者是empty
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            //判断是否为null或者是empty

            int Lindex = string.IsNullOrEmpty(left) ? 0 : text.IndexOf(left); //搜索left的位置

            if (Lindex == -1)
            { //判断是否找到left
                return string.Empty;
            }

            Lindex = Lindex + left.Length; //取出left右边文本起始位置

            int Rindex = string.IsNullOrEmpty(right) ? text.Length - 1 : text.LastIndexOf(right);//从left的右边开始寻找right

            if (Rindex == -1)
            {//判断是否找到right
                return string.Empty;
            }
            if (Lindex > Rindex)
            {
                return string.Empty;
            }

            return text.Substring(Lindex, Rindex - Lindex);//返回查找到的文本
        }

        ///<summary>
        ///取出文本中间内容，其中获取right字符串的时候是从left字符串右边开始第1个匹配的
        ///<summary>
        ///<param name="left">左边文本</param>
        ///<param name="right">右边文本</param>
        ///<param name="text">全文本</param>
        ///<return>完事返回成功文本|没有找到返回no</return>
        public static string TextGainCenter_firstRight(string left, string right, string text)
        {
            //判断是否为null或者是empty
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            //判断是否为null或者是empty

            int Lindex = string.IsNullOrEmpty(left) ? 0 : text.IndexOf(left); //搜索left的位置

            if (Lindex == -1)
            { //判断是否找到left
                return string.Empty;
            }

            Lindex = Lindex + left.Length; //取出left右边文本起始位置

            string rightPart = text.Substring(Lindex);
            int Rindex = string.IsNullOrEmpty(right) ? text.Length - 1 : rightPart.IndexOf(right) + Lindex;//从left的右边开始寻找right

            if (Rindex == -1)
            {//判断是否找到right
                return string.Empty;
            }

            return text.Substring(Lindex, Rindex - Lindex);//返回查找到的文本
        }

        public static bool CheckKeyword(List<string> items, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return true;

            foreach (var item in items)
            {
                if (item.ToLower().Contains(keyword.ToLower()))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 读取指定路径下的文本文件中的所有文本
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string ReadStrFromFile(string filePath)
        {
            if(!File.Exists(filePath))
            {
                return string.Empty;
            }
            string text = File.ReadAllText(filePath, Util.GetInstance().GetEncodeType(filePath));
            //如果源文件的最后一个字符不是换行符则添加一个换行符，否则在获取未翻译词条的时候会异常
            if (!text.EndsWith("\r\n"))
                text += "\r\n";
            return text;
        }

        public static string FormatXml(XmlDocument xml)
        {
            XmlDocument xd = xml as XmlDocument;
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            XmlTextWriter xtw = null;
            try
            {
                xtw = new XmlTextWriter(sw);
                xtw.Formatting = System.Xml.Formatting.Indented;
                xtw.Indentation = 1;
                xtw.IndentChar = '\t';
                xd.WriteTo(xtw);
            }
            finally
            {
                if (xtw == null)
                    xtw.Close();
            }
            return sb.ToString();
        }
    }
}
