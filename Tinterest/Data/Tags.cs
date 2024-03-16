using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace Tinterest.Data
{
    public class UserTags
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Users")]
        public int UserId { get; set; }

        [Column(TypeName = "jsonb")]
        public string Tags { get; set; }
    }

}
