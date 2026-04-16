using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieCatalog.Models
{
    public class ApiResponseDTO
    {

        [JsonPropertyName("msg")]
        public string Msg { get; set; }

        [JsonPropertyName("movie")]
        public MovieDTO Movie { get; set; } = new MovieDTO();
    }
}
