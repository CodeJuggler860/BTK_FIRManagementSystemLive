using FIRManagementSystem.DataAccess;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Web.Mvc;
using System.Web.Security;

namespace FIRManagementSystem2.Controllers
{
    public class AccountController : Controller
    {
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password, bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            // Verify credentials against APP_USER table
            if (ValidateUser(username, password))
            {
                // Create authentication cookie
                FormsAuthentication.SetAuthCookie(username, rememberMe);
                return RedirectToAction("Index", "FIR"); // go to dashboard
            }
            else
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }
        }
        private bool ValidateUser(string username, string password)
        {
            using (var conn = OracleDbHelper.GetConnection())
            using (var cmd = new OracleCommand(
                "SELECT COUNT(*) FROM APP_USER WHERE USERNAME = :uname AND PASSWORD = :pwd AND IS_ACTIVE = 'Y'", conn))
            {
                cmd.Parameters.Add("uname", OracleDbType.Varchar2).Value = username;
                cmd.Parameters.Add("pwd", OracleDbType.Varchar2).Value = password;
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
    }
}