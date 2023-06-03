using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StomatoloskaPoliklinika.Models
{
  public class TaskInfo
  {
    public string TID { get; set; }
    public string TaskKey { get; set; }
    public string TaskName { get; set; }
    public string Pacijent { get; set; }
    public int UgovoreniSastanakId { get; set; }
    public DateTime DatumSastanka { get; set; }
    public string PID { get; set; }
    public DateTime StartTime { get; set; }
    public string Stomatolog { get; set; }

  }
}
