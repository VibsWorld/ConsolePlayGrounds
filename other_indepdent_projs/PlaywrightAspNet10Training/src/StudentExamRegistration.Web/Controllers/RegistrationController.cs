using Microsoft.AspNetCore.Mvc;
using StudentExamRegistration.Web.Models;

namespace StudentExamRegistration.Web.Controllers;

public class RegistrationController : Controller
{
    private const decimal FeePerCourse = 500m;

    [HttpGet]
    public IActionResult Step1() => View(new RegistrationViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Step1(RegistrationViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        TempData["Name"] = model.Name;
        TempData["Email"] = model.Email;
        return RedirectToAction(nameof(Step2));
    }

    [HttpGet]
    public IActionResult Step2() => View(new RegistrationViewModel
    {
        Name = TempData.Peek("Name")?.ToString() ?? string.Empty,
        Email = TempData.Peek("Email")?.ToString() ?? string.Empty
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Step2(RegistrationViewModel model)
    {
        model.Name = TempData.Peek("Name")?.ToString() ?? string.Empty;
        model.Email = TempData.Peek("Email")?.ToString() ?? string.Empty;

        if (model.Courses is null || model.Courses.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Courses), "Select at least one course.");
            return View(model);
        }

        TempData["CoursesCsv"] = string.Join(",", model.Courses);
        TempData["Fee"] = (model.Courses.Count * FeePerCourse).ToString("0.##");
        return RedirectToAction(nameof(Step3));
    }

    [HttpGet]
    public IActionResult Step3() => View(new RegistrationViewModel
    {
        Name = TempData.Peek("Name")?.ToString() ?? string.Empty,
        Email = TempData.Peek("Email")?.ToString() ?? string.Empty,
        Courses = ReadCourses()
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Complete(RegistrationViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ReceiptId))
        {
            ModelState.AddModelError(nameof(model.ReceiptId), "Receipt id is required.");
            model.Name = TempData.Peek("Name")?.ToString() ?? string.Empty;
            model.Email = TempData.Peek("Email")?.ToString() ?? string.Empty;
            model.Courses = ReadCourses();
            return View("Step3", model);
        }

        TempData["ReceiptId"] = model.ReceiptId;
        return RedirectToAction(nameof(Success));
    }

    [HttpGet]
    public IActionResult Success() => View(new RegistrationViewModel
    {
        Name = TempData.Peek("Name")?.ToString() ?? string.Empty,
        Email = TempData.Peek("Email")?.ToString() ?? string.Empty,
        Courses = ReadCourses(),
        ReceiptId = TempData.Peek("ReceiptId")?.ToString() ?? string.Empty
    });

    [HttpGet("api/courses")]
    public IActionResult Courses() => Json(new[] { "Math", "Physics", "Chemistry", "Biology" });

    private List<string> ReadCourses() =>
        (TempData.Peek("CoursesCsv") as string ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
}