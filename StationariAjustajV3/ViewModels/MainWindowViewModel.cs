using Prism.Commands;
using Prism.Mvvm;
using S7.Net;
using StationariAjustajV3.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace StationariAjustajV3.ViewModels
{
    class MainWindowViewModel
    {


    }

    // Clasa scriere mesaje eroare in fisier 
    class LogMessegeRaportareAjustaj
    {
        public LogMessegeRaportareAjustaj()
        {
        }

        // Functie scriere mesaje de eroare . conectare / deconectare PLC-uri
        public static void WriteMessegeToLogFile(string mesajDeInregistrat = "")
        {
            StringBuilder csvContent = new StringBuilder();
            // Setare nume fisier nou la ora si data setata
            string numeFisier = string.Format(@"c:\Stationari Ajustaj/{0}/LogFile.txt",
               DateTime.Now.ToString("yyyy"));

            // Adaugare Cap tabel in continut fisier
            csvContent.AppendLine(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + " Mesaj: " + mesajDeInregistrat);
            File.AppendAllText(numeFisier, csvContent.ToString());
        }

        // Functie scriere in ce fisier dorim ce mesaje dorim
        public static void WriteMessegeToSpecifiedFile(string fisier = "nume fisier", string mesajDeInregistrat = "")
        {
            StringBuilder csvContent = new StringBuilder();
            // Setare nume fisier nou la ora si data setata
            string numeFisier = string.Format(@"c:\Stationari Ajustaj/{0}/{1}.txt",
               DateTime.Now.ToString("yyyy"), fisier);

            // Adaugare Cap tabel in continut fisier
            csvContent.AppendLine(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + " Mesaj: " + mesajDeInregistrat);
            File.AppendAllText(numeFisier, csvContent.ToString());
        }

        
        public static string GetLogFilePath (){
            return string.Format(@"c:\Stationari Ajustaj/{0}/LogFile.txt",
               DateTime.Now.ToString("yyyy"));
        }
        
    }


    // Clasa folosita pentru a afisa valori adaptate la grafica
    class PLCViewModel
    {
        public PLCService _plc;
        bool tempOprire1;
        DateTime startDefect;
        DateTime stopDefect;
        DateTime timpStartProgram;
        //PLCService rullatriceVechePlc = new PLCService(CpuType.S7300, "172.16.4.167", 0, 2, "Rullatrice Project Man",
        //    "A2.0", "A2.1", "A2.2", "A2.3", "A2.4", "A2.5", "A2.6");
        public PLCViewModel(PLCService plc)
        {
            _plc = plc;
            _plc.Connect();
            _plc.RefreshValues();
            tempOprire1 = false;
            timpStartProgram = DateTime.Now;
        }


        // Get Text afisare Functionare / Defect / Timp stationare (interval)
        public string GetTextFunctionare()
        {
            if (_plc.ConnectionState == ConnectionStates.Online)
            {
                // Daca masina mergea si apare stationare inregistram data inceput defect
                if (tempOprire1 == false && _plc.Oprire1)
                {
                    startDefect = DateTime.Now;
                    tempOprire1 = true;
                }
                // Daca masina stationa si incepe functionare inregistram data final defect
                if (tempOprire1 && _plc.Oprire1 == false)
                {
                    stopDefect = DateTime.Now;
                    tempOprire1 = false;
                }

                if (tempOprire1 && _plc.Oprire1)
                {
                    TimeSpan interval = DateTime.Now - startDefect;
                    if (interval.TotalHours >= 1)
                        return _plc.MotivStationare + " de " + interval.TotalHours.ToString("f1") + " ore";
                    //MessageBox.Show(interval.TotalMinutes.ToString());
                    //MessageBox.Show(startDefect.ToString("hh:mm:ss"));
                    return _plc.MotivStationare + " de " + interval.TotalMinutes.ToString("f0") + " min";
                }

                if (!tempOprire1 && !_plc.Oprire1)
                {
                    //MessageBox.Show(stopDefect.ToString("yyyy"));
                    TimeSpan interval;
                    if (stopDefect.ToString("yyyy") == "0001") // In cazul incare utilajul functioneaza, nu a avem stopDefect
                    {
                        interval = DateTime.Now - timpStartProgram;
                    }
                    else interval = DateTime.Now - stopDefect;
                    if (interval.TotalHours >= 1)
                        return "Functioneaza de " + interval.TotalHours.ToString("f1") + " ore";

                    return "Functioneaza de " + interval.TotalMinutes.ToString("f0") + " min";
                }

            }
            else
            {
                tempOprire1 = false;
                startDefect = DateTime.Now;
                stopDefect = DateTime.Now;
            }
            return "Plc " + _plc.NumePlc + " deconectat";
        }

        // Get Culoare Background Text Functionare 
        public Brush GetTextBackground()
        {
            if (_plc.ConnectionState == ConnectionStates.Online)
            {
                if (!_plc.Oprire1)
                    return Brushes.LawnGreen;
                return Brushes.Red;
            }
            var converter = new BrushConverter();
            var brush = (Brush)converter.ConvertFromString("#FFE7E5E2");
            return brush;
        }

        // Get Culoare Background Text Functionare 
        public Brush GetGridBackground()
        {
            if (_plc.Oprire1 || _plc.ConnectionState != ConnectionStates.Online)
                return Brushes.Red;

            var converter = new BrushConverter();
            var brush = (Brush)converter.ConvertFromString("#FFE7E5E2");
            return brush;
        }

        // Get Text Randament tinta
        public string GetTinta(string valoareTintaText) { return "Tinta: " + valoareTintaText + "%"; }

        // Get Text stare conexiune
        public string GetStareConexiune()
        {
            if (_plc.ConnectionState == ConnectionStates.Online)
                return "Online";
            return "Offline";
        }
        // Get ScanTime Conexiune
        public string GetScanTimeText()
        {
            return _plc.ScanTime.TotalMilliseconds.ToString("f0") + " ms";
        }
    }


    class CreareListaDefecte
    {
        PLCViewModel _plc;
        public List<Defect> listaDefecte;
        bool BreakDownInProgress_temp;

        public CreareListaDefecte(PLCViewModel plc)
        {
            _plc = plc;
            listaDefecte = new List<Defect>();
            BreakDownInProgress_temp = false;
        }


        // Functie adaugare defect in lista defecte utilaj Presa Valdora
        public void AdaugareDefectListaDefecte()
        {

            // Initializare semnale (prima citire)
            if (_plc.GetStareConexiune() == "Online")
            {
                // Cand apare stationare se inregistreaza
                if (_plc._plc.Oprire1)
                {
                    if ((listaDefecte.Count > 0 && listaDefecte.Last().DefectFinalizat) // Daca are ultimul defect finalizat se adauga defect
                        || listaDefecte.Count == 0) // Daca lista este goala, se adauga primul defect
                    {
                        listaDefecte.Add(new Defect
                        {
                            NumeUtilaj = _plc._plc.NumePlc,
                            TimpStartDefect = DateTime.Now,
                            IntervalStationare = TimeSpan.Parse("00:00")
                        });
                        // Console.WriteLine("A inceput defect");
                    }
                }

                // Scriere motiv stationare cand operatorul apasa pe buton
                if (listaDefecte.Count > 0 && _plc._plc.Oprire1)
                {
                    listaDefecte.Last().MotivStationare = _plc._plc.MotivStationare;
                }

                // Cand porneste masina se inregistraza datele stationarii
                if (!_plc._plc.Oprire1 && listaDefecte.Count > 0)
                {
                    if (!listaDefecte.Last().DefectFinalizat)
                    {
                        listaDefecte.Last().TimpStopDefect = DateTime.Now;
                        listaDefecte.Last().IntervalStationare = listaDefecte.Last().TimpStopDefect -
                            listaDefecte.Last().TimpStartDefect;
                        listaDefecte.Last().DefectFinalizat = true;
                        BreakDownInProgress_temp = false;
                    }
                }

                // Inregistrare al 2-lea BreakDown
                if (_plc._plc.Oprire2 && !BreakDownInProgress_temp)
                {
                    if (!listaDefecte.Last().DefectFinalizat)
                    {
                        listaDefecte.Last().TimpStopDefect = DateTime.Now;
                        listaDefecte.Last().IntervalStationare = listaDefecte.Last().TimpStopDefect -
                            listaDefecte.Last().TimpStartDefect;
                        listaDefecte.Last().DefectFinalizat = true;
                        BreakDownInProgress_temp = true;
                    }
                }
            }
            else // Stationare deoarece PLC e deconectat
            {
                if ((listaDefecte.Count > 0 && listaDefecte.Last().DefectFinalizat) // Daca are ultimul defect finalizat se adauga defect
                        || listaDefecte.Count == 0) // Daca lista este goala, se adauga primul defect
                {
                    listaDefecte.Add(new Defect
                    {
                        NumeUtilaj = _plc._plc.NumePlc,
                        TimpStartDefect = DateTime.Now,
                        IntervalStationare = TimeSpan.Parse("00:00")
                    });
                    // Console.WriteLine("A inceput defect");
                }
                // Scriere motiv stationare : Plc deconectat
                if (listaDefecte.Count > 0)
                {
                    listaDefecte.Last().MotivStationare = _plc._plc.MotivStationare;
                }
            }

        }

        // Functie golire liste defecte
        public void ClearListsOfDfects()
        {
            listaDefecte.Clear();
        }

    }

    class CreareRaportZilnic
    {
        public List<Defect> listaDefecteRullatriceVechePLC;
        public List<Defect> listaDefecteRullatriceLandgrafPLC;
        public List<Defect> listaDefecteElindPLC;
        public List<Defect> listaDefectePelatriceLandgrafPLC;
        public List<Defect> listaDefectePresaValdoraPLC;

        public string GetnumeFisierRullatriceVeche { get; set; }
        public string GetnumeFisierRullatriceLandgraf { get; set; }
        public string GetnumeFisierElind { get; set; }
        public string GetnumeFisierPellatrice { get; set; }
        public string GetnumeFisierPresaValdora { get; set; }



        public CreareRaportZilnic(List<Defect> _listalistaDefecteRullatriceVechePLC,
            List<Defect> _listaDefecteRullatriceLandgrafPLC, List<Defect> _listaDefecteElindPLC,
            List<Defect> _listaDefectePelatriceLandgrafPLC, List<Defect> _listaDefectePresaValdoraPLC)
        {
            listaDefecteRullatriceVechePLC = _listalistaDefecteRullatriceVechePLC;
            listaDefecteRullatriceLandgrafPLC = _listaDefecteRullatriceLandgrafPLC;
            listaDefecteElindPLC = _listaDefecteElindPLC;
            listaDefectePelatriceLandgrafPLC = _listaDefectePelatriceLandgrafPLC;
            listaDefectePresaValdoraPLC = _listaDefectePresaValdoraPLC;
        }

        // Clear lists of defects
        public void ClearListsOfDfects()
        {
            listaDefecteRullatriceVechePLC.Clear();
            listaDefecteRullatriceLandgrafPLC.Clear();
            listaDefecteElindPLC.Clear();
            listaDefectePelatriceLandgrafPLC.Clear();
            listaDefectePresaValdoraPLC.Clear();
        }

        // Functie raportare lista defecte utilaje | Raport total defecte utilaje | Raport per utilaj fisier anual
        public bool CreareRaportZilnicListaDefecteUtilaje(string oraRaport, string listaMailuiriDeTrimis,
            int minDelayReport, string oraInceputProgram)
        {
            // MessageBox.Show("Test");
            StringBuilder csvContent = new StringBuilder();
            StringBuilder csvContentRullatriceVechePLC = new StringBuilder();
            StringBuilder csvContentRullatriceLandgrafPLC = new StringBuilder();
            StringBuilder csvContentElindPLC = new StringBuilder();
            StringBuilder csvContentPelatriceLandgrafPLC = new StringBuilder();
            StringBuilder csvContentPresaValdoraPLC = new StringBuilder();

            // Setare nume fisier nou la ora si data setata
            string numeFisier = string.Format("Stationari_Ajustaj_{0}_{1}", DateTime.Now.ToString("yyyy-MM-dd"),
                DateTime.Now.ToString("HH.mm.ss"));
            // Setare nume pentru fiecare fisier cu lista de defecte per utilaj
            string numeFisierRullatriceVeche = string.Format(@"c:\Stationari Ajustaj/{0}/Lista_Defecte_Rullatrice_Veche_{0}.csv",
                DateTime.Now.ToString("yyyy"));
            string numeFisierRullatriceLandgraf = string.Format(@"c:\Stationari Ajustaj/{0}/Lista Defecte Rullatrice Landgraf {0}.csv",
                DateTime.Now.ToString("yyyy"));
            string numeFisierElind = string.Format(@"c:\Stationari Ajustaj/{0}/Lista Defecte Elind {0}.csv",
                DateTime.Now.ToString("yyyy"));
            string numeFisierPellatrice = string.Format(@"c:\Stationari Ajustaj/{0}/Lista Defecte Pellatrice Landgraf {0}.csv",
                DateTime.Now.ToString("yyyy"));
            string numeFisierPresaValdora = string.Format(@"c:\Stationari Ajustaj/{0}/Lista Defecte Presa Valdora {0}.csv",
                DateTime.Now.ToString("yyyy"));

            //Setare proprietati pentru calcul randament realizat
            GetnumeFisierRullatriceVeche = numeFisierRullatriceVeche;
            GetnumeFisierRullatriceLandgraf = numeFisierRullatriceLandgraf;
            GetnumeFisierElind = numeFisierElind;
            GetnumeFisierPellatrice = numeFisierPellatrice;
            GetnumeFisierPresaValdora = numeFisierPresaValdora;

            //Console.WriteLine(DateTime.Now.ToString("HH:mm:ss"));
            // Cand este ora stata se realizeaza raportul
            if (DateTime.Now.ToString("HH:mm:ss") == oraRaport) // setare ora pentru raport
            {
                // Adaugare Cap tabel in continut fisier
                csvContent.AppendLine("Denumire utilaj,Timp Start Defect,Timp Stop Defect,Motiv Stationare," +
                "Interval Stationare");
                // Setare cale fisier si denumire
                string csvPath = string.Format("{0}/{1}.csv", CreareFolderRaportare(), numeFisier);

                // Verificam daca exista fisier lista defecte pe utilaj, pentru a crea cap tabel
                if (!File.Exists(numeFisierRullatriceVeche))
                {
                    // Adaugare Cap tabel in continut fisier
                    csvContentRullatriceVechePLC.AppendLine("Denumire utilaj,Timp Start Defect,Timp Stop Defect,Motiv Stationare," +
                    "Interval Stationare," + oraInceputProgram);
                }
                // Verificam daca exista fisier lista defecte pe utilaj, pentru a crea cap tabel
                if (!File.Exists(numeFisierRullatriceLandgraf))
                {
                    // Adaugare Cap tabel in continut fisier
                    csvContentRullatriceLandgrafPLC.AppendLine("Denumire utilaj,Timp Start Defect,Timp Stop Defect,Motiv Stationare," +
                    "Interval Stationare," + oraInceputProgram);
                }
                // Verificam daca exista fisier lista defecte pe utilaj, pentru a crea cap tabel
                if (!File.Exists(numeFisierElind))
                {
                    // Adaugare Cap tabel in continut fisier
                    csvContentElindPLC.AppendLine("Denumire utilaj,Timp Start Defect,Timp Stop Defect,Motiv Stationare," +
                    "Interval Stationare," + oraInceputProgram);
                }
                // Verificam daca exista fisier lista defecte pe utilaj, pentru a crea cap tabel
                if (!File.Exists(numeFisierPellatrice))
                {
                    // Adaugare Cap tabel in continut fisier
                    csvContentPelatriceLandgrafPLC.AppendLine("Denumire utilaj,Timp Start Defect,Timp Stop Defect,Motiv Stationare," +
                    "Interval Stationare," + oraInceputProgram);
                }
                // Verificam daca exista fisier lista defecte pe utilaj, pentru a crea cap tabel
                if (!File.Exists(numeFisierPresaValdora))
                {
                    // Adaugare Cap tabel in continut fisier
                    csvContentPresaValdoraPLC.AppendLine("Denumire utilaj,Timp Start Defect,Timp Stop Defect,Motiv Stationare," +
                    "Interval Stationare," + oraInceputProgram);
                }

                // Console.WriteLine(csvPath);

                //Adaugare continut fisier din liste defecte
                //RullatriceVechePLC
                if (listaDefecteRullatriceVechePLC.Count > 0)
                {
                    foreach (Defect defect in listaDefecteRullatriceVechePLC)
                    {
                        if (!defect.DefectFinalizat)
                        {
                            defect.IntervalStationare = DateTime.Now - defect.TimpStartDefect;
                            defect.TimpStopDefect = DateTime.Now;
                        }
                        // Adaugam in fisier doar defecte care au depasit timpul delay setat
                        if (TimeSpan.Compare(defect.IntervalStationare, new TimeSpan(0, minDelayReport, 0)) == 1)
                        {
                            csvContent.AppendLine(string.Format("{0},{1},{2},{3},{4}", defect.NumeUtilaj,
                            defect.TimpStartDefect.ToString("dd/MM/yyyy HH:mm"), defect.TimpStopDefect.ToString("dd/MM/yyyy HH:mm"),
                            defect.MotivStationare, defect.IntervalStationare.ToString("hh\\:mm\\:ss")));
                            csvContentRullatriceVechePLC.AppendLine(string.Format("{0},{1},{2},{3},{4}", defect.NumeUtilaj,
                            defect.TimpStartDefect.ToString("dd/MM/yyyy HH:mm"), defect.TimpStopDefect.ToString("dd/MM/yyyy HH:mm"),
                            defect.MotivStationare, defect.IntervalStationare.ToString("hh\\:mm\\:ss")));
                        }
                    }
                }
                //RullatriceLandgrafPLC
                foreach (Defect defect in listaDefecteRullatriceLandgrafPLC)
                {
                    if (!defect.DefectFinalizat)
                    {
                        defect.IntervalStationare = DateTime.Now - defect.TimpStartDefect;
                        defect.TimpStopDefect = DateTime.Now;
                    }
                    if (TimeSpan.Compare(defect.IntervalStationare, new TimeSpan(0, minDelayReport, 0)) == 1)
                    {
                        csvContent.AppendLine(string.Format("{0},{1},{2},{3},{4}", defect.NumeUtilaj,
                        defect.TimpStartDefect.ToString("dd/MM/yyyy HH:mm"), defect.TimpStopDefect.ToString("dd/MM/yyyy HH:mm"),
                        defect.MotivStationare, defect.IntervalStationare.ToString("hh\\:mm\\:ss")));
                        csvContentRullatriceLandgrafPLC.AppendLine(string.Format("{0},{1},{2},{3},{4}", defect.NumeUtilaj,
                        defect.TimpStartDefect.ToString("dd/MM/yyyy HH:mm"), defect.TimpStopDefect.ToString("dd/MM/yyyy HH:mm"),
                        defect.MotivStationare, defect.IntervalStationare.ToString("hh\\:mm\\:ss")));
                    }
                }
                //ElindPLC
                foreach (Defect defect in listaDefecteElindPLC)
                {
                    if (!defect.DefectFinalizat)

                    {
                        defect.IntervalStationare = DateTime.Now - defect.TimpStartDefect;
                        defect.TimpStopDefect = DateTime.Now;
                    }
                    if (TimeSpan.Compare(defect.IntervalStationare, new TimeSpan(0, minDelayReport, 0)) == 1)
                    {
                        csvContent.AppendLine(string.Format("{0},{1},{2},{3},{4}", defect.NumeUtilaj,
                        defect.TimpStartDefect.ToString("dd/MM/yyyy HH:mm"), defect.TimpStopDefect.ToString("dd/MM/yyyy HH:mm"),
                        defect.MotivStationare, defect.IntervalStationare.ToString("hh\\:mm\\:ss")));
                        csvContentElindPLC.AppendLine(string.Format("{0},{1},{2},{3},{4}", defect.NumeUtilaj,
                        defect.TimpStartDefect.ToString("dd/MM/yyyy HH:mm"), defect.TimpStopDefect.ToString("dd/MM/yyyy HH:mm"),
                        defect.MotivStationare, defect.IntervalStationare.ToString("hh\\:mm\\:ss")));
                    }
                }
                // PelatriceLandgrafPLC
                foreach (Defect defect in listaDefectePelatriceLandgrafPLC)
                {
                    if (!defect.DefectFinalizat)
                    {
                        defect.IntervalStationare = DateTime.Now - defect.TimpStartDefect;
                        defect.TimpStopDefect = DateTime.Now;
                    }
                    if (TimeSpan.Compare(defect.IntervalStationare, new TimeSpan(0, minDelayReport, 0)) == 1)
                    {
                        csvContent.AppendLine(string.Format("{0},{1},{2},{3},{4}", defect.NumeUtilaj,
                        defect.TimpStartDefect.ToString("dd/MM/yyyy HH:mm"), defect.TimpStopDefect.ToString("dd/MM/yyyy HH:mm"),
                        defect.MotivStationare, defect.IntervalStationare.ToString("hh\\:mm\\:ss")));
                        csvContentPelatriceLandgrafPLC.AppendLine(string.Format("{0},{1},{2},{3},{4}", defect.NumeUtilaj,
                        defect.TimpStartDefect.ToString("dd/MM/yyyy HH:mm"), defect.TimpStopDefect.ToString("dd/MM/yyyy HH:mm"),
                        defect.MotivStationare, defect.IntervalStationare.ToString("hh\\:mm\\:ss")));
                    }
                }
                // PresaValdoraPLC
                foreach (Defect defect in listaDefectePresaValdoraPLC)
                {
                    if (!defect.DefectFinalizat)
                    {
                        defect.IntervalStationare = DateTime.Now - defect.TimpStartDefect;
                        defect.TimpStopDefect = DateTime.Now;
                    }
                    if (TimeSpan.Compare(defect.IntervalStationare, new TimeSpan(0, minDelayReport, 0)) == 1)
                    {
                        csvContent.AppendLine(string.Format("{0},{1},{2},{3},{4}", defect.NumeUtilaj,
                        defect.TimpStartDefect.ToString("dd/MM/yyyy HH:mm"), defect.TimpStopDefect.ToString("dd/MM/yyyy HH:mm"),
                        defect.MotivStationare, defect.IntervalStationare.ToString("hh\\:mm\\:ss")));
                        csvContentPresaValdoraPLC.AppendLine(string.Format("{0},{1},{2},{3},{4}", defect.NumeUtilaj,
                        defect.TimpStartDefect.ToString("dd/MM/yyyy HH:mm"), defect.TimpStopDefect.ToString("dd/MM/yyyy HH:mm"),
                        defect.MotivStationare, defect.IntervalStationare.ToString("hh\\:mm\\:ss")));
                    }
                }

                // Console.WriteLine("S-a creat primul raport la ora: {0}", DateTime.Now.ToString("HH:mm:ss"));
                //Console.WriteLine(csvContent);
                //Console.ReadKey();
                File.AppendAllText(csvPath, csvContent.ToString());

                // Adauga text in fisiere liste defecte anual
                File.AppendAllText(numeFisierRullatriceVeche, csvContentRullatriceVechePLC.ToString());
                File.AppendAllText(numeFisierRullatriceLandgraf, csvContentRullatriceLandgrafPLC.ToString());
                File.AppendAllText(numeFisierElind, csvContentElindPLC.ToString());
                File.AppendAllText(numeFisierPellatrice, csvContentPelatriceLandgrafPLC.ToString());
                File.AppendAllText(numeFisierPresaValdora, csvContentPresaValdoraPLC.ToString());

                //Functie trimitere mail (string adreseMailDeTrimis, string filePathDeTrimis, string subiect)
                TrimitereRaportMail(listaMailuiriDeTrimis, csvPath, numeFisier);
                // , e.deganello@beltrame-group.com, i.sutu@beltrame-group.com

                // Stergere lista defecte pentru a incepe a 2-a zi
                ClearListsOfDfects(); // Clear list of defects

                // Delay 1 secunda pentru a evita sa faca mai multe rapoarte in aceeasi secunda
                System.Threading.Thread.Sleep(1000);

                return true;
            }
            return false;
        }

        public string CreareFolderRaportare()
        {
            string path = string.Format(@"c:\Stationari Ajustaj/{0}/{1}", DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MMMM"));
            try
            {
                // Determine whether the directory exists.
                if (Directory.Exists(path))
                {
                    // Console.WriteLine("That path exists already.");
                    return path;
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(path);
                // Console.WriteLine("The directory was created successfully at {0}.", Directory.GetCreationTime(path));

                // Delete the directory.
                //di.Delete();
                //Console.WriteLine("The directory was deleted successfully.");
            }
            catch (Exception ex)
            {
                LogMessegeRaportareAjustaj.WriteMessegeToLogFile(ex.Message.ToString());
                //MessageBox.Show(ex.Message);
                // Console.WriteLine("The process failed: {0}", e.ToString());
                //throw ex;
            }

            return path;
        }

        // Functie trimitere mail
        public static void TrimitereRaportMail(string adreseMailDeTrimis, string filePathDeTrimis, string subiect)
        {
            try
            {
                // "don.rap.ajustaj@gmail.com", "v.moisei@beltrame-group.com, vladmoisei@yahoo.com"
                // Mail(emailFrom , emailTo)
                MailMessage mail = new MailMessage("don.rap.ajustaj@gmail.com", adreseMailDeTrimis);

                //mail.From = new MailAddress("don.rap.ajustaj@gmail.com");
                mail.Subject = "Raport stationari utilaje ajustaj";
                string Body = string.Format("Buna dimineata. <br>Atasat gasiti raport stationari utilaje ajustaj " +
                    "<br>O zi buna.");
                mail.Body = Body;
                mail.IsBodyHtml = true;
                using (Attachment attachment = new Attachment(filePathDeTrimis))
                {
                    mail.Attachments.Add(attachment);

                    SmtpClient smtp = new SmtpClient();
                    smtp.Host = "smtp.gmail.com"; //Or Your SMTP Server Address
                    smtp.Credentials = new System.Net.NetworkCredential("don.rap.ajustaj@gmail.com", "Beltrame.1");
                    smtp.Port = 587;
                    smtp.EnableSsl = true;
                    smtp.Send(mail);

                    mail = null;
                    smtp = null;
                }

                // Console.WriteLine("Mail Sent succesfully");
            }
            catch (Exception ex)
            {
                LogMessegeRaportareAjustaj.WriteMessegeToLogFile(ex.Message.ToString());
                //MessageBox.Show(ex.Message);
                // Console.WriteLine(ex.ToString());
                //throw ex;
            }
        }

        // Get Text Randament actual
        public string GetRandamentActual(List<Defect> listaDefecte, DateTime startProgramTimp)
        {
            //// Copiez lista defecte pentru a modifica stare ultim defect lista fara a afecta lista originala
            //List<Defect> listaDefecteNoua = new List<Defect>();
            //foreach (Defect defect in listaDefecte)
            //    listaDefecteNoua.Add(new Defect(defect));

            //// Finalizez ultimul defect pentru a putea calcula randamentul
            //if (!listaDefecteNoua.Last().DefectFinalizat)
            //{
            //    listaDefecteNoua.Last().DefectFinalizat = true;
            //    listaDefecteNoua.Last().TimpStopDefect = DateTime.Now;
            //    listaDefecteNoua.Last().IntervalStationare = listaDefecteNoua.Last().TimpStopDefect - listaDefecteNoua.Last().TimpStartDefect;
            //}

            // Declar variabile calcul
            TimeSpan interval = DateTime.Now - startProgramTimp;
            double maxHoursFunctionare = 0.0; //24
            double totalHoursStationare = 0.0;
            double totalHoursFunctionare = 0.0;
            double totalHoursOprireProgramata = 0.0;
            double randamentActual = 0.0;

            // Daca programul ruleaza de mai putin de 24 ore, nr. total ore functionare este de cand ruleaza programul pana la data curenta
            //if (interval.TotalHours < 24)
            //maxHoursFunctionare = Math.Round(interval.TotalHours, 2);
            maxHoursFunctionare = interval.TotalHours;

            if (listaDefecte.Count > 0)
            {
                foreach (Defect defect in listaDefecte)
                {
                    if (!defect.DefectFinalizat) // Notam interval stationare daca ultimul defect nu este finalizat
                    {
                        defect.TimpStopDefect = DateTime.Now;
                        defect.IntervalStationare = defect.TimpStopDefect - defect.TimpStartDefect;
                    }
                    if (defect.MotivStationare == "Oprire programata")
                        totalHoursOprireProgramata += defect.IntervalStationare.TotalHours;
                    else totalHoursStationare += defect.IntervalStationare.TotalHours;
                }

                maxHoursFunctionare -= totalHoursOprireProgramata;
                //maxHoursFunctionare = Math.Round(maxHoursFunctionare, 2);
                totalHoursFunctionare = maxHoursFunctionare - totalHoursStationare;
                //totalHoursFunctionare = Math.Round(totalHoursFunctionare);


                randamentActual = (totalHoursFunctionare / maxHoursFunctionare) * 100;
                if (totalHoursFunctionare.ToString("f2") == "0.00") // Sa returneze in cap daca nu functioneaza
                    randamentActual = 0;

                //MessageBox.Show(listaDefecte.Last().NumeUtilaj +
                //    "\nTotal ore functionare: " + totalHoursFunctionare.ToString("f2") +
                //    "\nTotal ore stationare:  " + totalHoursStationare.ToString("f2") +
                //    "\nMax ore functionare: " + maxHoursFunctionare.ToString("f2")
                //    + "\nRandament: " + randamentActual.ToString("f2") +
                //    "\nOprire programata: " + totalHoursOprireProgramata.ToString("f2")
                //    + "\nData inceput: " + startProgramTimp +
                //    "\nInterval timp [ore]: " + interval.TotalHours.ToString("f2"));

            }
            else return "R. Actual: 100%";

            return "R. Actual: " + randamentActual.ToString("f0") + "%";

        }

        // Functie Get Lista defecte din fisier
        public Tuple<List<Defect>, DateTime> GetListaDefecteDinFisier(string caleFisier)
        {
            if (File.Exists(caleFisier)) // Daca exista fisier 
            {
                DateTime startProgramTimp = new DateTime();
                List<Defect> listaDefecte = new List<Defect>();
                StreamReader sr = new StreamReader(caleFisier);
                string line;
                string[] row = new string[5];
                int i = 0;
                CultureInfo provider = CultureInfo.InvariantCulture;
                CultureInfo culture = CultureInfo.CurrentCulture;
                // Citim din fisier si cream lista defecte
                while ((line = sr.ReadLine()) != null)
                {
                    row = line.Split(',');

                    if (i == 0)
                    {
                        // Verific data inceput program daca e mai mica data la care a inceput prim defect pentru a calcula
                        // corect randament realizat
                        startProgramTimp = DateTime.ParseExact(row[5], "dd/MM/yyyy HH:mm", provider);
                        //if (DateTime.Compare(startProgramTimpOriginal, startProgramTimp) < 0)
                        //    startProgramTimp = startProgramTimpOriginal;
                        //MessageBox.Show("Data Start ptogram: " + startProgramTimpOriginal.ToString("hh:mm:ss") +
                        //    "\nData utilizata in progr: " + startProgramTimp.ToString("hh:mm:ss"));
                        //MessageBox.Show("Data Start ptogram: " + startProgramTimp.ToString("hh:mm:ss"));
                    } // Salvam data prim defect
                    if (i > 0) // Sarim primul rand (cap tabel)
                    {

                        listaDefecte.Add(new Defect
                        {
                            NumeUtilaj = row[0],
                            TimpStartDefect = DateTime.ParseExact(row[1], "dd/MM/yyyy HH:mm", provider),
                            TimpStopDefect = DateTime.ParseExact(row[2], "dd/MM/yyyy HH:mm", provider),
                            MotivStationare = row[3],
                            IntervalStationare = TimeSpan.ParseExact(row[4], "hh\\:mm\\:ss", culture)
                        });

                    }

                    i++;
                }
                return Tuple.Create(listaDefecte, startProgramTimp);
            }
            return Tuple.Create(new List<Defect>(), DateTime.Now);
        }

        // Get Text Randament realizat de la inceputul anului
        public Tuple<string, KeyValuePair<string, int>[]> GetRandamentRealizat(string caleFisier)
        {
            string randamentTextDeAfisat = "R. Realizat: -";

            var tupleListaDataStart = GetListaDefecteDinFisier(caleFisier);
            List<Defect> listaDefecte = tupleListaDataStart.Item1;
            DateTime startProgramTimp = tupleListaDataStart.Item2;

            // Finalizam fiecare defect din lista | ne trebuie la calcul randament actual
            foreach (Defect defect in listaDefecte)
                defect.DefectFinalizat = true;


            // DE PROBA SA VEDEM DACA A FACUT LISTA BINE SI TIMP START ESTE CORECT
            //TimeSpan interval = DateTime.Now - startProgramTimp;
            //foreach (Defect defect in listaDefecte)
            //{
            //    MessageBox.Show(string.Format("Interval timp: {6}" +
            //        "\nData Start ptogram: {5}\n{0}\n{1}\n{2}\n{3}\n{4}", defect.NumeUtilaj,
            //        defect.TimpStartDefect.ToString("dd/MM/yyyy HH:mm"), defect.TimpStopDefect.ToString("dd/MM/yyyy HH:mm"),
            //        defect.MotivStationare, defect.IntervalStationare.ToString("hh\\:mm\\:ss"),
            //        startProgramTimp.ToString("dd/MM/yyyy HH:mm"), interval.TotalHours.ToString()));
            //}
            //MessageBox.Show(GetRandamentActual(listaDefecte, startProgramTimp));

            randamentTextDeAfisat = GetRandamentActual(listaDefecte, startProgramTimp).Replace("R. Actual:", "R. Realizat");

            //return randamentTextDeAfisat;

            // REturnam string randament Realizat cat si grafic stationari
            return Tuple.Create(randamentTextDeAfisat, GetItemSourceGraficStationari(listaDefecte)); 
        }

        // Get Item Source Grafic Stationari TO DO
        public KeyValuePair<string, int>[] GetItemSourceGraficStationari(List<Defect> listaDefecte)
        {
            double timpTotalDefectMecanic = 0;
            double timpTotalDefectElectric = 0;
            double timpTotalOprireProgramata = 0;
            double timpTotalOprireTehnologica = 0;
            double timpTotalLipsaMaterialPod = 0;
            double timpTotalNuS_aApasatCauza = 0;
            double timpTotalStationari = 0;

            // Calculam timp in ore pentru fiecare motiv stationare si total stationare in ore
            foreach (Defect defect in listaDefecte)
            {
                timpTotalStationari += defect.IntervalStationare.TotalHours;
                switch (defect.MotivStationare)
                {
                    case "Defect mecanic":
                        timpTotalDefectMecanic += defect.IntervalStationare.TotalHours;
                        break;
                    case "Defect electric":
                        timpTotalDefectElectric += defect.IntervalStationare.TotalHours;
                        break;
                    case "Oprire programata":
                        timpTotalOprireProgramata += defect.IntervalStationare.TotalHours;
                        break;
                    case "Oprire tehnologica":
                        timpTotalOprireTehnologica += defect.IntervalStationare.TotalHours;
                        break;
                    case "Lipsa pod rulant / Lipsa material":
                        timpTotalLipsaMaterialPod += defect.IntervalStationare.TotalHours;
                        break;
                    case "Nu s-a apasat cauza":
                    case "Plc deconectat":
                        timpTotalNuS_aApasatCauza += defect.IntervalStationare.TotalHours;
                        break;
                    default:
                        break;
                }
            }

            int procentDefectMecanic = (int)Math.Round(timpTotalDefectMecanic / timpTotalStationari * 100);
            int procentDefectElectric = (int)Math.Round(timpTotalDefectElectric / timpTotalStationari * 100);
            int procentOprireProgramata = (int)Math.Round(timpTotalOprireProgramata / timpTotalStationari * 100);
            int procentOprireTehnologica = (int)Math.Round(timpTotalOprireTehnologica / timpTotalStationari * 100);
            int procentLipsaMaterialPod = (int)Math.Round(timpTotalLipsaMaterialPod / timpTotalStationari * 100);
            int procentNuS_aApasatCauza = (int)Math.Round(timpTotalNuS_aApasatCauza / timpTotalStationari * 100);

            // Daca lista defecte este 0 punem toate procentele 0
            if (listaDefecte.Count == 0)
            {
                procentDefectMecanic = 0;
                procentDefectElectric = 0;
                procentOprireProgramata = 0;
                procentOprireTehnologica = 0;
                procentLipsaMaterialPod = 0;
                procentNuS_aApasatCauza = 0;
            }

            //    string nnume = "Utilaj: -";
            //if (listaDefecte.Count > 0 ) nnume = "Utilaj: " + listaDefecte.Last().NumeUtilaj;
            //MessageBox.Show(nnume +
            //    "\nDefect mecanic = " + procentDefectMecanic.ToString() +
            //    "\nDefect electric = " + procentDefectElectric.ToString() +
            //    "\nOprire programata = " + procentOprireProgramata.ToString() +
            //    "\nOprire tehnologica = " + procentOprireTehnologica.ToString() +
            //    "\n Lipsa material, pod = " + procentLipsaMaterialPod.ToString() +
            //    "\nNu s-a apasat cauza = " + procentNuS_aApasatCauza.ToString());

            return new KeyValuePair<string, int>[]
               {
                    new KeyValuePair<string, int>("Defect mecanic", procentDefectMecanic),
                    new KeyValuePair<string, int>("Defect electric", procentDefectElectric),
                    new KeyValuePair<string, int>("Oprire programata", procentOprireProgramata),
                    new KeyValuePair<string, int>("Oprire tehnologica", procentOprireTehnologica),
                    new KeyValuePair<string, int>("Lipsa material, pod", procentLipsaMaterialPod),
                    new KeyValuePair<string, int>("Nu s-a apasat cauza", procentNuS_aApasatCauza)
               };


            //return new KeyValuePair<string, int>[2];
        }

    }

}
