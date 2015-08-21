using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IM.BulkInsert.Tutorial.Entities
{
    [Table("Employees")]
    public partial class Employee
    {
        [Key]
        public int Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MyAddress { get; set; }
        public string City { get; set; }
    }
}
