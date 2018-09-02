using System.ComponentModel.DataAnnotations;

namespace ImageHost.Models
{
    public class Setting
    {
        [Key]
        public string Key { get; set; }
        public string Val { get; set; }
    }
}