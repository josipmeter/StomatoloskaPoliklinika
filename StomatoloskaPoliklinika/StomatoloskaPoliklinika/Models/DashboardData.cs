using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StomatoloskaPoliklinika.Models
{
  public class DashboardData
  {
    public List<ReviewInfo> ProcessInstances { get; set; }
    public List<TaskInfo> MyTasks { get; set; }
    public List<TaskInfo> CoordinatorsTasks { get; set; }

    public IEnumerable<ReviewInfo> ActiveReviews
    {
      get
      {
        return ProcessInstances.Where(instance => !instance.Ended);
      }
    }

    public IEnumerable<ReviewInfo> FinishedReviews
    {
      get
      {
        return ProcessInstances.Where(instance => instance.Ended);
      }
    }
  }
}
