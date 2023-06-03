using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StomatoloskaPoliklinika.Models
{
  public class ReviewInfo
  {
    public string Pacijent { get; set; }
    public int UgovoreniSastanakId { get; set; }
    public DateTime DatumSastanka { get; set; }
    public string PID { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool Ended { get; set; }
    public bool CanApplyForReview { get; set; }
    public string Stomatolog { get; set; }

  }
}
