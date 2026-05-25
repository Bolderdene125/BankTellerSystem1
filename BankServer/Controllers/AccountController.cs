using BankServer.Business;
using BankServer.Domain.DTOs;
using BankSystem.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BankServer.Controllers;

/// <summary>
/// Дансны мэдээлэл харах болон мөнгө шилжүүлэх endpoint-ууд.
/// GET  /api/account         — бүх дансны жагсаалт
/// GET  /api/account/{id}    — нэг дансны мэдээлэл
/// POST /api/account/transfer — мөнгө шилжүүлэх
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>Идэвхтэй бүх дансны жагсаалт буцаана.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponseDto>>> GetAll()
    {
        var accounts = await _accountService.GetAllAccountsAsync();
        return Ok(accounts.Select(a => new AccountResponseDto(
            a.AccountNumber, a.OwnerName, a.Currency, a.Balance, a.IsActive)));
    }

    /// <summary>Дансны дугаараар нэг данс хайна.</summary>
    [HttpGet("{accountNumber}")]
    public async Task<ActionResult<AccountResponseDto>> Get(string accountNumber)
    {
        var account = await _accountService.GetByAccountNumberAsync(accountNumber);
        if (account is null) return NotFound("Данс олдсонгүй");

        return Ok(new AccountResponseDto(
            account.AccountNumber, account.OwnerName,
            account.Currency, account.Balance, account.IsActive));
    }

    /// <summary>
    /// А данснаас Б данс руу мөнгө шилжүүлнэ.
    /// TransferRequest — Shared.DTOs-аас авна:
    ///   FromAccountNumber, ToAccountNumber, Amount, TellerWindowId
    /// TransferResponse — Shared.DTOs-аас авна:
    ///   Success, Message, TransactionId
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

        var (success, message) = await _accountService.TransferAsync(
            req.FromAccountNumber, req.ToAccountNumber, req.Amount);

        return success
            ? Ok(new TransferResponse { Success = true, Message = message })
            : BadRequest(new TransferResponse { Success = false, Message = message });
    }
}