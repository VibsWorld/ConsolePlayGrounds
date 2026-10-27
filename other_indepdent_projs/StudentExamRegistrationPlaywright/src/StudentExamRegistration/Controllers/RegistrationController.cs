using Microsoft.AspNetCore.Mvc;
namespace StudentExamRegistration.Controllers;
public class RegistrationController:Controller{
[HttpGet("/")] public IActionResult Index()=>View("Step1");
[HttpPost] public IActionResult Step1(string name,string email){TempData["Name"]=name;TempData["Email"]=email;return View("Step2");}
[HttpPost] public IActionResult Step2(string[] courses){var selected=courses??Array.Empty<string>();TempData["Courses"]=string.Join(",",selected);TempData["Fee"]=(selected.Length*500).ToString();return View("Step3");}
[HttpPost] public IActionResult Complete(string receiptId){ViewBag.Receipt=receiptId;return View("Success");}
}