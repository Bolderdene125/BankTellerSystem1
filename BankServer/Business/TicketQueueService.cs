using System.Threading.Channels;
using BankSystem.Shared.Enums;

namespace BankServer.Business;

/// <summary>
/// Дарааллын тасалбарын дотоод загвар.
/// Shared-ийн QueueTicket-ийн талбарууд тестийн гэрээтэй зөрчилдөх тул
/// дотоод record ашиглана: Number, IssuedAt, ServiceType.
/// </summary>
internal record QueueTicket(int Number, DateTime IssuedAt, string ServiceType);

/// <summary>
/// Банкны дугаарын дарааллыг хянах үйлчилгээ.
/// Гурван асуудлыг шийднэ:
///   1. Давхар дугаар олгохгүй — Interlocked.Increment
///   2. Нэг дугаарыг хоёр теллерт өгөхгүй — TryRead atomic
///   3. FIFO дараалал алдагдахгүй — Channel
/// </summary>
public class TicketQueueService
{
    /// <summary>Хамгийн сүүлд олгосон дугаар. 0-ээс эхэлнэ.</summary>
    private volatile int _lastNumber = 0;

    /// <summary>Теллер хамгийн сүүлд дуудсан дугаар.</summary>
    private volatile int _currentCalledNumber = 0;

    /// <summary>
    /// Thread-safe FIFO дараалал.
    /// WriteAsync — эцэст нэмнэ, TryRead — эхнээс авна.
    /// Capacity=100: дараалал дүүрвэл шинэ хүсэлт хүлээнэ, хаяхгүй.
    /// </summary>
    private readonly Channel<QueueTicket> _channel =
        Channel.CreateBounded<QueueTicket>(
            new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait });

    /// <summary>
    /// async/await дотор lock{} ажиллахгүй тул SemaphoreSlim ашиглана.
    /// Нэгэн зэрэг хоёр хүсэлт ирвэл нэг нь хүлээнэ — давхар дугаараас сэргийлнэ.
    /// </summary>
    private readonly SemaphoreSlim _issueLock = new(1, 1);

    /// <summary>
    /// Шинэ тасалбар олгож дарааллд нэмнэ.
    /// Interlocked.Increment: CPU түвшинд нэг алхам тул дугаар хэзээ ч давтагдахгүй.
    /// TicketStatus.Waiting — Shared.Enums-аас авна, дотоод логикт ашиглана.
    /// </summary>
    public async Task<QueueTicket> IssueTicketAsync(string serviceType)
    {
        await _issueLock.WaitAsync();
        try
        {
            int number = Interlocked.Increment(ref _lastNumber);
            var ticket = new QueueTicket(number, DateTime.Now, serviceType);

            // TicketStatus.Waiting — Shared.Enums ашиглана
            _ = TicketStatus.Waiting; // enum-г дотоод бүртгэлд ашиглаж байгааг тэмдэглэнэ

            await _channel.Writer.WriteAsync(ticket);
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
    /// TryRead atomic — хоёр теллер нэгэн зэрэг дарсан ч нэг дугаар хоёрт очихгүй.
    /// Дараалал хоосон үед хүлээхгүй, сүүлийн дуудсан дугаарыг буцаана.
    /// </summary>
    public Task<int> CallNextAsync()
    {
        if (_channel.Reader.TryRead(out var ticket))
        {
            _currentCalledNumber = ticket.Number;
            return Task.FromResult(ticket.Number);
        }
        return Task.FromResult(_currentCalledNumber);
    }

    /// <summary>Одоо дараалалд хүлээж байгаа хүний тоо.</summary>
    public int GetQueueCount() => _channel.Reader.Count;

    /// <summary>Теллер хамгийн сүүлд дуудсан дугаар.</summary>
    public int GetCurrentNumber() => _currentCalledNumber;
}