using Microsoft.AspNetCore.Mvc;
using ApiHelpFast.Data;
using ApiHelpFast.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ApiHelpFast.Controllers;
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    // assegure que PasswordHasher<Usuario> é usado para hash/verificação
    private readonly PasswordHasher<Usuario> _pwdHasher = new();

    public AuthController(ApplicationDbContext db) => _db = db;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await _db.Usuarios.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { error = "Email já cadastrado" });

        // Registrar somente como Cliente (técnicos devem ser criados pelo Admin)
        var cargoCliente = await _db.Cargos.FirstOrDefaultAsync(c => c.Nome == "Cliente");
        if (cargoCliente == null)
            return StatusCode(500, new { error = "Cargo 'Cliente' não encontrado (configuração do sistema)" });

        var usuario = new Usuario
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone,
            CargoId = cargoCliente.Id,
            UltimoLogin = null
        };

        usuario.Senha = _pwdHasher.HashPassword(usuario, dto.Senha);

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = usuario.Id }, new { usuario.Id, usuario.Nome, usuario.Email, usuario.CargoId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _db.Usuarios.Include(u => u.Cargo).FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return Unauthorized(new { error = "Credenciais inválidas" });

        var verify = _pwdHasher.VerifyHashedPassword(user, user.Senha, dto.Senha);
        if (verify == PasswordVerificationResult.Failed) return Unauthorized(new { error = "Credenciais inválidas" });

        user.UltimoLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Nome, Cargo = user.Cargo?.Nome, user.CargoId });
    }

    // endpoint auxiliar para CreatedAtAction (mantenha ou ajuste conforme existente)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var u = await _db.Usuarios.Include(x => x.Cargo).Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Nome, x.Email, Cargo = x.Cargo!.Nome, x.CargoId }).FirstOrDefaultAsync();
        if (u == null) return NotFound();
        return Ok(u);
    }
}

public class RegisterDto
{
    public string Nome { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Senha { get; set; } = null!;
    public string? Telefone { get; set; }
}

public class LoginDto
{
    public string Email { get; set; } = null!;
    public string Senha { get; set; } = null!;
}
