using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;

namespace OHotel.NETCoreMVC.Helper
{
    /// <summary>通用輔助方法</summary>
    public static class AppHelper
    {
        /// <summary>調整 model 數值，防止 SQL 注入與 XSS</summary>
        public static T ModelResetValueInjection<T>(T tt) where T : class, new()
        {
            foreach (var property in typeof(T).GetProperties())
            {
                var value = property.GetValue(tt, null);
                if (value is string s)
                    value = s.Replace("'", "").Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;").Replace(" ", "&nbsp;&nbsp;");
                property.SetValue(tt, value);
            }
            return tt;
        }

        /// <summary>從網址取得 Host</summary>
        public static string? GetDomainHost(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
        }

        [Obsolete("請使用 GetDomainHost，此方法名稱為拼字錯誤保留")]
        public static string? GetDomianGetHost(string? url) => GetDomainHost(url);

        /// <summary>從 hostname 取得 IP</summary>
        public static string? GetHostIp(string? host)
        {
            if (string.IsNullOrEmpty(host)) return null;
            try
            {
                var addresses = Dns.GetHostAddresses(host);
                return addresses.Length > 0 ? addresses[0].ToString() : null;
            }
            catch { return null; }
        }

        /// <summary>從 IP 取得 HostName</summary>
        public static string? GetIpHost(string? ip)
        {
            if (string.IsNullOrEmpty(ip)) return null;
            try { return Dns.GetHostEntry(ip).HostName; }
            catch { return null; }
        }

        /// <summary>取得本機對外 IP</summary>
        public static string? GetServerIpAddresses()
        {
            var network = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up);
            if (network == null) return null;
            var ip = network.GetIPProperties().UnicastAddresses
                .FirstOrDefault(p => p.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(p.Address));
            return ip?.Address.ToString();
        }

        /// <summary>判斷是否為有效網址</summary>
        public static bool IsValidUri(string? uri) =>
            !string.IsNullOrEmpty(uri) && Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out _);
    }
}
