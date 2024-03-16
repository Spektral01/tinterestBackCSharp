using System.ComponentModel.DataAnnotations;

namespace Tinterest.Data
{
    public class Users
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? LastName { get; set; }

        public string? Gender { get; set; }

        public string? Image { get; set; }

        public string? City { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }
    }


}
