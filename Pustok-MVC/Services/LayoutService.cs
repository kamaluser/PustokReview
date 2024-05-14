using Newtonsoft.Json;
using Pustok_MVC.Data;
using Pustok_MVC.Models;
using Pustok_MVC.ViewModels;

namespace Pustok_MVC.Services
{
    public class LayoutService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LayoutService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        public List<Genre> GetGenres()
        {
            return _context.Genres.ToList();
        }

        public Dictionary<String, String> GetSettings()
        {
            return _context.Settings.ToDictionary(x=>x.Key, x => x.Value);
        }

        public List<BasketViewModel> GetBasket()
        {
            List<BasketViewModel> basketVMList = new List<BasketViewModel>();

            var BasketVMstr = _httpContextAccessor.HttpContext.Request.Cookies["BasketVMList"];
            if (BasketVMstr != null)
            {
                basketVMList = JsonConvert.DeserializeObject<List<BasketViewModel>>(BasketVMstr);
            }

            return basketVMList;
        }
    }
}
