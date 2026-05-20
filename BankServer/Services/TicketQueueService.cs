using System.Threading.Channels;
using BankServer.Models;

namespace BankServer.Services;

/// <summary>
/// Тасалбарын дарааллыг хянана. Гурван асуудлыг шийдсэн:
/// давхар дугаар олгохгүй, давхар дуудахгүй, FIFO дараалал алдагдахгүй.
/// </summary>
public class TicketQueueService
{
    /// <summary>Хамгийн сүүлд олгосон дугаар. 0-ээс эхэлнэ.</summary>
    private volatile int _lastNumber = 0;

    /// <summary>Теллер хамгийн сүүлд дуудсан дугаар. Дэлгэцэнд харуулна.</summary>
    private volatile int _currentCalledNumber = 0;

    /// <summary>
    /// Thread-safe дараалал. WriteAsync → дарааллын эцэст нэмнэ,
    /// TryRead → дарааллын эхнээс авна (FIFO).
    /// Capacity=100: дараалал дүүрвэл шинэ хүсэлт хүлээнэ, хаяхгүй.
    /// </summary>
    private readonly Channel<QueueTicket> _ticketChannel =
        Channel.CreateBounded<QueueTicket>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    /// <summary>
    /// Дугаар олгох үед нэгэн зэрэг хоёр хүсэлт ирвэл нэг нь хүлээдэг lock.
    /// SemaphoreSlim(1,1) ашиглах шалтгаан: async/await дотор
    /// lock{} keyword ажиллахгүй, SemaphoreSlim.WaitAsync() ажиллана.
    /// </summary>
    private readonly SemaphoreSlim _issueLock = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Шинэ тасалбар олгож дарааллд нэмнэ.
    /// Interlocked.Increment ашигласан учир дугаар хэзээ ч давтагдахгүй.
    /// </summary>
    /// <param name="serviceType">Үйлчилгээний төрөл.</param>
    /// <returns>Олгосон тасалбар.</returns>
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
    /// TryRead atomic тул хоёр теллер нэгэн зэрэг дарсан ч нэг дугаар хоёрт очихгүй.
    /// </summary>
    /// <returns>Дуудагдсан дугаар. Дараалал хоосон бол сүүлийн дугаарыг буцаана.</returns>
    public async Task<int> CallNextAsync()
    {
        if (await _ticketChannel.Reader.WaitToReadAsync())
        {
            if (_ticketChannel.Reader.TryRead(out var ticket))
            {
                _currentCalledNumber = ticket.Number;
                return ticket.Number;
            }
        }
        return _currentCalledNumber;
    }

    /// <summary>Одоо хүлээж байгаа хүний тоо.</summary>
    public int GetQueueCount() => _ticketChannel.Reader.Count;

    /// <summary>Теллер хамгийн сүүлд дуудсан дугаар.</summary>
    public int GetCurrentNumber() => _currentCalledNumber;
}