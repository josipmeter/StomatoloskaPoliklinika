using Microsoft.AspNetCore.Mvc;

namespace StomatoloskaPoliklinika.ViewComponents
{
  public class NavigationViewComponent : ViewComponent
  {
    public IViewComponentResult Invoke()
    {
      ViewBag.Action = RouteData?.Values["action"];
      return View();
    }
  }
}
