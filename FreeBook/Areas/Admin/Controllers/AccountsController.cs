using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class AccountsController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly FreeBookDbContext _context;

        public AccountsController(RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, FreeBookDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Roles()
        {
            var model = new RolesViewModel()
            {
                NewRole = new NewRole(),
                Roles = _roleManager.Roles.OrderBy(x => x.Name).ToList()

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
                        SessuinMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                    else                
                        //not succeeded
                        SessuinMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole)
                }
                else
                {   //update
                    var RoleUpdate = await _roleManager.FindByIdAsync(role.Id);
                    RoleUpdate.Id = role.Id;
                    RoleUpdate.Name = role.Name;
                    var result = await _roleManager.UpdateAsync(RoleUpdate);
                    if (result.Succeeded)
                    {
                        HttpContext.Session.SetString("msgType", "success");
                        HttpContext.Session.SetString("title", Resources.ResourceWeb.lbUpdate);
                        HttpContext.Session.SetString("msg", Resources.ResourceWeb.lbUpdateMsgRole);

                    }
                    else
                    {
                        HttpContext.Session.SetString("msgType", "error");
                        HttpContext.Session.SetString("title", Resources.ResourceWeb.lbNotUpdate);
                        HttpContext.Session.SetString("msg", Resources.ResourceWeb.lbNotUpdateMsgRole);
                    }

                }
            }
            return RedirectToAction(nameof(Roles));
        }

        private void SessuinMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }

        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> DeleteRole(string Id)
        {
            var role = _roleManager.Roles.FirstOrDefault(x => x.Id == Id);
            if ((await _roleManager.DeleteAsync(role)).Succeeded)
            {
                return RedirectToAction(nameof(Roles));
            }

            return RedirectToAction(nameof(Roles));


        }
        [Authorize(Roles = "Admin,User")]

        public IActionResult Register()
        {
            var model = new RegisterViewModel
            {
                NewRegister = new NewRegister(),
                Roles = _roleManager.Roles.OrderBy(x => x.Name).ToList(),
                Users = _context.VwUsers.OrderBy(x => x.Role).ToList()   // _userManager.Users.OrderBy(x=>x.Name).ToList()


            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,User")]

        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var file = HttpContext.Request.Form.Files;
                if (file.Count() > 0)
                {
                    string ImageName = Guid.NewGuid().ToString() + Path.GetExtension(file[0].FileName);
                    var fileStream = new FileStream(Path.Combine(@"wwwroot/", Helper.PathSaveImageUser, ImageName), FileMode.Create);
                    file[0].CopyTo(fileStream);
                    model.NewRegister.ImageUser = ImageName;

                }
                else
                {
                    model.NewRegister.ImageUser = model.NewRegister.ImageUser;
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
                        {
                            HttpContext.Session.SetString("msgType", "success");
                            HttpContext.Session.SetString("title", Resources.ResourceWeb.lbSave);
                            HttpContext.Session.SetString("msg", Resources.ResourceWeb.lbSaveMsgRole);
                        }
                        else
                        {
                            HttpContext.Session.SetString("msgType", "error");
                            HttpContext.Session.SetString("title", Resources.ResourceWeb.lbNotSave);
                            HttpContext.Session.SetString("msg", Resources.ResourceWeb.lbNotSaveMsgRole);
                        }
                    }
                    else
                    {
                        //Not Succeded
                        HttpContext.Session.SetString("msgType", "error");
                        HttpContext.Session.SetString("title", Resources.ResourceWeb.lbNotSave);
                        HttpContext.Session.SetString("msg", Resources.ResourceWeb.lbNotSaveMsgRole);
                    }
                }
                else
                {
                    //Update
                    var userUpdate = await _userManager.FindByIdAsync(user.Id);
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
                        {
                            HttpContext.Session.SetString("msgType", "success");
                            HttpContext.Session.SetString("title", Resources.ResourceWeb.lbUpdate);
                            HttpContext.Session.SetString("msg", Resources.ResourceWeb.lbUpdateMsgRole);
                        }
                        else
                        {
                            HttpContext.Session.SetString("msgType", "error");
                            HttpContext.Session.SetString("title", Resources.ResourceWeb.lbNotUpdate);
                            HttpContext.Session.SetString("msg", Resources.ResourceWeb.lbNotSaveMsgRole);
                        }

                    }
                    else
                    {
                        //Not Succeded
                        HttpContext.Session.SetString("msgType", "error");
                        HttpContext.Session.SetString("title", Resources.ResourceWeb.lbNotUpdate);
                        HttpContext.Session.SetString("msg", Resources.ResourceWeb.lbNotSaveMsgRole);
                    }
                }
                return RedirectToAction("Register", "Accounts");
            }
            return RedirectToAction("Register", "Accounts");
        }
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = _userManager.Users.FirstOrDefault(x => x.Id == userId);
            //Add this Row Number One
            var oldRole = await _userManager.GetRolesAsync(user);

            if (user.ImageUser != null && user.ImageUser != Guid.Empty.ToString())
            {
                var ImagePath = Path.Combine(@"wwwroot/", Helper.PathImageUser, user.ImageUser);
                if (System.IO.File.Exists(ImagePath))
                    System.IO.File.Delete(ImagePath);
            }
            //Add this Row Number Two
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
                {
                    HttpContext.Session.SetString("msgType", "success");
                    HttpContext.Session.SetString("title", Resources.ResourceWeb.lbSave);
                    HttpContext.Session.SetString("msg", Resources.ResourceWeb.lbMsgSaveChangePassword);
                }
                else
                {
                    HttpContext.Session.SetString("msgType", "error");
                    HttpContext.Session.SetString("title", Resources.ResourceWeb.lbNotSave);
                    HttpContext.Session.SetString("msg", Resources.ResourceWeb.lbMsgNotSaveChangePassword);
                }
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

    }
}

