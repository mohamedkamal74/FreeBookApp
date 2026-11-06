using System.IO;
using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class AccountsController : Controller
    {
        #region Declration
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly FreeBookDbContext _context;

        #endregion

        #region Constructor
        public AccountsController(RoleManager<IdentityRole> roleManager,
           UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, FreeBookDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }
        #endregion

        #region Methods
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Roles()
        {
            var model = new RolesViewModel()
            {
                NewRole = new NewRole(),
                Roles = await _roleManager.Roles.OrderBy(x => x.Name).ToListAsync()
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
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                    else
                        //not succeeded
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
                }
                else
                {   //update
                    var RoleUpdate = await _roleManager.FindByIdAsync(role.Id);
                    if (RoleUpdate is null)
                    {
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotUpdateMsgRole);
                        return RedirectToAction(nameof(Roles));
                    }

                    RoleUpdate.Id = role.Id;
                    RoleUpdate.Name = role.Name;
                    var result = await _roleManager.UpdateAsync(RoleUpdate);
                    if (result.Succeeded)
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbUpdateMsgRole);

                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotUpdateMsgRole);
                }
            }
            return RedirectToAction(nameof(Roles));
        }

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
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
                Roles = await _roleManager.Roles.OrderBy(x => x.Name).ToListAsync(),
                Users = await _context.VwUsers.OrderBy(x => x.Role).ToListAsync()


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

            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    var uploaded = files[0];
                    var extension = Path.GetExtension(uploaded.FileName);

                    if (!Helper.AllowedImageExtensions.Contains(extension) || uploaded.Length > Helper.MaxImageSizeBytes)
                    {
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave,
                            "Only JPG, JPEG, PNG, GIF, or WEBP images up to 5 MB are allowed.");
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
                            SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                        else
                            SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
                    }
                    else
                    {
                        //Not Succeded
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
                    }
                }
                else
                {
                    //Update
                    var userUpdate = await _userManager.FindByIdAsync(user.Id);
                    if (userUpdate is null)
                    {
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotSaveMsgRole);
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

                            SessionMsg(Helper.Success, Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbUpdateMsgRole);
                        else

                            SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotSaveMsgRole);
                    }
                    else
                    {
                        //Not Succeded
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotSaveMsgRole);
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
                    SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbMsgSaveChangePassword);
                else
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSaveChangePassword);
                return RedirectToAction("Register");
            }
            return RedirectToAction("Register");

        }


        [AllowAnonymous]
        [Authorize(Roles = "Admin,User")]

        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        [Authorize(Roles = "Admin,User")]
        [EnableRateLimiting("login")]

        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                    return RedirectToAction("Index", "Home");
                else
                    ViewBag.ErrorLogin = false;

            }
            return View(model);
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

