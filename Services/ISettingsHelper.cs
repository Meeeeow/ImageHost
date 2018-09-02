using System.Threading.Tasks;
using ImageHost.Models;

namespace ImageHost.Services
{
    public interface ISettingsHelper
    {
        Task<bool> Write(string key, string val);
        Task<string> Get(string key);
        Task<string> GetOrAdd(string key, string defaultVal);
        void Delete(string key);
    }
}