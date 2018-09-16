using System.Threading.Tasks;
using ImageHost.Data;
using ImageHost.Models;
using Microsoft.EntityFrameworkCore;

namespace ImageHost.Services
{
    public class SettingsHelper : ISettingsHelper
    {
        private readonly ApplicationDbContext _context;

        public SettingsHelper(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Write(string key, string val)
        {
            var setting = await _context.Settings.FirstAsync(s => s.Key == key);
            if (setting != null)
            {
                setting.Val = val;
                _context.Settings.Update(setting);
            }
            else
            {
                await _context.Settings.AddAsync(new Setting {Key = key, Val = val});
            }

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<string> Get(string key)
        {
            Setting setting = await _context.Settings.FindAsync(key);
            if (setting != null && setting.GetType() == typeof(Setting))
            {
                return setting.Val;
            }

            return null;
        }

        public async Task<string> GetOrAdd(string key, string defaultVal)
        {
            var setting = await _context.Settings.FindAsync(key);
            if (setting != null && setting.GetType() == typeof(Setting))
            {
                return setting.Val;
            }
            else
            {
                await _context.Settings.AddAsync(new Setting {Key = key, Val = defaultVal});
                await _context.SaveChangesAsync();

                return defaultVal;
            }
        }

        public async void Delete(string key)
        {
            var setting = await _context.Settings.FindAsync(key);
            if (setting != null && setting.GetType() != typeof(Setting)) return;
            
            _context.Settings.Remove(setting);
            await _context.SaveChangesAsync();
        }
    }
}