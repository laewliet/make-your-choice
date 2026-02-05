using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MakeYourChoice
{
    public class AwsIpService
    {
        private const string IpRangesUrl = "https://ip-ranges.amazonaws.com/ip-ranges.json";
        private List<AwsPrefix> _prefixes;

        public async Task InitializeAsync()
        {
            try
            {
                using var client = new HttpClient();
                var json = await client.GetStringAsync(IpRangesUrl);
                var root = JsonSerializer.Deserialize<AwsIpRangesRoot>(json);
                _prefixes = root?.Prefixes?
                    .Where(p => string.Equals(p.Service, "EC2", StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? new List<AwsPrefix>();
                
            }
            catch (Exception ex)
            {
                // Log or handle error
                Console.WriteLine($"Failed to fetch AWS IP ranges: {ex.Message}");
                _prefixes = new List<AwsPrefix>();
            }
        }

        public string GetRegionForIp(string ipAddress)
        {
            if (_prefixes == null || !_prefixes.Any()) return null;

            if (!IPAddress.TryParse(ipAddress, out var ip)) return null;

            // Simple linear search. Optimizations like Interval Tree could be used but 
            // for just one lookup, linear search over ~7000 prefixes is instant enough.
            
            
            var match = _prefixes.FirstOrDefault(p => IsIpInCidr(ip, p.IpPrefix));
            return match?.Region; // Returns e.g., "us-east-1"
        }

        private bool IsIpInCidr(IPAddress ip, string cidr)
        {
            if (string.IsNullOrEmpty(cidr)) return false;

            var parts = cidr.Split('/');
            if (parts.Length != 2) return false;

            if (!IPAddress.TryParse(parts[0], out var cidrIp)) return false;
            if (!int.TryParse(parts[1], out var prefixLength)) return false;

            var ipBytes = ip.GetAddressBytes();
            var cidrBytes = cidrIp.GetAddressBytes();

            if (ipBytes.Length != cidrBytes.Length) return false;
            
            // For IPv4
            if (ipBytes.Length == 4) 
            {
                uint ipVal = (uint)((ipBytes[0] << 24) | (ipBytes[1] << 16) | (ipBytes[2] << 8) | ipBytes[3]);
                uint cidrVal = (uint)((cidrBytes[0] << 24) | (cidrBytes[1] << 16) | (cidrBytes[2] << 8) | cidrBytes[3]);
                
                uint mask = prefixLength == 0 ? 0 : 0xFFFFFFFF << (32 - prefixLength);
                
                return (ipVal & mask) == (cidrVal & mask);
            }
            
            return false;
        }

        public static string GetPrettyRegionName(string regionCode)
        {
            // Map aws region codes to readable names matching the app's style if possible
            return regionCode switch
            {
                "us-east-1" => "US East (N. Virginia)",
                "us-east-2" => "US East (Ohio)",
                "us-west-1" => "US West (N. California)",
                "us-west-2" => "US West (Oregon)",
                "ca-central-1" => "Canada (Central)",
                "sa-east-1" => "South America (São Paulo)",
                "eu-west-1" => "Europe (Ireland)",
                "eu-west-2" => "Europe (London)",
                "eu-central-1" => "Europe (Frankfurt am Main)",
                "eu-north-1" => "Europe (Stockholm)",
                "eu-west-3" => "Europe (Paris)",
                "eu-south-1" => "Europe (Milan)",
                "ap-northeast-1" => "Asia Pacific (Tokyo)",
                "ap-northeast-2" => "Asia Pacific (Seoul)",
                "ap-south-1" => "Asia Pacific (Mumbai)",
                "ap-southeast-1" => "Asia Pacific (Singapore)",
                "ap-southeast-2" => "Asia Pacific (Sydney)",
                "ap-east-1" => "Asia Pacific (Hong Kong)",
                "af-south-1" => "Africa (Cape Town)",
                "me-south-1" => "Middle East (Bahrain)",
                "ap-northeast-3" => "Asia Pacific (Osaka)",
                _ => regionCode // Fallback
            };
        }
    }

    public class AwsIpRangesRoot
    {
        [JsonPropertyName("prefixes")]
        public List<AwsPrefix> Prefixes { get; set; }
    }

    public class AwsPrefix
    {
        [JsonPropertyName("ip_prefix")]
        public string IpPrefix { get; set; }

        [JsonPropertyName("region")]
        public string Region { get; set; }

        [JsonPropertyName("service")]
        public string Service { get; set; }

        [JsonPropertyName("network_border_group")]
        public string NetworkBorderGroup { get; set; }
    }
}
