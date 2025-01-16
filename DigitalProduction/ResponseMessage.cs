using Newtonsoft.Json;

namespace DigitalProduction
{
    public class ResponseMessage<T>
    {
        public string Action { get; set; }
        public string Status { get; set; }

        [JsonProperty("devices", NullValueHandling = NullValueHandling.Ignore)]
        public T Devices { get; set; }

        [JsonProperty("users", NullValueHandling = NullValueHandling.Ignore)]
        public T Users { get; set; }

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
