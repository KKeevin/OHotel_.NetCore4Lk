using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Reflection;

namespace OHotel.NETCoreMVC.Helper
{
    public class AppHelper
    {
        /// <summary>
        /// 調整 model數值,防止SQL 注入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tt"></param>
        /// <returns></returns>
        public static T ModelResetValueInjection<T>(T tt) where T : class, new()
        {
            T result = default;
            foreach (PropertyInfo property in tt.GetType().GetProperties())
            {
                var Key = property.Name;
                var Value = property.GetValue(tt, null);
                if (Value?.GetType() == typeof(int) && Value != null)
                    Value = Convert.ToInt32(Value);
                else if (Value?.GetType() == typeof(string))
                    Value = Value?.ToString().Replace("'", "").Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;").Replace(" ", "&nbsp;&nbsp;");
                property.SetValue(tt, Value);
                //mgrMenuJsonConvert.DeserializeObject<Person>(JsonConvert.SerializeObject(source));
                //_RETURN += "Key="+ Key+ "/Value="+ Value+ "/type="+ type+"\r\n";
            }
            result = tt;
            return result;
        }
        /// <summary>
        /// 根據網址取得 IP位置 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string? GetDomianGetHost(string? url)
        {
            string? host = default;
            try
            {
                if (!String.IsNullOrEmpty(url)) host = new Uri(url).Host;
            }
            catch { }
            return host;
        }
        /// <summary>
        /// hostname　取得ＩＰ位置，這個有點奇怪　如果放在相同服務器上　會顯示 127.0.0.1
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static string? GetHostIp(string host)
        {
            string? ip = default;
            try
            {
                if (!String.IsNullOrEmpty(host))
                    ip = System.Net.Dns.GetHostAddresses(host).GetValue(0)?.ToString();
            }
            catch { }
            return ip;
        }
        /// <summary>
        /// IP 取得 Host
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static string? GetIpHost(string ip)
        {
            string? host = default;
            try
            {
                if (!String.IsNullOrEmpty(ip))
                    host = System.Net.Dns.GetHostEntry(ip).HostName;
            }
            catch { }
            return host;
        }
        public static string? GetServerIpAddresses()
        {

            string? serverIpAddresses = string.Empty;
            //--取得NetworkInterfaces 所有資訊
            var networks = NetworkInterface.GetAllNetworkInterfaces();
            serverIpAddresses = networks.FirstOrDefault()?.GetIPProperties().UnicastAddresses.Where(p => p.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(p.Address)).FirstOrDefault()?.Address.ToString();
            /*foreach (var network in networks)
            {
                var ipAddress = network.GetIPProperties().UnicastAddresses.Where(p => p.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(p.Address)).FirstOrDefault()?.Address.ToString();

                serverIpAddresses += network.Name + ":" + ipAddress + "|";
            }*/
            return serverIpAddresses;
        }
        /// <summary>
        /// 判斷是否為有效網址
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static bool IsValidUri(string uri)
        {
            Uri validatedUri;
            return Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out validatedUri);
        }
    }
}
