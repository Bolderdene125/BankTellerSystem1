using BankServer.Business;
using BankServer.Domain.DTOs;
using BankSystem.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BankServer.Controllers;

/// <summary>
/// Дансны мэдээлэл харах, мөнгө шилжүүлэх endpoint-ууд.
/// GET  /api/account          — бүх дансны жагсаалт
/// GET  /api/account/{id}     — нэг дансны мэдээлэл
/// POST /api/account/transfer — мөнгө шилжүүлэх
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly AccountService _svc;
    public AccountController(AccountService svc) => _svc = svc;

    /// <summary>Бүх идэвхтэй дансны жагсаалт буцаана.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<AccountResponseDto>> GetAll() =>
        Ok(_svc.GetAllAccounts()
            .Select(a => new AccountResponseDto(
                a.AccountNumber, a.OwnerName, "MNT", a.MNT, true)));

    /// <summary>Дансны дугаараар нэг данс хайна.</summary>
    [HttpGet("{accountNumber}")]
    public ActionResult<AccountResponseDto> Get(string accountNumber)
    {
        var a = _svc.GetAccount(accountNumber);
        if (a is null) return NotFound("Данс олдсонгүй");
        return Ok(new AccountResponseDto(
            a.AccountNumber, a.OwnerName, "MNT", a.MNT, true));
    }

    /// <summary>
    /// А данснаас Б данс руу мөнгө шилжүүлнэ.
    /// Shared.DTOs.TransferRequest ашиглана.
    /// Алдааны мессежүүд: ижил данс, үлдэгдэл хүрэлцэхгүй, данс олдсонгүй гэх мэт.
    /// </summary>
    [HttpPost("transfer")]
    public async Task<ActionResult<TransferResponse>> Transfer(
        [FromBody] TransferRequest req)
    {
        if (string.IsNullOrEmpty(req.FromAccountNumber) ||
            string.IsNullOrEmpty(req.ToAccountNumber) ||
            req.Amount <= 0)
            return BadRequest(new TransferResponse
            { Success = false, Message = "Дутуу мэдээлэл" });

        var (ok, msg) = await _svc.TransferAsync(
            req.FromAccountNumber, req.ToAccountNumber, req.Amount);

        return ok
            ? Ok(new TransferResponse { Success = true, Message = msg })
            : BadRequest(new TransferResponse { Success = false, Message = msg });
    }
}