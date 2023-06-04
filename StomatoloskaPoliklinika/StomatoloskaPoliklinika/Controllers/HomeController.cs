using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StomatoloskaPoliklinika.Models;
using StomatoloskaPoliklinika.Util;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace StomatoloskaPoliklinika.Controllers
{
    public class HomeController : Controller
    {
        private const string CoordinatorsGroup = "Coordinators"; // ili Kordinatori

        public async Task<IActionResult> Index(string user)
        {
            DashboardData data = new DashboardData();
            data.ProcessInstances = await CamundaUtil.GetUgovoreniSastanci();
            data.MyTasks = await CamundaUtil.GetTasks(user);
            if (await CamundaUtil.IsUserInGroup(user, CoordinatorsGroup))
            {
                data.CoordinatorsTasks = await CamundaUtil.UnAssignedGroupTasks(CoordinatorsGroup);
            }

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> ApplyForUgovaranjeSastanka(string user, string pid)
        {
            await CamundaUtil.ApplyForUgovaranjeSastanka(pid, user);
            return RedirectToAction(nameof(Index), new { user });
        }

        [HttpPost]
        public async Task<IActionResult> PickSastanak(string user, string taskId)
        {
            await CamundaUtil.PickSastanak(taskId, user);
            return RedirectToAction(nameof(Index), new { user });
        }

        [HttpPost]
        public async Task<IActionResult> FinishSastanak(string user, string taskId)
        {
            await CamundaUtil.FinishSastanak(taskId);
            return RedirectToAction(nameof(Index), new { user });
        }

        [HttpPost]
        public async Task<IActionResult> FinishUgovaranjeSastanka(string user, string taskId, bool ok)
        {
            await CamundaUtil.FinishUgovaranjeSastanka(taskId, ok);
            return RedirectToAction(nameof(Index), new { user });
        }

        [HttpPost]
        public async Task<IActionResult> AssignStomatolog(string user, string stomatolog, string taskId)
        {
            await CamundaUtil.AssignStomatolog(taskId, stomatolog);
            return RedirectToAction(nameof(Index), new { user });
        }


        [HttpGet]
        public IActionResult Start(string user, DateTime date)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Start(string user, int id, DateTime date)
        {
            var pid = await CamundaUtil.StartUgovaranjeSastankaProcess(id, user, date);

            return RedirectToAction(nameof(Index), new { user });
        }

        public async Task<ActionResult<string>> Diagram()
        {
            var xml = await CamundaUtil.GetXmlDefinition();
            return xml;
        }


    }
}