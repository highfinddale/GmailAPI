using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace GmailAPI
{
    public class AutoCompleteResponse:DefaultResponse
    {
        [JsonProperty("predictions")]
        public virtual  IEnumerable<Prediction> Predictions { get; set; }
    }
}
