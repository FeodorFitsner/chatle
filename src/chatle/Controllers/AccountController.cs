﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using ChatLe.Hubs;
using ChatLe.Models;
using ChatLe.ViewModels;
using System.Net;
using ChatLe.Repository.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ChatLe.Controllers
{
    public class Role
    {
        public string Name { get; set; }
    }

    [Authorize]
    public class AccountController : Controller
    {
        public AccountController(UserManager<ChatLeUser> userManager, 
            SignInManager signInManager,
            IChatManager<string, ChatLeUser, Conversation, Attendee, Message, NotificationConnection> chatManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            ChatManager = chatManager;
        }

        public UserManager<ChatLeUser> UserManager { get; private set; }

        public SignInManager<ChatLeUser> SignInManager { get; private set; }

        public IChatManager<string, ChatLeUser, Conversation, Attendee, Message, NotificationConnection> ChatManager { get; private set; }

        // GET: /Account/Index
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string returnUrl = null, string reason = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginPageViewModel());
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var signInStatus = await SignInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (signInStatus.Succeeded)
                    return RedirectToLocal(returnUrl);

                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            // If we got this far, something failed, redisplay form
            return View("Index", new LoginPageViewModel() { Login = model });
        }

        //
        // POST: /Account/SpaLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SpaLogin(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var signInStatus = await SignInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (signInStatus.Succeeded)
                    return new JsonResult(signInStatus.Succeeded);

                ModelState.AddModelError("", "Invalid username or password.");                
            }

            return ReturnSpaError();
        }

        //
        // POST: /Account/Guess
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guess(GuessViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = new ChatLeUser { UserName = model.UserName };

                var result = await UserManager.CreateAsync(user);
                
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                else
                    AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View("Index", new LoginPageViewModel() { Guess = model });
        }

        //
        // POST: /Account/SpaGuess
        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> SpaGuess([FromBody] GuessViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ChatLeUser { UserName = model.UserName };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false);
                    return new JsonResult(result.Succeeded);
                }
                else
                {
                    AddErrors(result);
                }
            }

            return ReturnSpaError();
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ChatLeUser { UserName = model.UserName };
                var result = await UserManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                else
                    AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Manage
        [HttpGet]
        public IActionResult Manage(ManageMessageId? message = null)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageUserViewModel model)
        {
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (ModelState.IsValid)
            {
                var user = await GetCurrentUserAsync();
                var result = await UserManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (result.Succeeded)
                    return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                else
                    AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // PUT: /Account/ChangePassword
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([FromBody] ManageUserViewModel model)
        {
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (ModelState.IsValid)
            {
                var user = await GetCurrentUserAsync();
                var result = await UserManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (result.Succeeded)
                    return new JsonResult(ManageMessageId.ChangePasswordSuccess);
                else
                    AddErrors(result);
            }

            return ReturnSpaError();
        }

        //
        // POST: /Account/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword([FromBody] CreatePasswordViewModel model)
        {
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (ModelState.IsValid)
            {
                var user = await GetCurrentUserAsync();
                var result = await UserManager.AddPasswordAsync(user, model.NewPassword);
                if (result.Succeeded)
                    return new JsonResult(ManageMessageId.ChangePasswordSuccess);
                else
                    AddErrors(result);
            }

            return ReturnSpaError();
        }

        JsonResult ReturnSpaError()
        {
            Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return new JsonResult(ModelState.Root.Children);            
        }
        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff([FromServices] IConnectionManager signalRConnectionManager, string reason = null)
        {
            var user = await GetCurrentUserAsync();
			if (user != null)
			{
				if (user.IsGuess)
				{
					var hub = signalRConnectionManager.GetHubContext<ChatHub>();
					hub.Clients.All.userDisconnected(user.UserName);
					await ChatManager.RemoveUserAsync(user);
				}
			}
            await SignInManager.SignOutAsync();
			return RedirectToAction("Index", routeValues: new { Reason = reason });
        }

        //
        // POST: /Account/SpaLogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task SpaLogOff([FromServices] IConnectionManager signalRConnectionManager, string reason = null)
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                if (user.IsGuess)
                {
                    var hub = signalRConnectionManager.GetHubContext<ChatHub>();
                    hub.Clients.All.userDisconnected(user.UserName);
                    await ChatManager.RemoveUserAsync(user);
                }
            }

            await SignInManager.SignOutAsync();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
        }

        private async Task<ChatLeUser> GetCurrentUserAsync()
        {
            return await UserManager.GetUserAsync(HttpContext.User);
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            Error
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction("Index", "Home");
        }
        #endregion
    }
}