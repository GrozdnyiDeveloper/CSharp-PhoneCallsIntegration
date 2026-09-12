using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace crmPark.Integration.SpeechAnalysis.Models
{
    class Request
    {
        [JsonPropertyName("name")]
        public string name { get; set; }
        [JsonPropertyName("destination")]
        public string destination { get; set; }
        [JsonPropertyName("isSync")]
        public bool isSync { get; set; }
        [JsonPropertyName("Data")]
        public  Data Data { get; set; }
    }
}
