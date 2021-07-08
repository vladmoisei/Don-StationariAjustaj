using S7.Net;
using StationariAjustajV3.Models;
using StationariAjustajV3.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StationariAjustajV3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        bool executieProgram = true;
        string oraRaport = "06:00:00";
        int delayDefectRaport = 1;
        string adreseMailDeTrimis = "v.moisei@beltrame-group.com, e.deganello@beltrame-group.com, " +
            "i.sutu@beltrame-group.com, i.micu@beltrame-group.com, a.zvincu@beltrame-group.com, f.tudor@beltrame-group.com";

        public MainWindow()
        {
            InitializeComponent();

            Thread t = new Thread(new ThreadStart(ProgramBackground));
            t.Name = "PlcCommunication";
            t.Start();

            Seteaza_Data.Click += Seteaza_Data_Click;
            Afiseaza_Data.Click += Afiseaza_Data_Click;
            Start_Comm.Click += Start_Comm_Click;
            Stop_Comm.Click += Stop_Comm_click;
        }

        void ProgramBackground()
        {
            try
            {
                DateTime timpStartProgram = DateTime.Now;
                DateTime timpStartRandamentActual = DateTime.Now;

                // Initializare variabile PLC-uri
                PLCViewModel rullatriceVechePlc = new PLCViewModel(new PLCService(CpuType.S7300, "172.16.4.167", 0, 2, "Rullatrice Project Man",
                    "A2.0", "A2.1", "A2.2", "A2.3", "A2.4", "A2.5", "A2.6"));
                PLCViewModel rullatriceLandgrafPLC = new PLCViewModel(new PLCService(CpuType.S7400, "172.16.4.72", 0, 2, "Rullatrice Landgraf",
                "A35.4", "A35.5", "A35.6", "A35.7", "A36.0", "A36.6", "A36.7"));
                PLCViewModel elindPLC = new PLCViewModel(new PLCService(CpuType.S7300, "172.16.4.5", 0, 2, "Elind",
                "A47.6", "A47.0", "A47.1", "A47.2", "A47.3", "A47.4", "A47.5"));
                PLCViewModel pelatriceLandgrafPLC = new PLCViewModel(new PLCService(CpuType.S7300, "192.168.1.1", 0, 2, "Pellatrice Landgraf",
                "A22.6", "A23.0", "A23.1", "A23.4", "A23.5", "A23.6", "A23.7"));
                PLCViewModel presaValdoraPLC = new PLCViewModel(new PLCService(CpuType.S71500, "172.16.4.195", 0, 2, "Presa Valdora",
                "A3.0", "A3.1", "A3.2", "A3.3", "A3.4", "A3.5", "A3.6"));

                // Creare lista defecte pentru PLC-uri
                CreareListaDefecte CLDrullatriceVechePlc = new CreareListaDefecte(rullatriceVechePlc);
                CreareListaDefecte CLDrullatriceLandgrafPLC = new CreareListaDefecte(rullatriceLandgrafPLC);
                CreareListaDefecte CLDelindPLC = new CreareListaDefecte(elindPLC);
                CreareListaDefecte CLDpelatriceLandgrafPLC = new CreareListaDefecte(pelatriceLandgrafPLC);
                CreareListaDefecte CLDpresaValdoraPLC = new CreareListaDefecte(presaValdoraPLC);

                CreareRaportZilnic raportZilnic = new CreareRaportZilnic(CLDrullatriceVechePlc.listaDefecte,
                    CLDrullatriceLandgrafPLC.listaDefecte, CLDelindPLC.listaDefecte,
                    CLDpelatriceLandgrafPLC.listaDefecte, CLDpresaValdoraPLC.listaDefecte);

                try
                {
                    while (executieProgram)
                    {
                        // Refresh citire variabile PLC
                        rullatriceVechePlc._plc.RefreshValues();
                        rullatriceLandgrafPLC._plc.RefreshValues();
                        elindPLC._plc.RefreshValues();
                        pelatriceLandgrafPLC._plc.RefreshValues();
                        presaValdoraPLC._plc.RefreshValues();

                        // Adaugare defect lista defecte
                        CLDrullatriceVechePlc.AdaugareDefectListaDefecte();
                        CLDrullatriceLandgrafPLC.AdaugareDefectListaDefecte();
                        CLDelindPLC.AdaugareDefectListaDefecte();
                        CLDpelatriceLandgrafPLC.AdaugareDefectListaDefecte();
                        CLDpresaValdoraPLC.AdaugareDefectListaDefecte();

                        // Resetare ora start randament actual
                        if (DateTime.Now.ToString("HH:mm:ss") == oraRaport) // setare ora pentru raport
                        {
                            timpStartRandamentActual = DateTime.Now;
                            //MessageBox.Show("S-a resetat ora randament actual: " + timpStartRandamentActual.ToString("HH:mm:ss"));
                        }
                        // Creare raport la ora setata
                        // v.moisei@beltrame-group.com, e.deganello@beltrame-group.com, i.sutu@beltrame-group.com, i.micu@beltrame-group.com, a.zvincu@beltrame-group.com, f.tudor@beltrame-group.com
                        raportZilnic.CreareRaportZilnicListaDefecteUtilaje(oraRaport, adreseMailDeTrimis, 
                                delayDefectRaport, timpStartProgram.ToString("dd/MM/yyyy HH:mm"));

                        // Calcul si afisare randament realizat la ora raport
                        if (DateTime.Now.AddSeconds(-30).ToString("HH:mm:ss") == oraRaport)
                        {
                            // Afisare randament realizat
                            this.Dispatcher.Invoke(() =>
                            {
                                rullatriceVecheRandamentRealizat.Text = raportZilnic.GetRandamentRealizat(raportZilnic.GetnumeFisierRullatriceVeche).Item1;
                            });
                            // Afisare randament realizat
                            this.Dispatcher.Invoke(() =>
                            {
                                rullatriceLandgrafRandamentRealizat.Text = raportZilnic.GetRandamentRealizat(raportZilnic.GetnumeFisierRullatriceLandgraf).Item1;
                            });
                            // Afisare randament realizat
                            this.Dispatcher.Invoke(() =>
                            {
                                elindRandamentRealizat.Text = raportZilnic.GetRandamentRealizat(raportZilnic.GetnumeFisierElind).Item1;
                            });
                            // Afisare randament realizat
                            this.Dispatcher.Invoke(() =>
                            {
                                pellatriceLandgrafRandamentRealizat.Text = raportZilnic.GetRandamentRealizat(raportZilnic.GetnumeFisierPellatrice).Item1;
                            });
                            // Afisare randament realizat
                            this.Dispatcher.Invoke(() =>
                            {
                                presaValdoraRandamentRealizat.Text = raportZilnic.GetRandamentRealizat(raportZilnic.GetnumeFisierPresaValdora).Item1;
                            });

                            // Afisare grafic stationari
                            this.Dispatcher.Invoke(() =>
                            {
                                ((ColumnSeries)rullatriceVecheChart.Series[0]).ItemsSource = raportZilnic.GetRandamentRealizat(raportZilnic.GetnumeFisierRullatriceVeche).Item2;
                            });
                            // Afisare grafic stationari
                            this.Dispatcher.Invoke(() =>
                            {
                                ((ColumnSeries)rullatriceLandgrafChart.Series[0]).ItemsSource = raportZilnic.GetRandamentRealizat(raportZilnic.GetnumeFisierRullatriceLandgraf).Item2;
                            });
                            // Afisare grafic stationari
                            this.Dispatcher.Invoke(() =>
                            {
                                ((ColumnSeries)elindChart.Series[0]).ItemsSource = raportZilnic.GetRandamentRealizat(raportZilnic.GetnumeFisierElind).Item2;
                            });
                            // Afisare grafic stationari
                            this.Dispatcher.Invoke(() =>
                            {
                                ((ColumnSeries)pellatriceChart.Series[0]).ItemsSource = raportZilnic.GetRandamentRealizat(raportZilnic.GetnumeFisierPellatrice).Item2;
                            });
                            // Afisare grafic stationari
                            this.Dispatcher.Invoke(() =>
                            {
                                ((ColumnSeries)presaValdoraChart.Series[0]).ItemsSource = raportZilnic.GetRandamentRealizat(raportZilnic.GetnumeFisierPresaValdora).Item2;
                            });
                        }

                        this.Dispatcher.Invoke(() =>
                        {
                            DataDeAfisat.Text = DateTime.Now.ToString("hh:mm:ss");
                        });
                        /*
                         * Grafica Rullatrice Veche 
                         */
                        // Afisare text functionare rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            rullatriceVecheText.Text = rullatriceVechePlc.GetTextFunctionare();
                        });

                        // Afisare Background text functionare rullatrice veche
                        this.Dispatcher.Invoke(() =>
                            {
                                rullatriceVecheTextBackground.Background = rullatriceVechePlc.GetTextBackground();
                            });

                        // Afisare Background grid rullatrice veche                      
                        this.Dispatcher.Invoke(() =>
                            {
                                rullatriceVecheGridBackground.Background = rullatriceVechePlc.GetGridBackground();
                            });

                        // Afisare stare conexiune rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            rullatriceVecheStareConexiune.Text = rullatriceVechePlc.GetStareConexiune();
                        });

                        // Afisare scan time rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            rullatriceVecheScanTime.Text = rullatriceVechePlc.GetScanTimeText();
                        });

                        // Afisare randament actual
                        this.Dispatcher.Invoke(() =>
                        {
                            rullatriceVecheRandamanetActual.Text = raportZilnic.GetRandamentActual(raportZilnic.listaDefecteRullatriceVechePLC, timpStartRandamentActual);
                        });

                        


                        /*
                         * Grafica Rullatrice Landgraf
                         */
                        // Afisare text functionare rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            rullatriceLandgrafText.Text = rullatriceLandgrafPLC.GetTextFunctionare();
                        });

                        // Afisare Background text functionare rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            rullatriceLandgrafTextBackgound.Background = rullatriceLandgrafPLC.GetTextBackground();
                        });

                        // Afisare Background grid rullatrice veche                      
                        this.Dispatcher.Invoke(() =>
                        {
                            rullatriceLandgrafGridBackground.Background = rullatriceLandgrafPLC.GetGridBackground();
                        });

                        // Afisare stare conexiune rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            rullatriceLandgrafStareConexiune.Text = rullatriceLandgrafPLC.GetStareConexiune();
                        });

                        // Afisare scan time rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            rullatriceLandgrafScanTime.Text = rullatriceLandgrafPLC.GetScanTimeText();
                        });

                        // Afisare randament actual
                        this.Dispatcher.Invoke(() =>
                        {
                            rullatriceLandgrafRandamentActual.Text = raportZilnic.GetRandamentActual(raportZilnic.listaDefecteRullatriceLandgrafPLC, timpStartRandamentActual);
                        });

                        /*
                         * Grafica Elind
                         */
                        // Afisare text functionare rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            elindText.Text = elindPLC.GetTextFunctionare();
                        });

                        // Afisare Background text functionare rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            elindTextBackground.Background = elindPLC.GetTextBackground();
                        });

                        // Afisare Background grid rullatrice veche                      
                        this.Dispatcher.Invoke(() =>
                        {
                            elindGridBackgound.Background = elindPLC.GetGridBackground();
                        });

                        // Afisare stare conexiune rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            elindStareConexiune.Text = elindPLC.GetStareConexiune();
                        });

                        // Afisare scan time rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            elindScanTime.Text = elindPLC.GetScanTimeText();
                        });

                        // Afisare randament actual
                        this.Dispatcher.Invoke(() =>
                        {
                            elindRandamentActual.Text = raportZilnic.GetRandamentActual(raportZilnic.listaDefecteElindPLC, timpStartRandamentActual);
                        });


                        /*
                         * Grafica Pellatrice Landgraf
                         */
                        // Afisare text functionare rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            pellatriceLandgrafText.Text = pelatriceLandgrafPLC.GetTextFunctionare();
                        });

                        // Afisare Background text functionare rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            pellatriceLandgrafTextBackground.Background = pelatriceLandgrafPLC.GetTextBackground();
                        });

                        // Afisare Background grid rullatrice veche                      
                        this.Dispatcher.Invoke(() =>
                        {
                            pellatriceLandgrafGridBackground.Background = pelatriceLandgrafPLC.GetGridBackground();
                        });

                        // Afisare stare conexiune rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            pellatriceLandgrafStareConexiune.Text = pelatriceLandgrafPLC.GetStareConexiune();
                        });

                        // Afisare scan time rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            pellatriceLandgrafScanTime.Text = pelatriceLandgrafPLC.GetScanTimeText();
                        });

                        // Afisare randament actual
                        this.Dispatcher.Invoke(() =>
                        {
                            pellatriceLandgrafRandamentActual.Text = raportZilnic.GetRandamentActual(raportZilnic.listaDefectePelatriceLandgrafPLC, timpStartRandamentActual);
                        });

                        /*
                         * Grafica Presa Valdora
                         */
                        // Afisare text functionare rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            presaValdoraText.Text = presaValdoraPLC.GetTextFunctionare();
                        });

                        // Afisare Background text functionare rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            presaValdoraTextBackground.Background = presaValdoraPLC.GetTextBackground();
                        });

                        // Afisare Background grid rullatrice veche                      
                        this.Dispatcher.Invoke(() =>
                        {
                            presaValdoraGridBackground.Background = presaValdoraPLC.GetGridBackground();
                        });

                        // Afisare stare conexiune rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            presaValdoraStareConexiune.Text = presaValdoraPLC.GetStareConexiune();
                        });

                        // Afisare scan time rullatrice veche
                        this.Dispatcher.Invoke(() =>
                        {
                            presaValdoraScanTime.Text = presaValdoraPLC.GetScanTimeText();
                        });

                        // Afisare randament actual
                        this.Dispatcher.Invoke(() =>
                        {
                            presaValdoraRandamentActual.Text = raportZilnic.GetRandamentActual(raportZilnic.listaDefectePresaValdoraPLC, timpStartRandamentActual);
                        });

                        Thread.Sleep(400);
                    }


                    rullatriceVechePlc._plc.Disconnect();
                    rullatriceLandgrafPLC._plc.Disconnect();
                    elindPLC._plc.Disconnect();
                    pelatriceLandgrafPLC._plc.Disconnect();
                    presaValdoraPLC._plc.Disconnect();
                }
                catch (Exception ex)
                {
                    LogMessegeRaportareAjustaj.WriteMessegeToLogFile(ex.Message.ToString());
                    LogMessegeRaportareAjustaj.WriteMessegeToLogFile("eroare la ultimul catch " +
                        "din program inainte de finally");
                    /*throw*/
                    ;
                }
                finally
                {
                    rullatriceVechePlc._plc.Disconnect();
                    rullatriceLandgrafPLC._plc.Disconnect();
                    elindPLC._plc.Disconnect();
                    pelatriceLandgrafPLC._plc.Disconnect();
                    presaValdoraPLC._plc.Disconnect();
                    CreareRaportZilnic.TrimitereRaportMail("v.moisei@beltrame-group.com", 
                        LogMessegeRaportareAjustaj.GetLogFilePath(), "LogFileStationariAjustaj");
                }



            }
            catch (Exception ex)
            {
                LogMessegeRaportareAjustaj.WriteMessegeToLogFile(ex.Message.ToString());
                LogMessegeRaportareAjustaj.WriteMessegeToLogFile("eroare la ultimul catch " +
                           "din program dupa de finally");
                //MessageBox.Show(ex.Message);
            }


        }

        // Buton setare text din casute
        private void Seteaza_Data_Click(object sender, RoutedEventArgs e)
        {
            oraRaport = oraRaportTextBox.Text;
            delayDefectRaport = Convert.ToInt32(delayRaportTextBox.Text);
            adreseMailDeTrimis = adreseMailTextBox.Text;
        }


        // Buton Afisare text in casute
        private void Afiseaza_Data_Click(object sender, RoutedEventArgs e)
        {
            oraRaportTextBox.Text = oraRaport;
            delayRaportTextBox.Text = delayDefectRaport.ToString();
            adreseMailTextBox.Text = adreseMailDeTrimis;
        }

        // Functie click start comunicatie
        private void Start_Comm_Click(object sender, RoutedEventArgs e)
        {
            if (!executieProgram)
            {

                executieProgram = true;
                Thread t = new Thread(new ThreadStart(ProgramBackground));
                t.Name = "PlcCommunication";
                t.Start();

            }
            //MessageBox.Show(executieProgram.ToString());
        }

        // Functie click stop comunicatie
        private void Stop_Comm_click(object sender, RoutedEventArgs e)
        {
            if (executieProgram)
                executieProgram = false;
            //MessageBox.Show(executieProgram.ToString());
        }

        // Functie incheiere while loop cand inchzi fereastra
        void DataWindow_Closing(object sender, EventArgs e)
        {
            //MessageBox.Show("S-a inchis fereastra");
            executieProgram = false;
            //MessageBox.Show(executieProgram.ToString());
        }
    }
}
