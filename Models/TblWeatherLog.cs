using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_Krushi_Officer.Models
{
    [Table("TblWeatherLog")]
    public class TblWeatherLog
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string? District { get; set; }

        [MaxLength(100)]
        public string? Village { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Temperature { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Humidity { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? RainChance { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? WindSpeed { get; set; }

        [MaxLength(200)]
        public string? WeatherDescription { get; set; }

        [MaxLength(500)]
        public string? FarmingAlert { get; set; }

        [MaxLength(50)]
        public string? AlertType { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
