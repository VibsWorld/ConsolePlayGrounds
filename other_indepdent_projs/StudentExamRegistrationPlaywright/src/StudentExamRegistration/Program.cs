using Microsoft.AspNetCore.Builder;

var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
var app=builder.Build();
app.MapDefaultControllerRoute();
app.Run();
public partial class Program {}
