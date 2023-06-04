using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StomatoloskaPoliklinika.Models
{
  public class DashboardData
  {
    public List<SastanakInfo> ProcessInstances { get; set; }
    public List<TaskInfo> MyTasks { get; set; }
    public List<TaskInfo> CoordinatorsTasks { get; set; }

    public IEnumerable<SastanakInfo> NeugovoreniSastanci
    {
      get
      {
        return ProcessInstances.Where(instance => !instance.Ended);
      }
    }

    public IEnumerable<SastanakInfo> UgovoreniSastanci
    {
      get
      {
        return ProcessInstances.Where(instance => instance.Ended);
      }
    }
  }
}
