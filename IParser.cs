using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskNotissimusParse
{
    public interface IParser<T>
    {
        Task<T> ParseAsync(params string[] urls);
    }
}
