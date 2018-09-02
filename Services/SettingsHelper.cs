using System.Linq;
using System.Threading.Tasks;
using ImageHost.Data;
using ImageHost.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

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
            if (await _context.Settings.AnyAsync(s => s.Key == key))
            {
                _context.Settings.Update(new Setting { Key = key, Val = val});
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