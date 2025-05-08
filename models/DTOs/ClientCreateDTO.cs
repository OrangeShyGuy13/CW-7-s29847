namespace CW-7-s29847.Models.DTOs;
using System.ComponentModel.DataAnnotations;
public class ClientCreateDTO
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(120, ErrorMessage = "First name cannot exceed 120 characters")]
    public string FirstName { get; set; }
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(120, ErrorMessage = "Last name cannot exceed 120 characters")]
    public string LastName { get; set; }
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }
    public string Telephone { get; set; }
    public string Pesel { get; set; }
    
}