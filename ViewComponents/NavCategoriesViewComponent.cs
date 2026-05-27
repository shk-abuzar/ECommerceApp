using ECommerceApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.ViewComponents;

public class NavCategoriesViewComponent : ViewComponent
{
    private readonly IProductService _productService;

    public NavCategoriesViewComponent(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IViewComponentResult> InvokeAsync(bool mobile = false)
    {
        var categories = await _productService.GetCategoriesAsync();
        ViewBag.Mobile = mobile;
        return View(categories);
    }
}
