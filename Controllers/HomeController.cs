using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stock_Manager.Models;

namespace Stock_Manager.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult AboutUs()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser { UserName = model.UserName, Password = model.Password };

            // Create the user without any extra validation
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Log the user in immediately after registration
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Redirect to homepage or portfolio after successful login
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // If registration failed, add error and return to view
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }

        return View(model);
    }




    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        // If there is a return URL, store it in the ViewData so we can use it later
        ViewData["ReturnUrl"] = returnUrl;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            // Find the user by username (you could also use email or another unique identifier)
            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                // Sign the user in
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Redirect to the originally requested page or the home page
                if (returnUrl != null && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl); // Redirect to the original requested page
                }
                else
                {
                    return RedirectToAction("Index", "Home"); // Redirect to the home page if no return URL
                }
            }
            else
            {
                // Add an error message if login is invalid
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
        }

        // If we get here, something went wrong, so return the model to the view
        return View(model);
    }


    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Home");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
