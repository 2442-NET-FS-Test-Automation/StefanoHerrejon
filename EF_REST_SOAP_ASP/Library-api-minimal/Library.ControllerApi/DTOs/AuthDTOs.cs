using System.ComponentModel.DataAnnotations;

namespace Library.ControllerApi.DTOs;

//You probably want different login and register DTOs
//based on what info you have users provide when they register

public record RegisterDto(
    [Required, MaxLength(64)] string Username,
    [Required, MinLength(8)] string Password
    //Could ask for phonoe number, email, etc as well
);

public record LoginDto(
    [Required] string Username,
    [Required] string Password

);