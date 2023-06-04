using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StomatoloskaPoliklinika.Models
{
  public class SastanakInfo
  {
    public string Pacijent { get; set; }
    public int UgovoreniSastanakID { get; set; }
    public string PID { get; set; }
    public DateTime DatumVrijeme { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? StartTime { get; set; }
    public bool Ended { get; set; }
    public bool PotrebneDorade { get; set; }
    public string Stomatolog { get; set; }

  }
}
