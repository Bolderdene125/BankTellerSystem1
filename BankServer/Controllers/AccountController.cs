using BankServer.Services;
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
    public IActionResult GetAll() => Ok(_accountService.GetAllAccounts());

    /// <summary>Нэг дансны мэдээлэл. GET /api/account/ACC001</summary>
    [HttpGet("{accountNumber}")]
    public IActionResult Get(string accountNumber)
    {
        var account = _accountService.GetAccount(accountNumber);
        return account is null ? NotFound("Данс олдсонгүй") : Ok(account);
    }

    /// <summary>
    /// Мөнгө шилжүүлнэ. POST /api/account/transfer
    /// Body: { "fromAccount": "ACC001", "toAccount": "ACC002", "amount": 100000 }
    /// </summary>
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest req)
    {
        if (string.IsNullOrEmpty(req.FromAccount) ||
            string.IsNullOrEmpty(req.ToAccount) ||
            req.Amount <= 0)
            return BadRequest("Дансны дугаар болон дүн заавал байна");

        var (success, message) = await _accountService.TransferAsync(
            req.FromAccount, req.ToAccount, req.Amount);

        return success ? Ok(new { message }) : BadRequest(new { message });
    }
}

/// <summary>Гүйлгээний хүсэлтийн body загвар.</summary>
public record TransferRequest(string FromAccount, string ToAccount, decimal Amount);