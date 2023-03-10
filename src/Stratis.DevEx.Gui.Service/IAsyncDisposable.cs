using System.Threading.Tasks;
namespace Stratis.DevEx
{
    public interface IAsyncDisposable
    {
        Task DisposeAsync();
    }
}
