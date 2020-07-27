using Newtonsoft.Json;
using System.Collections.Generic;

namespace GmailAPI
{
    public class Prediction
    {
        [JsonProperty("place_id")]
        public virtual string PlaceId { get; set; }

        /// <summary>
        /// Description contains the human-readable name for the returned result. 
        /// For establishment results, this is usually the business name.
        /// </summary>
        [JsonProperty("description")]
        public virtual string Description { get; set; }

   }
}