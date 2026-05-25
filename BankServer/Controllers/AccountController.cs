using BankServer.Business;
using BankServer.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BankServer.Controllers;

/// <summary>Дансны мэдээлэл харах, мөнгө шилжүүлэх endpoint-ууд.</summary>
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>Бүх дансны жагсаалт. GET /api/account</summary>
    [HttpGet]
    public ActionResult<IEnumerable<AccountResponseDto>> GetAll() =>
        Ok(_accountService.GetAllAccounts()
            .Select(a => new AccountResponseDto(
                a.AccountNumber, a.OwnerName, a.MNT, a.USD)));

    /// <summary>Нэг дансны мэдээлэл. GET /api/account/ACC001</summary>
    [HttpGet("{accountNumber}")]
    public ActionResult<AccountResponseDto> Get(string accountNumber)
    {
        var account = _accountService.GetAccount(accountNumber);
        if (account is null) return NotFound("Данс олдсонгүй");

        return Ok(new AccountResponseDto(
            account.AccountNumber, account.OwnerName, account.MNT, account.USD));
    }

    /// <summary>
    /// Мөнгө шилжүүлнэ. POST /api/account/transfer
    /// Body: { "fromAccount": "ACC001", "toAccount": "ACC002", "amount": 100000 }
    /// </summary>
    [HttpPost("transfer")]
    public async Task<ActionResult<TransferResponseDto>> Transfer(
        [FromBody] TransferRequestDto req)
    {
        if (string.IsNullOrEmpty(req.FromAccount) ||
            string.IsNullOrEmpty(req.ToAccount) ||
            req.Amount <= 0)
            return BadRequest(new TransferResponseDto(false, "Дутуу мэдээлэл"));

        var (success, message) = await _accountService.TransferAsync(
            req.FromAccount, req.ToAccount, req.Amount);

        return success
            ? Ok(new TransferResponseDto(true, message))
            : BadRequest(new TransferResponseDto(false, message));
    }
}