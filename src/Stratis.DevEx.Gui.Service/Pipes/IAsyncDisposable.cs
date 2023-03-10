using System.Threading.Tasks;
namespace Stratis.DevEx.Pipes
{
    public interface IAsyncDisposable
    {
        Task DisposeAsync();
    }
}
