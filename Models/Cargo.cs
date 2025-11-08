using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiHelpFast.Models
{
    [Table("Cargos", Schema = "dbo")]
    public class Cargo
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        public string? Nome { get; set; }

        public ICollection<Usuario>? Usuarios { get; set; } = new List<Usuario>();
    }
}