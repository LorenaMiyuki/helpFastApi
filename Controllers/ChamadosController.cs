using Microsoft.AspNetCore.Mvc;
using ApiHelpFast.Data;
using ApiHelpFast.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiHelpFast.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ChamadosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ChamadosController(ApplicationDbContext db) => _db = db;

    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir([FromBody] AbrirChamadoDto dto)
    {
        var cliente = await _db.Usuarios.FindAsync(dto.ClienteId);
        if (cliente == null) return BadRequest("Cliente inválido");

        var chamado = new Chamado
        {
            ClienteId = dto.ClienteId,
            Motivo = dto.Motivo,
            Status = "Em Atendimento",
            DataAbertura = DateTime.UtcNow
        };
        _db.Chamados.Add(chamado);
        await _db.SaveChangesAsync();

        // inserir FAQ / IA inicial como mensagens no chat se desejar
        _db.Chats.Add(new Chat { ChamadoId = chamado.Id, Mensagem = "IA iniciada: perguntas iniciais", EnviadoPorCliente = false });
        await _db.SaveChangesAsync();

        // selecionar técnico aleatório online (simulação: técnicos com Cargo.Nome == "Tecnico")
        var tecnicos = await _db.Usuarios.Include(u => u.Cargo).Where(u => u.Cargo!.Nome == "Tecnico").ToListAsync();
        if (tecnicos.Any())
        {
            var rnd = new Random();
            var escolhido = tecnicos[rnd.Next(tecnicos.Count)];
            chamado.TecnicoId = escolhido.Id;
            _db.Historicos.Add(new HistoricoChamado { ChamadoId = chamado.Id, Acao = $"Atribuído a técnico {escolhido.Nome}" });
            await _db.SaveChangesAsync();
        }

        return Ok(new { chamado.Id });
    }

    [HttpGet("meus/{clienteId}")]
    public async Task<IActionResult> MeusChamados(int clienteId)
    {
        var list = await _db.Chamados.Where(c => c.ClienteId == clienteId)
            .Select(c => new { c.Id, c.Motivo, c.Status }).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}/chat")]
    public async Task<IActionResult> GetChat(int id)
    {
        var msgs = await _db.Chats.Where(c => c.ChamadoId == id).OrderBy(c => c.DataEnvio)
            .Select(m => new { m.Id, m.Mensagem, m.EnviadoPorCliente, m.DataEnvio }).ToListAsync();
        if (!msgs.Any()) return NotFound();
        return Ok(msgs);
    }

    [HttpPost("{id}/mensagem")]
    public async Task<IActionResult> EnviarMensagem(int id, [FromBody] MensagemDto dto)
    {
        var chamado = await _db.Chamados.FindAsync(id);
        if (chamado == null) return NotFound();

        _db.Chats.Add(new Chat { ChamadoId = id, Mensagem = dto.Mensagem, EnviadoPorCliente = dto.EnviadoPorCliente });
        await _db.SaveChangesAsync();

        // Lógica simplificada de IA: contar mensagens do cliente; se > 3, transferir para técnico (já atribuído)
        if (!dto.EnviadoPorCliente) return Ok();
        var clienteMsgCount = await _db.Chats.CountAsync(c => c.ChamadoId == id && c.EnviadoPorCliente);
        if (clienteMsgCount >= 3)
        {
            // enviar notificação via histórico
            _db.Historicos.Add(new HistoricoChamado { ChamadoId = id, Acao = "IA transferiu para técnico após 3 perguntas" });
            await _db.SaveChangesAsync();
        }

        return Ok();
    }
}

public class AbrirChamadoDto { public int ClienteId { get; set; } public string Motivo { get; set; } = null!; }
public class MensagemDto { public string Mensagem { get; set; } = null!; public bool EnviadoPorCliente { get; set; } }
