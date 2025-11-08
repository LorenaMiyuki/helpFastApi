using Microsoft.AspNetCore.Mvc;
using ApiHelpFast.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiHelpFast.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CargosController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	public CargosController(ApplicationDbContext db) => _db = db;

	// GET /api/cargos/usuarios
	[HttpGet("usuarios")]
	public async Task<IActionResult> GetUsuariosComCargo()
	{
		var list = await _db.Usuarios
			.Include(u => u.Cargo)
			.Select(u => new
			{
				UsuarioId = u.Id,
				Nome = u.Nome,
				CargoId = u.CargoId,
				CargoNome = u.Cargo != null ? u.Cargo.Nome : null
			})
			.ToListAsync();

		return Ok(list);
	}

	// GET /api/cargos/usuarios/{usuarioId}
	[HttpGet("usuarios/{usuarioId}")]
	public async Task<IActionResult> GetUsuarioCargo(int usuarioId)
	{
		var item = await _db.Usuarios
			.Where(u => u.Id == usuarioId)
			.Include(u => u.Cargo)
			.Select(u => new
			{
				UsuarioId = u.Id,
				Nome = u.Nome,
				CargoId = u.CargoId,
				CargoNome = u.Cargo != null ? u.Cargo.Nome : null
			})
			.FirstOrDefaultAsync();

		if (item == null) return NotFound();
		return Ok(item);
	}
}
