var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
var app=builder.Build();
app.UseStaticFiles();
app.MapControllerRoute(name:"default",pattern:"{controller=Registration}/{action=Step1}/{id?}");
app.Run();
public partial class Program{}