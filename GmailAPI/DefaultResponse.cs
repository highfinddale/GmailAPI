using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GmailAPI
{
    public class DefaultResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("html_attributions")]
        public IEnumerable<dynamic> HtmlAttribute { get; set; }

        [JsonProperty("error_message")]
        public String ErroMessage { get; set; }

    }
}
