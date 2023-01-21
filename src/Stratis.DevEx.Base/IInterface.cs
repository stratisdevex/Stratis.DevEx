namespace Stratis.DevEx
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IInterface
    {
        [DebuggerStepThrough]
        public T GetTimed<T>(Func<T> p, string status, string messageTemplate, params object[] o);

        public void DownloadFile(string name, Uri downloadUrl, string downloadPath);
    }
}
