﻿using DigitalProduction.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DigitalProduction
{
    public class ResponseMessage<T>
    {
        public string Action { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }

        [JsonProperty("devices", NullValueHandling = NullValueHandling.Ignore)]
        public T Devices { get; set; }

        [JsonProperty("users", NullValueHandling = NullValueHandling.Ignore)]
        public T Users { get; set; }

        [JsonProperty("pages", NullValueHandling = NullValueHandling.Ignore)]
        public List<int> Pages { get; set; }

        [JsonProperty("schedule", NullValueHandling = NullValueHandling.Ignore)]
        public List<ProductionSchedule> Schedules { get; set; } 
         
        // Method to serialize the object into JSON string
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        // Method to deserialize JSON string to object
        public static ResponseMessage<T> FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ResponseMessage<T>>(json);
        }
    }
}
