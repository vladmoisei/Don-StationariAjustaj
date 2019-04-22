using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StationariAjustajV3.Models
{
    // Clasa defect
    class Defect
    {
        public Defect()
        {
            NumeUtilaj = "";
            MotivStationare = "";
            DefectFinalizat = false;
        }

        public Defect(Defect newDefect)
        {
            NumeUtilaj = newDefect.NumeUtilaj;
            TimpStartDefect = newDefect.TimpStartDefect;
            TimpStopDefect = newDefect.TimpStopDefect;
            IntervalStationare = newDefect.IntervalStationare;
            MotivStationare = newDefect.MotivStationare;
            DefectFinalizat = newDefect.DefectFinalizat;
        }
        public string NumeUtilaj { get; set; }
        public DateTime TimpStartDefect { get; set; }
        public DateTime TimpStopDefect { get; set; }
        public TimeSpan IntervalStationare { get; set; }
        public string MotivStationare { get; set; }
        public bool DefectFinalizat { get; set; }
    }
}
