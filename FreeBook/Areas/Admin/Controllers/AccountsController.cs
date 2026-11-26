using System.Globalization;
using System.IO;
using System.Security.Claims;
using Domin.Entity;
using FreeBook.Configuration;
using FreeBook.Resources;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class AccountsController : Controller
    {
        private const string DefaultExternalRole = "User";
        private const string ExternalReturnUrlKey = "externalReturnUrl";

        #region Declration
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAccountQueryService _accountQueries;
        private readonly IStringLocalizer<ResourceWeb> _localizer;
        private readonly IOptions<GoogleAuthSettings> _googleAuth;
        private readonly IEmailSender<ApplicationUser> _emailSender;
        private readonly ILogger<AccountsController> _logger;
        private IFlashMessageService Flash { get; }

        #endregion

        #region Constructor
        public AccountsController(RoleManager<IdentityRole> roleManager,
           UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
           IAccountQueryService accountQueries, IStringLocalizer<ResourceWeb> localizer,
           IOptions<GoogleAuthSettings> googleAuth, IFlashMessageService flash,
           IEmailSender<ApplicationUser> emailSender, ILogger<AccountsController> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _accountQueries = accountQueries;
            _localizer = localizer;
            _googleAuth = googleAuth;
            _emailSender = emailSender;
            _logger = logger;
            Flash = flash;
        }
        #endregion

        #region Methods
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Roles()
        {
            var model = new RolesViewModel()
            {
                NewRole = new NewRole(),
                Roles = await _accountQueries.GetRolesAsync()
            };
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Roles(RolesViewModel model)
        {
            if (ModelState.IsValid)
            {
                var role = new IdentityRole
                {
                    Id = model.NewRole.RoleId,
                    Name = model.NewRole.RoleName
                };
                if (role.Id == null)
                {  //create
                    role.Id = Guid.NewGuid().ToString();
                    var Result = await _roleManager.CreateAsync(role);
                    if (Result.Succeeded)
                        // succeeded
                        Flash.Success(Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                    else
                        //not succeeded
                        Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
                }
                else
                {   //update
                    var RoleUpdate = await _roleManager.FindByIdAsync(role.Id);
                    if (RoleUpdate is null)
                    {
                        Flash.Error(Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotUpdateMsgRole);
                        return RedirectToAction(nameof(Roles));
                    }

                    RoleUpdate.Id = role.Id;
                    RoleUpdate.Name = role.Name;
                    var result = await _roleManager.UpdateAsync(RoleUpdate);
                    if (result.Succeeded)
                        Flash.Success(Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbUpdateMsgRole);

                    else
                        Flash.Error(Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotUpdateMsgRole);
                }
            }
            return RedirectToAction(nameof(Roles));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteRole(string Id)
        {
            var role = await _roleManager.Roles.FirstOrDefaultAsync(x => x.Id == Id);
            if (role is not null && (await _roleManager.DeleteAsync(role)).Succeeded)
            {
                return RedirectToAction(nameof(Roles));
            }

            return RedirectToAction(nameof(Roles));


        }
        [Authorize(Roles = "Admin,User")]

        public async Task<IActionResult> Register()
        {
            var model = new RegisterViewModel
            {
                NewRegister = new NewRegister(),
                Roles = await _accountQueries.GetRolesAsync(),
                Users = await _accountQueries.GetUsersAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,User")]

        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.NewRegister is null)
                return RedirectToAction("Register", "Accounts");

            // The user form only posts NewRegister. The ChangePassword fields live in a
            // different modal/form, but they are part of RegisterViewModel and their
            // [Required] attributes would otherwise make every Register POST invalid.
            ModelState.Remove("changePassword.NewPassword");
            ModelState.Remove("changePassword.ConfirmPassword");

            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    var uploaded = files[0];
                    var extension = Path.GetExtension(uploaded.FileName);

                    if (!Helper.AllowedImageExtensions.Contains(extension) || uploaded.Length > Helper.MaxImageSizeBytes)
                    {
                        Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgCoverImageInvalid"]);
                        return RedirectToAction("Register", "Accounts");
                    }

                    var imageName = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
                    var uploadDirectory = Path.Combine("wwwroot", Helper.PathSaveImageUser);
                    Directory.CreateDirectory(uploadDirectory);

                    await using var fileStream = new FileStream(Path.Combine(uploadDirectory, imageName), FileMode.Create);
                    await uploaded.CopyToAsync(fileStream);
                    model.NewRegister.ImageUser = imageName;
                }


                var user = new ApplicationUser
                {
                    Id = model.NewRegister.Id,
                    Name = model.NewRegister.Name,
                    UserName = model.NewRegister.Email,
                    Email = model.NewRegister.Email,
                    ActiveUser = model.NewRegister.ActiveUser,
                    ImageUser = model.NewRegister.ImageUser

                };
                if (user.Id == null)
                {
                    //Create
                    user.Id = Guid.NewGuid().ToString();
                    var result = await _userManager.CreateAsync(user, model.NewRegister.Password);
                    if (result.Succeeded)
                    {
                        //Succeded
                        var role = await _userManager.AddToRoleAsync(user, model.NewRegister.RoleName);
                        if (role.Succeeded)
                            Flash.Success(Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbMsgSavedOk);
                        else
                            Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
                    }
                    else
                    {
                        //Not Succeded
                        Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
                    }
                }
                else
                {
                    //Update
                    var userUpdate = await _userManager.FindByIdAsync(user.Id);
                    if (userUpdate is null)
                    {
                        Flash.Error(Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbMsgNotUpdatedOk);
                        return RedirectToAction("Register", "Accounts");
                    }

                    userUpdate.Id = model.NewRegister.Id;
                    userUpdate.Name = model.NewRegister.Name;
                    userUpdate.UserName = model.NewRegister.Email;
                    userUpdate.Email = model.NewRegister.Email;
                    userUpdate.ActiveUser = model.NewRegister.ActiveUser;
                    userUpdate.ImageUser = model.NewRegister.ImageUser;

                    var result = await _userManager.UpdateAsync(userUpdate);
                    if (result.Succeeded)
                    {
                        var oldRole = await _userManager.GetRolesAsync(userUpdate);
                        await _userManager.RemoveFromRolesAsync(userUpdate, oldRole);
                        var addRole = await _userManager.AddToRoleAsync(userUpdate, model.NewRegister.RoleName);
                        if (addRole.Succeeded)

                            Flash.Success(Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbMsgUpdatedOk);
                        else

                            Flash.Error(Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbMsgNotUpdatedOk);
                    }
                    else
                    {
                        //Not Succeded
                        Flash.Error(Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbMsgNotUpdatedOk);
                    }
                }
                return RedirectToAction("Register", "Accounts");
            }
            return RedirectToAction("Register", "Accounts");
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null)
                return RedirectToAction("Register", "Accounts");

            var oldRole = await _userManager.GetRolesAsync(user);

            if (user.ImageUser != null && user.ImageUser != Guid.Empty.ToString())
            {
                var imagePath = Path.Combine("wwwroot", Helper.PathSaveImageUser, user.ImageUser);
                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }
            if ((await _userManager.RemoveFromRolesAsync(user, oldRole)).Succeeded)
            {
                if ((await _userManager.DeleteAsync(user)).Succeeded)
                {
                    return RedirectToAction("Register", "Accounts");

                }
            }

            return RedirectToAction("Register", "Accounts");

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,User")]

        public async Task<IActionResult> ChangePassword(RegisterViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.changePassword.Id);
            if (user != null)
            {
                await _userManager.RemovePasswordAsync(user);
                var AddNewPassword = await _userManager.AddPasswordAsync(user, model.changePassword.NewPassword);
                if (AddNewPassword.Succeeded)
                    Flash.Success(Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbMsgSaveChangePassword);
                else
                    Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSaveChangePassword);
                return RedirectToAction("Register");
            }
            return RedirectToAction("Register");

        }


        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        [EnableRateLimiting("login")]

        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email!);
                    return user is null
                        ? RedirectToAction("Index", "Home", new { area = string.Empty })
                        : await RedirectAfterLoginAsync(user);
                }

                ViewBag.ErrorLogin = false;

            }
            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Always show the same message so the endpoint cannot be used to
            // enumerate registered email addresses.
            var user = await _userManager.FindByEmailAsync(model.Email!);
            if (user is not null && !string.IsNullOrWhiteSpace(user.Email))
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetUrl = Url.Action(
                    nameof(ResetPassword),
                    "Accounts",
                    new { area = "Admin", userId = user.Id, code, culture = CultureInfo.CurrentUICulture.Name },
                    Request.Scheme);

                if (!string.IsNullOrWhiteSpace(resetUrl))
                {
                    try
                    {
                        await _emailSender.SendPasswordResetLinkAsync(user, user.Email, resetUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send a password reset link to {Email}", user.Email);
                    }
                }
            }

            Flash.Success(_localizer["lbSendResetLink"].Value, _localizer["msgResetLinkSent"].Value);
            return RedirectToAction(nameof(ForgotPassword));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(string? userId, string? code)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                Flash.Error(_localizer["lbNotSave"].Value, _localizer["msgPasswordResetError"].Value);
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel { UserId = userId, Code = code });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user is null)
            {
                Flash.Error(_localizer["lbNotSave"].Value, _localizer["msgPasswordResetError"].Value);
                return RedirectToAction(nameof(ForgotPassword));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.NewPassword);
            if (result.Succeeded)
            {
                Flash.Success(_localizer["lbSave"].Value, _localizer["msgPasswordResetSuccess"].Value);
                return RedirectToAction(nameof(Login), "Accounts", new { area = "Admin" });
            }

            _logger.LogWarning(
                "Password reset failed for user {UserId}: {Errors}",
                user.Id,
                string.Join("; ", result.Errors.Select(error => error.Code)));
            ModelState.AddModelError(string.Empty, _localizer["msgPasswordResetError"].Value);
            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ExternalLogin(string? returnUrl = null)
        {
            if (!_googleAuth.Value.Enabled)
                return RedirectToAction(nameof(Login));

            var callbackUrl = Url.Action(nameof(ExternalLoginCallback), "Accounts", new { area = "Admin" })!;
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                GoogleDefaults.AuthenticationScheme, callbackUrl);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                properties.Items[ExternalReturnUrlKey] = returnUrl;

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info is null)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgExternalLoginFailed"]);
                return RedirectToAction(nameof(Login));
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);

            if (signInResult.Succeeded)
            {
                var signedInEmail = info.Principal.FindFirstValue(ClaimTypes.Email);
                var signedInUser = string.IsNullOrWhiteSpace(signedInEmail)
                    ? null
                    : await _userManager.FindByEmailAsync(signedInEmail);

                if (signedInUser is null)
                {
                    Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgExternalLoginFailed"]);
                    return RedirectToAction(nameof(Login));
                }

                return await RedirectAfterLoginAsync(
                    signedInUser,
                    ResolveExternalReturnUrl(returnUrl, info));
            }

            if (signInResult.IsLockedOut || signInResult.RequiresTwoFactor)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgExternalLoginFailed"]);
                return RedirectToAction(nameof(Login));
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgExternalLoginEmailMissing"]);
                return RedirectToAction(nameof(Login));
            }

            var displayName = info.Principal.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = email.Split('@')[0];

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = displayName,
                    ActiveUser = true,
                    EmailConfirmed = true,
                    ImageUser = string.Empty
                };

                var created = await _userManager.CreateAsync(user);
                if (!created.Succeeded)
                {
                    Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgExternalLoginFailed"]);
                    return RedirectToAction(nameof(Login));
                }
            }
            else if (!user.ActiveUser)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgExternalLoginFailed"]);
                return RedirectToAction(nameof(Login));
            }

            if (!await EnsureExternalRoleExistsAsync())
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgExternalLoginFailed"]);
                return RedirectToAction(nameof(Login));
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count == 0)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, DefaultExternalRole);
                if (!roleResult.Succeeded)
                {
                    Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgExternalLoginFailed"]);
                    return RedirectToAction(nameof(Login));
                }
            }

            var addLogin = await _userManager.AddLoginAsync(user, info);
            if (!addLogin.Succeeded)
            {
                Flash.Error(Resources.ResourceWeb.lbNotSave, _localizer["msgExternalLoginFailed"]);
                return RedirectToAction(nameof(Login));
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return await RedirectAfterLoginAsync(user, ResolveExternalReturnUrl(returnUrl, info));
        }

        private async Task<IActionResult> RedirectAfterLoginAsync(ApplicationUser user, string? returnUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            var roles = await _userManager.GetRolesAsync(user);
            return roles.Contains(Helper.Admin)
                ? RedirectToAction("Index", "Dashboard", new { area = Helper.Admin })
                : RedirectToAction("Index", "Home", new { area = string.Empty });
        }

        private static string? ResolveExternalReturnUrl(string? returnUrl, ExternalLoginInfo info)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl))
                return returnUrl;

            return info.AuthenticationProperties?.Items.TryGetValue(ExternalReturnUrlKey, out var saved) == true
                ? saved
                : null;
        }

        private async Task<bool> EnsureExternalRoleExistsAsync()
        {
            if (await _roleManager.RoleExistsAsync(DefaultExternalRole))
                return true;

            return (await _roleManager.CreateAsync(new IdentityRole(DefaultExternalRole))).Succeeded;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOut(LoginViewModel model)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        #endregion
    }
}

