using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NuGet.ProjectModel;
using Pustok_MVC.Data;
using Pustok_MVC.Models;
using Pustok_MVC.ViewModels;
using System.Security.Claims;

namespace Pustok_MVC.Controllers
{
    public class BookController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BookController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult GetBookById(int id)
        {
            Book book = _context.Books.Include(x => x.Genre).Include(x => x.BookImages.Where(x => x.PosterStatus == true)).FirstOrDefault(x => x.Id == id);
            return PartialView("_BookModalContentPartial", book);
        }

        public IActionResult Detail(int id)
        {
            var vm = getBookDetailVM(id);

            if (vm.Book == null) return RedirectToAction("notfound", "error");

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(BookReview review)
        {
            AppUser? user = await _userManager.GetUserAsync(User);

            if (user == null || !await _userManager.IsInRoleAsync(user, "member"))
            {
                return RedirectToAction("login", "account", new { returnUrl = Url.Action("detail", "book", new { id = review.BookId }) });
            }

            if (!_context.Books.Any(x => x.Id == review.BookId && !x.IsDeleted))
            {
                return RedirectToAction("notfound", "error");
            }

            if (_context.BookReviews.Any(x => x.Id == review.BookId && x.AppUserId == user.Id))
            {
                return RedirectToAction("notfound", "error");
            }

            if (!ModelState.IsValid)
            {
                var vm = getBookDetailVM(review.BookId);
                vm.Review = review;
                return View("detail", vm);
            }

            review.AppUserId = user.Id;
            review.CreatedAt = DateTime.Now;

            _context.BookReviews.Add(review);
            _context.SaveChanges();

            return RedirectToAction("Detail", new { id = review.BookId });
        }




        public async Task<IActionResult> AddToBasket(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);

            if (book == null)
            {
                return NotFound();
            }

            List<BasketViewModel> basketVMList = new List<BasketViewModel>();
            BasketViewModel basketVM = null;
            AppUser appUser = null;
            var basketVMstr = HttpContext.Request.Cookies["basketVMList"];

            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                if (basketVMstr != null)
                {
                    basketVMList = JsonConvert.DeserializeObject<List<BasketViewModel>>(basketVMstr);

                    var basketViewM = basketVMList.FirstOrDefault(bvm => bvm.BookId == bookId);

                    if (basketViewM != null)
                    {
                        basketViewM.Count++;

                    }
                    else
                    {
                        double discountPercent = book.DiscountPercent == null ? book.DiscountPercent : 0;
                        basketVM = new BasketViewModel
                        {
                            BookId = bookId,
                            Count = 1,
                            Title = book.Name,
                            Price = book.SalePrice * (100 - discountPercent) / 100,
                            ImageUrl = book.BookImages.FirstOrDefault(x => x.PosterStatus == true).Name

                        };
                        basketVM.TotalAmount = book.SalePrice * (100 - discountPercent) / 100 * basketVM.Count;
                        basketVMList.Add(basketVM);
                    }
                }
                else
                {
                    basketVM = new BasketViewModel
                    {
                        BookId = bookId,
                        Count = 1
                    };
                    basketVMList.Add(basketVM);
                }

                var BasketVMstr = JsonConvert.SerializeObject(basketVMList);

                HttpContext.Response.Cookies.Append("basketVMList", BasketVMstr);
            }
            else
            {
                string Username = HttpContext.User.Identity.Name;
                appUser = await _userManager.FindByNameAsync(Username);
                var basketItem = await _context.BasketItems.FirstOrDefaultAsync(bi => bi.AppUserId == appUser.Id && bi.BookId == bookId);
                if (basketItem != null)
                {
                    basketItem.Count++;
                }
                else
                {
                    BasketItem BasketItem = new BasketItem
                    {
                        AppUserId = appUser.Id,
                        BookId = bookId,
                        Count = 1,
                    };
                    _context.BasketItems.Add(BasketItem);
                }
            }
            _context.SaveChanges();

            return Ok();

            return RedirectToAction("index", "home");
        }

        public IActionResult GetBasket()
        {
            List<BasketViewModel> basketVMList = new List<BasketViewModel>();

            var BasketVMstr = HttpContext.Request.Cookies["BasketVMList"];
            if (BasketVMstr != null)
            {
                basketVMList = JsonConvert.DeserializeObject<List<BasketViewModel>>(BasketVMstr);
            }

            return Ok(basketVMList);
        }


        private BookDetailViewModel getBookDetailVM(int bookId)
        {


            Book? book = _context.Books
              .Include(x => x.Genre)
              .Include(x => x.Author)
              .Include(x => x.BookImages)
              .Include(x => x.BookReviews).ThenInclude(r => r.AppUser)
              .Include(x => x.BookTags).ThenInclude(bt => bt.Tag)
              .FirstOrDefault(x => x.Id == bookId && !x.IsDeleted);

            if (book == null)
            {
                return null;
            }

            int totalCommentPage = book.BookReviews?.Count() ?? 0;
            ViewBag.TotalCommentPage = totalCommentPage > 0 ? Math.Ceiling(totalCommentPage / 2m) : 0;


            BookDetailViewModel vm = new BookDetailViewModel
            {
                Book = book,
                RelatedBooks = _context.Books
                       .Include(x => x.Author)
                       .Include(x => x.BookImages
                               .Where(bi => bi.PosterStatus != null))
                       .Where(x => book != null && x.GenreId == book.GenreId)
                       .Take(5).ToList(),
                Review = new BookReview { BookId = bookId }
            };

            AppUser? user = _userManager.GetUserAsync(User).Result;

            if (user == null)
            {

                return vm;
            }
            if (_userManager.IsInRoleAsync(user, "member").Result && _context.BookReviews.Any(x => x.BookId == bookId && x.AppUserId == user.Id && x.Status != Models.Enums.ReviewStatus.Rejected))
            {
                vm.HasUserReview = true;
            }

            vm.TotalReviewsCount = _context.BookReviews.Count(x => x.BookId == bookId && x.Status == Models.Enums.ReviewStatus.Accepted);
            vm.AvgRate = vm.TotalReviewsCount > 0 ? (int)Math.Ceiling(_context.BookReviews.Where(x => x.BookId == bookId && x.Status == Models.Enums.ReviewStatus.Accepted).Average(x => x.Rate)) : 0;

            return vm;
        }


        public IActionResult LoadReview(int id, int page = 1)
        {
            List<BookReview> comments = _context.BookReviews.Include(x => x.AppUser).Where(x => x.BookId == id).OrderByDescending(x => x.CreatedAt).Skip((page - 1) * 2).Take(2).ToList();

            return PartialView("_BookReviews", comments);
        }

    }
}