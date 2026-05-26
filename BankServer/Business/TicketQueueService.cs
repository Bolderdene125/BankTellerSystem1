using System.Threading.Channels;
using BankSystem.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace BankServer.Business;

/// <summary>
/// Банкны дугаарын дарааллыг хянах үйлчилгээ.
///
/// Гурван асуудлыг шийднэ:
///   1. Давхар дугаар олгохгүй  — Interlocked.Increment
///   2. Нэг дугаарыг хоёр теллерт өгөхгүй — TryRead atomic
///   3. FIFO дараалал алдагдахгүй — Channel&lt;T&gt;
///
/// ЗАСВАР: ILogger нэмэгдсэн.
/// </summary>
public class TicketQueueService
{
    private readonly ILogger<TicketQueueService> _logger;

    /// <summary>
    /// Хамгийн сүүлд олгосон дугаар.
    /// volatile: кэшлэгдэхгүй, үргэлж сүүлийн утгыг уншина.
    /// </summary>
    private volatile int _lastNumber = 0;

    /// <summary>Теллер хамгийн сүүлд дуудсан дугаар.</summary>
    private volatile int _currentCalledNumber = 0;

    /// <summary>
    /// Thread-safe FIFO дараалал.
    /// Channel&lt;T&gt;-г сонгосон шалтгаан:
    ///   - WriteAsync: эцэст нэмнэ, хүлээнэ (хаяхгүй)
    ///   - TryRead: эхнээс atomic-аар авна
    ///   - Capacity=100: дараалал дүүрвэл шинэ хүсэлт хүлээнэ
    /// Queue&lt;T&gt;, ConcurrentQueue&lt;T&gt;-аас ялгаатай нь
    /// async/await-д нативаар дэмжигддэг.
    /// </summary>
    private readonly Channel<QueueTicketInternal> _channel =
        Channel.CreateBounded<QueueTicketInternal>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

    /// <summary>
    /// async/await дотор lock{} ажиллахгүй тул SemaphoreSlim ашиглана.
    /// Нэгэн зэрэг хоёр хүсэлт ирвэл нэг нь хүлээнэ — давхар дугаараас сэргийлнэ.
    /// SemaphoreSlim(1,1): нэгэн зэрэг зөвхөн нэг thread орно.
    /// </summary>
    private readonly SemaphoreSlim _issueLock = new(1, 1);

    public TicketQueueService(ILogger<TicketQueueService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Шинэ тасалбар олгож дарааллд нэмнэ.
    ///
    /// Interlocked.Increment:
    ///   CPU түвшний атомик үйлдэл — дугаар хэзээ ч давтагдахгүй.
    ///   lock{} шаардахгүй, thread-safe.
    /// </summary>
    public async Task<QueueTicketInternal> IssueTicketAsync(string serviceType)
    {
        await _issueLock.WaitAsync();
        try
        {
            // Interlocked.Increment: race condition байхгүй, дугаар дахин давтагдахгүй
            int number = Interlocked.Increment(ref _lastNumber);
            var ticket = new QueueTicketInternal(number, DateTime.Now, serviceType);

            // Channel-д нэмнэ — FIFO дараалал хадгалагдана
            await _channel.Writer.WriteAsync(ticket);

            _logger.LogInformation(
                "Тасалбар олгогдлоо: #{Number}, сервис: {ServiceType}, дараалал: {Count}",
                number, serviceType, _channel.Reader.Count);

            return ticket;
        }
        finally
        {
            // finally: алдаа гарсан ч lock заавал суллагдана
            _issueLock.Release();
        }
    }

    /// <summary>
    /// Дарааллаас дараагийн тасалбарыг авна.
    ///
    /// TryRead atomic:
    ///   Хоёр теллер нэгэн зэрэг дарсан ч нэг дугаар хоёрт очихгүй.
    ///   Channel-ийн TryRead нь OS-түвшний lock ашиглана.
    /// </summary>
    public Task<int> CallNextAsync()
    {
        if (_channel.Reader.TryRead(out var ticket))
        {
            _currentCalledNumber = ticket.Number;
            _logger.LogInformation(
                "Дугаар дуудагдлаа: #{Number}, үлдсэн дараалал: {Count}",
                ticket.Number, _channel.Reader.Count);
            return Task.FromResult(ticket.Number);
        }

        _logger.LogDebug("Дараалал хоосон — одоогийн дугаар буцаана: #{Current}",
            _currentCalledNumber);
        return Task.FromResult(_currentCalledNumber);
    }

    /// <summary>Одоо дарааллд хүлээж байгаа хүний тоо.</summary>
    public int GetQueueCount() => _channel.Reader.Count;

    /// <summary>Теллер хамгийн сүүлд дуудсан дугаар.</summary>
    public int GetCurrentNumber() => _currentCalledNumber;
}

/// <summary>
/// Дарааллын тасалбарын дотоод загвар.
/// Number, IssuedAt, ServiceType — тест гэрээний дагуу.
/// </summary>
public record QueueTicketInternal(
    int      Number,
    DateTime IssuedAt,
    string   ServiceType)
{
    /// <summary>Тасалбарын төлөв — Shared.Enums.TicketStatus ашиглана.</summary>
    public TicketStatus Status { get; init; } = TicketStatus.Waiting;
}
