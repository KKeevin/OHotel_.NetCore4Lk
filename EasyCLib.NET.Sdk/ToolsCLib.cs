using System.Text.RegularExpressions;

namespace EasyCLib.NET.Sdk
{
    public class ToolsCLib : IToolsCLib
    {
        /// <summary>
        /// 移除特殊字元，防止 SQL 注入
        /// </summary>
        public string SpecialChar(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, @"[^\p{L}\p{N}_.-]", "", RegexOptions.None);
        }
    }
}
