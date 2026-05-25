using System.Threading.Channels;
using BankServer.Domain.Models;

namespace BankServer.Business;

/// <summary>
/// Тасалбарын дарааллыг хянана. Гурван асуудлыг шийдсэн:
/// давхар дугаар олгохгүй, давхар дуудахгүй, FIFO дараалал алдагдахгүй.
/// </summary>
public class TicketQueueService
{
    /// <summary>Хамгийн сүүлд олгосон дугаар. 0-ээс эхэлнэ.</summary>
    private volatile int _lastNumber = 0;

    /// <summary>Теллер хамгийн сүүлд дуудсан дугаар.</summary>
    private volatile int _currentCalledNumber = 0;

    /// <summary>
    /// Thread-safe дараалал. WriteAsync → эцэст нэмнэ,
    /// TryRead → эхнээс авна (FIFO).
    /// Capacity=100: дараалал дүүрвэл хүлээнэ, хаяхгүй.
    /// </summary>
    private readonly Channel<QueueTicket> _ticketChannel =
        Channel.CreateBounded<QueueTicket>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    /// <summary>
    /// Дугаар олгох үед нэгэн зэрэг хоёр хүсэлт ирвэл нэг нь хүлээдэг lock.
    /// async/await дотор lock{} ажиллахгүй тул SemaphoreSlim ашиглана.
    /// </summary>
    private readonly SemaphoreSlim _issueLock = new(1, 1);

    /// <summary>
    /// Шинэ тасалбар олгож дарааллд нэмнэ.
    /// Interlocked.Increment ашигласан учир дугаар хэзээ ч давтагдахгүй.
    /// </summary>
    public async Task<QueueTicket> IssueTicketAsync(string serviceType)
    {
        await _issueLock.WaitAsync();
        try
        {
            // Interlocked.Increment: i++ биш, CPU түвшинд нэг алхам — thread-safe
            int number = Interlocked.Increment(ref _lastNumber);
            var ticket = new QueueTicket(number, DateTime.Now, serviceType);
            await _ticketChannel.Writer.WriteAsync(ticket);
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
    /// TryRead atomic тул хоёр теллер нэгэн зэрэг дарсан ч
    /// нэг дугаар хоёрт очихгүй.
    /// WaitToReadAsync биш — дараалал хоосон үед хүлээхгүй,
    /// тэр даруй сүүлийн дугаарыг буцаана.
    /// </summary>
    public Task<int> CallNextAsync()
    {
        if (_ticketChannel.Reader.TryRead(out var ticket))
        {
            _currentCalledNumber = ticket.Number;
            return Task.FromResult(ticket.Number);
        }

        return Task.FromResult(_currentCalledNumber);
    }

    /// <summary>Одоо хүлээж байгаа хүний тоо.</summary>
    public int GetQueueCount() => _ticketChannel.Reader.Count;

    /// <summary>Теллер хамгийн сүүлд дуудсан дугаар.</summary>
    public int GetCurrentNumber() => _currentCalledNumber;
}