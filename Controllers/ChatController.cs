using Microsoft.AspNetCore.Mvc;
using ApiHelpFast.Data;
using ApiHelpFast.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiHelpFast.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public ChatController(ApplicationDbContext db) => _db = db;

	[HttpGet]
	public async Task<IActionResult> GetAll([FromQuery] int? chamadoId, [FromQuery] int? userId, [FromQuery] int? otherUserId)
	{
		var query = _db.Chats
			.Include(c => c.Remetente)
			.Include(c => c.Destinatario)
			.AsQueryable();

		if (chamadoId.HasValue)
			query = query.Where(c => c.ChamadoId == chamadoId.Value);

		if (userId.HasValue && otherUserId.HasValue)
		{
			var a = userId.Value;
			var b = otherUserId.Value;
			query = query.Where(c => (c.RemetenteId == a && c.DestinatarioId == b) || (c.RemetenteId == b && c.DestinatarioId == a));
		}
		else if (userId.HasValue)
		{
			var u = userId.Value;
			query = query.Where(c => c.RemetenteId == u || c.DestinatarioId == u);
		}

		var messages = await query
			.OrderBy(c => c.DataEnvio)
			.Select(c => new ChatResponseDbo
			{
				Id = c.Id,
				Mensagem = c.Mensagem,
				DataEnvio = c.DataEnvio,
				EnviadoPorCliente = c.EnviadoPorCliente,
				Tipo = c.Tipo,
				ChamadoId = c.ChamadoId,
				Remetente = c.Remetente == null ? null : new UserLiteDbo { Id = c.Remetente.Id, Nome = c.Remetente.Nome, Telefone = c.Remetente.Telefone },
				Destinatario = c.Destinatario == null ? null : new UserLiteDbo { Id = c.Destinatario.Id, Nome = c.Destinatario.Nome, Telefone = c.Destinatario.Telefone }
			})
			.ToListAsync();

		return Ok(messages);
	}

	// POST /api/chat
	[HttpPost]
	public async Task<IActionResult> Create([FromBody] ChatDbo dbo)
	{
		if (dbo == null) return BadRequest(new { error = "Dados inválidos" });

		if (string.IsNullOrWhiteSpace(dbo.Mensagem)) return BadRequest(new { error = "Mensagem obrigatória" });
		if (!dbo.RemetenteId.HasValue) return BadRequest(new { error = "RemetenteId obrigatório" });
		if (!dbo.DestinatarioId.HasValue) return BadRequest(new { error = "DestinatarioId obrigatório" });

		var remetenteExists = await _db.Usuarios.AnyAsync(u => u.Id == dbo.RemetenteId.Value);
		var destinatarioExists = await _db.Usuarios.AnyAsync(u => u.Id == dbo.DestinatarioId.Value);
		if (!remetenteExists || !destinatarioExists) return BadRequest(new { error = "Remetente ou destinatário inválido" });

		var entity = new Chat
		{
			ChamadoId = dbo.ChamadoId,
			RemetenteId = dbo.RemetenteId.Value,
			DestinatarioId = dbo.DestinatarioId.Value,
			Mensagem = dbo.Mensagem.Trim(),
			DataEnvio = DateTime.UtcNow,
			EnviadoPorCliente = dbo.EnviadoPorCliente,
			Tipo = string.IsNullOrWhiteSpace(dbo.Tipo) ? "Usuario" : dbo.Tipo.Trim()
		};

		_db.Chats.Add(entity);
		await _db.SaveChangesAsync();

		var response = new ChatResponseDbo
		{
			Id = entity.Id,
			Mensagem = entity.Mensagem,
			DataEnvio = entity.DataEnvio,
			EnviadoPorCliente = entity.EnviadoPorCliente,
			Tipo = entity.Tipo,
			ChamadoId = entity.ChamadoId,
			Remetente = await _db.Usuarios
				.Where(u => u.Id == entity.RemetenteId)
				.Select(u => new UserLiteDbo { Id = u.Id, Nome = u.Nome, Telefone = u.Telefone })
				.FirstOrDefaultAsync(),
			Destinatario = await _db.Usuarios
				.Where(u => u.Id == entity.DestinatarioId)
				.Select(u => new UserLiteDbo { Id = u.Id, Nome = u.Nome, Telefone = u.Telefone })
				.FirstOrDefaultAsync()
		};

		return CreatedAtAction(nameof(GetAll), new { id = response.Id }, response);
	}
}

public class ChatDbo
{
	public int? ChamadoId { get; set; }
	public int? RemetenteId { get; set; }
	public int? DestinatarioId { get; set; }
	public string Mensagem { get; set; } = string.Empty;
	public bool EnviadoPorCliente { get; set; } = true;
	public string? Tipo { get; set; }
}

public class ChatResponseDbo
{
	public int Id { get; set; }
	public int? ChamadoId { get; set; }
	public string Mensagem { get; set; } = string.Empty;
	public DateTime DataEnvio { get; set; }
	public bool EnviadoPorCliente { get; set; }
	public string? Tipo { get; set; }
	public UserLiteDbo? Remetente { get; set; }
	public UserLiteDbo? Destinatario { get; set; }
}

public class UserLiteDbo
{
	public int Id { get; set; }
	public string? Nome { get; set; }
	public string? Telefone { get; set; }
}
