using System;
using Newtonsoft.Json;

namespace Demo.Caliburn.Micro.Models
{
    public class CarDto
    {
        [JsonIgnore]
        public string NumberPlate { get; set; }
        [JsonProperty("registration_number")]
        public string RegistrationNumber { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("status_date")]
        public DateTime StatusDate { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("use")]
        public string Use { get; set; }
        [JsonProperty("first_registration")]
        public string FirstRegistration { get; set; }
        [JsonProperty("vin")]
        public string Vin { get; set; }
        [JsonProperty("own_weight")]
        public string OwnWeight { get; set; }
        [JsonProperty("total_weight")]
        public string TotalWeight { get; set; }
        [JsonProperty("axels")]
        public string Axels { get; set; }
        [JsonProperty("pulling_axels")]
        public string PullingAxels { get; set; }
        [JsonProperty("seats")]
        public string Seats { get; set; }
        [JsonProperty("coupling")]
        public bool Coupling { get; set; }
        [JsonProperty("doors")]
        public string Doors { get; set; }
        [JsonProperty("make")]
        public string Make { get; set; }
        [JsonProperty("model")]
        public string Model { get; set; }
        [JsonProperty("variant")]
        public string Variant { get; set; }
        [JsonProperty("model_type")]
        public string ModelType { get; set; }
        [JsonProperty("model_year")]
        public string ModelYear { get; set; }
        [JsonProperty("color")]
        public string Color { get; set; }
        [JsonProperty("chassis_type")]
        public object ChassisType { get; set; }
        [JsonProperty("engine_cylinders")]
        public string EngineCylinders { get; set; }
        [JsonProperty("engine_volume")]
        public string EngineVolume { get; set; }
        [JsonProperty("engine_power")]
        public string EnginePower { get; set; }
        [JsonProperty("fuel_type")]
        public string FuelType { get; set; }
        [JsonProperty("registration_zipcode")]
        public string RegistrationZipcode { get; set; }
        [JsonProperty("vehicle_id")]
        public string VehicleId { get; set; }
    }
}
