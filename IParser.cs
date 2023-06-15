using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskNotissimusParse
{
    public interface IParser<T>
    {
        Task<T> ParseAsync(string html, RequestMetadata? metadata = null);
    }

    public class RequestMetadata
    {
        public Uri Uri { get; init; }
    }
}
