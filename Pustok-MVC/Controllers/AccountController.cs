using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pustok_MVC.Models;
using Pustok_MVC.ViewModels;
using System.Security.Claims;

namespace Pustok_MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
		private readonly SignInManager<AppUser> _signInManager;

		public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
			_signInManager = signInManager;
		}
        public IActionResult Register()
        {
            return View();
        }

		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(MemberRegisterViewModel model)
		{
			if (!ModelState.IsValid) return View(model);

			if (_userManager.Users.Any(x => x.NormalizedEmail == model.Email.ToUpper()))
			{
				ModelState.AddModelError("Email", "Email is already taken");
				return View();
			}


			AppUser user = new AppUser
			{
				UserName = model.UserName,
				Email = model.Email,
				Fullname = model.FullName
			};

			var result = await _userManager.CreateAsync(user, model.Password);

			if (!result.Succeeded)
			{
				foreach (var err in result.Errors)
				{
					if (err.Code == "DuplicateUserName")
						ModelState.AddModelError("UserName", "UserName is already registered!");
					else ModelState.AddModelError("", err.Description);
				}
				return View();
			}
			await _userManager.AddToRoleAsync(user, "member");

			return RedirectToAction("index", "home");
		}


		public IActionResult Login()
		{
			return View();
		}

		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(MemberLoginModel model, string? returnUrl)
		{
			if ((!ModelState.IsValid)) return View();

			AppUser? user = await _userManager.FindByEmailAsync(model.Email);

			if (user == null || !await _userManager.IsInRoleAsync(user, "member"))
			{
				ModelState.AddModelError("","Email or Password is incorrect!");
				return View();
			}

			var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, true);

			if (result.IsLockedOut)
			{
				ModelState.AddModelError("", "You are locked out for 5 minutes!");
				return View();
			}

            else if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Email or Password incorrect");
                return View();
            }

			return returnUrl!=null ? Redirect(returnUrl) : RedirectToAction("index","home");

		}

		[Authorize(Roles ="member")]	
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return RedirectToAction("Index", "Home");
		}

        [Authorize(Roles ="member")]
        public async Task<IActionResult> Profile(string tab = "dashboard")
        {
			AppUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("login", "account");
            }

			ProfileViewModel profileVM = new ProfileViewModel
			{
				ProfileEditVM = new ProfileEditViewModel
				{
					FullName = user.Fullname,
					Email = user.Email,
					UserName = user.UserName
				}
			};

			ViewBag.Tab = tab;

			return View(profileVM);
        }

		[Authorize(Roles ="member")]
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileEditViewModel editVM, string tab="profile")
		{
			ViewBag.Tab = tab;

            ProfileViewModel profileVM = new ProfileViewModel();
            profileVM.ProfileEditVM = editVM;

            if (!ModelState.IsValid) return View(profileVM);

            AppUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("login", "account");
            }
			user.UserName = editVM.UserName;
			user.Email = editVM.Email;
			user.Fullname = editVM.FullName;

            if (_userManager.Users.Any(x => x.Id != User.FindFirstValue(ClaimTypes.NameIdentifier) && x.NormalizedEmail == editVM.Email.ToUpper()))
            {
                ModelState.AddModelError("Email", "Email is already taken!");
                return View(profileVM);
            }

            if (editVM.NewPassword != null)
            {
                var passwordResult = await _userManager.ChangePasswordAsync(user, editVM.CurrentPassword, editVM.NewPassword);

                if (!passwordResult.Succeeded)
                {
                    foreach (var err in passwordResult.Errors)
                        ModelState.AddModelError("", err.Description);

                    return View(profileVM);
                }
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    if (err.Code == "DuplicateUserName")
                        ModelState.AddModelError("UserName", "UserName is already taken");
                    else ModelState.AddModelError("", err.Description);
                }
                return View(profileVM);
            }

            await _signInManager.SignInAsync(user, false);

            return View(profileVM);
        }

    }
}
