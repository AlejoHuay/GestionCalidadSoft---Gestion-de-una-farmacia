using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProyectoArqSoft.Pages.Auth
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            return CerrarSesion();
        }

        public IActionResult OnPost()
        {
            return CerrarSesion();
        }

        private RedirectToPageResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Index");
        }
    }
}
