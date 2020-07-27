using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace GmailAPI
{
    [DataContract]
    public class TokenResponse
    {
        [DataMember]
        [JsonProperty("access_token")]
        public string Token {get; set;}

        [DataMember]
        [JsonProperty("scope")]
        public string Scope { get; set; }

        [DataMember]
        [JsonProperty("token_type")]
       public  string TokenType { get; set; }

        [DataMember] 
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

    }
}
