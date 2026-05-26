using BankServer.Business;
using BankServer.Domain.DTOs;
using BankSystem.Shared.DTOs.Requests;
using BankSystem.Shared.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BankServer.Controllers;

/// <summary>
/// Дансны мэдээлэл харах, мөнгө шилжүүлэх endpoint-ууд.
///
/// GET  /api/account              — бүх дансны жагсаалт
/// GET  /api/account/{id}         — нэг дансны мэдээлэл
/// POST /api/account/transfer     — мөнгө шилжүүлэх
/// GET  /api/account/history      — гүйлгээний түүх (ШИНЭ)
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
    /// ЗАСВАР: TransferId хариуд нэмэгдсэн, TellerWindowId дамжуулагдана.
    /// </summary>
    [HttpPost("transfer")]
    public async Task<ActionResult<TransferResponse>> Transfer(
        [FromBody] TransferRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(new TransferResponse
            {
                Success = false,
                Message = string.Join(", ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
            });

        var (ok, msg, id) = await _svc.TransferAsync(
            req.FromAccountNumber,
            req.ToAccountNumber,
            req.Amount,
            req.TellerWindowId);

        return ok
            ? Ok(new TransferResponse { Success = true, Message = msg, TransferId = id })
            : BadRequest(new TransferResponse { Success = false, Message = msg });
    }

    /// <summary>
    /// Гүйлгээний бүртгэлийн түүх буцаана.
    /// ШИНЭ: аудитын шаардлагаар нэмэгдсэн.
    /// </summary>
    [HttpGet("history")]
    public ActionResult GetHistory() =>
        Ok(_svc.GetTransferHistory().Select(r => new
        {
            r.Id,
            r.FromAccount,
            r.ToAccount,
            Amount    = r.Amount.ToString("N0"),
            r.TellerId,
            r.Status,
            CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            r.ErrorMessage
        }));
}
