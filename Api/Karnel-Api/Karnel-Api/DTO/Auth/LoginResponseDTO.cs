namespace Karnel_Api.DTO;

public class LoginResponseDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email {get; set;}
    public string Avatar {get; set;}
    public string Role {get; set;}
    public string Token { get; set; } 
}