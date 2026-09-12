using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace crmPark.Integration.SpeechAnalysis.DBDataClasses
{
    public partial class Transcription
    {
        [JsonIgnore]
        public int task_id { get; set; }
        [JsonPropertyName("call_id")]
        public long? call_id { get; set; }
        [JsonIgnore]
        public string transcription_raw { get; set; }
        [JsonPropertyName("transcription")]
        public string transcription_enhanced { get; set; }
        [JsonIgnore]
        public decimal? duration { get; set; }
        [JsonIgnore]
        public string language { get; set; }
        [JsonIgnore]
        public string config { get; set; }
        [JsonIgnore]
        public DateTime? created_at { get; set; }
        [JsonPropertyName("summary")]
        public string summary { get; set; }
        [JsonIgnore]
        public bool? is_processed { get; set; }
    }
}
