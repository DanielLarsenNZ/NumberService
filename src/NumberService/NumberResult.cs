using Newtonsoft.Json;

namespace NumberService
{
    public class NumberResult
    {
        [JsonProperty("id")]
        public string Key { get; set; }

        public long Number { get; set; }

        public string ClientId { get; set; }
        
        [JsonProperty("_etag")]
        public string ETag { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double? RequestCharge { get; set; }
    }
}
